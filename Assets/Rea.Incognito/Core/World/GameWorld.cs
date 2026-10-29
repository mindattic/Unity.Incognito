using System;
using System.Collections.Generic;
using Incognito.Content;
using Incognito.Npc;
using Incognito.Player;
using Incognito.Settings;
using Incognito.Terminal;
using Incognito.Weapon;

namespace Incognito.World
{
    /// <summary>A seated player: their locomotion and weapon rules, what they are holding, and autowalk.</summary>
    public sealed class PlayerEntity
    {
        internal PlayerEntity(PlayerId id, PlayerSetup setup)
        {
            Id = id;
            Locomotion = new PlayerLocomotionState(setup.Locomotion);
            Locomotion.SetInitialPositionX(setup.StartX);
            Weapon = new PlayerWeaponState(setup.Weapon);
        }

        public PlayerId Id { get; }
        public PlayerLocomotionState Locomotion { get; }
        public PlayerWeaponState Weapon { get; }

        /// <summary>What the player was last sent holding (<see cref="SetControls"/>).</summary>
        public PlayerControls Controls { get; internal set; } = PlayerControls.Idle;

        /// <summary>obj_walker's autowalk: walking the way they face, ignoring the walk keys (a side exit set it).</summary>
        public bool AutoWalk { get; internal set; }

        /// <summary>Aiming locks walking (the original forced idle and ignored the keys).</summary>
        public bool MovementLocked => Weapon.IsAiming;
    }

    /// <summary>How a player is set up when seated: where they start (original px) and their
    /// validated locomotion/weapon tuning (U.INC-T22, <see cref="SettingsRegistry"/>).</summary>
    public struct PlayerSetup
    {
        public float StartX;
        public LocomotionTuning Locomotion;
        public WeaponTuning Weapon;

        /// <summary>The original's own tuning, for callers that need a setup without loading one.</summary>
        public static PlayerSetup Default => new PlayerSetup
        {
            StartX = 0f,
            Locomotion = new LocomotionTuning(),
            Weapon = new WeaponTuning(),
        };
    }

    /// <summary>A patrolling NPC: its patrol and its suspicion of the player it watches.</summary>
    public sealed class NpcEntity
    {
        internal NpcEntity(NpcId id, NpcSetup setup)
        {
            Id = id;
            Watching = setup.Watching;
            Patrol = new NpcPatrolState { WalkSpeed = setup.WalkSpeed, ArriveRange = setup.ArriveRange };
            Patrol.SetInitialPositionX(setup.StartX);
            Patrol.SetRoute(setup.Waypoints ?? new float[0]);
            Suspicion = new SuspicionMeter(setup.Suspicion);
        }

        public NpcId Id { get; }

        /// <summary>The player whose movement, facing and cursor this NPC reacts to.</summary>
        public PlayerId Watching { get; }

        public NpcPatrolState Patrol { get; }
        public SuspicionMeter Suspicion { get; }
    }

    /// <summary>How an NPC is set up: where it starts and patrols (original px), its tuning and who it watches.</summary>
    public struct NpcSetup
    {
        public PlayerId Watching;
        public float StartX;
        public float[] Waypoints;
        public float WalkSpeed;
        public float ArriveRange;
        public SuspicionTuning Suspicion;
    }

    /// <summary>
    /// The whole game's state and rules in one place, held by one authority. Players change it only by
    /// <see cref="Submit"/>ting <see cref="GameCommand"/>s (each carrying the sender's
    /// <see cref="PlayerId"/>); the authority advances it with <see cref="Step"/>, once per frame.
    /// Everything that happens comes out as <see cref="GameEvent"/>s, in order
    /// (<see cref="TakeEvents"/>), for presentation to play. Nothing else mutates it: presentation reads
    /// <see cref="Players"/>, <see cref="Npcs"/> and the entities' states.
    ///
    /// One <see cref="Step"/> runs the original's frame in the order the pieces ran it: each player
    /// walks (the held keys, autowalk or the aim lock), barriers hold them, side exits catch them,
    /// each NPC patrols and reacts to the player it watches, then each weapon's arms step.
    /// Seating players, NPCs, barriers, side exits and console objects is the authority's own setup
    /// (what the scene contains), not a player's request, so those are methods rather than commands.
    /// </summary>
    public sealed class GameWorld
    {
        private sealed class Barrier
        {
            public float StripX;
            public float PlayerWidth;
        }

        private sealed class SideExit
        {
            public int Id;
            public float StripX;
            public float PlayerWidth;

            // Per player, not one flag for the whole exit: a seat taking it must not block every
            // other seat sharing the room (the multiplayer seam).
            public readonly HashSet<PlayerId> TakenBy = new HashSet<PlayerId>();
        }

        private readonly List<PlayerEntity> players = new List<PlayerEntity>();
        private readonly List<NpcEntity> npcs = new List<NpcEntity>();
        private readonly List<Barrier> barriers = new List<Barrier>();
        private readonly List<SideExit> sideExits = new List<SideExit>();
        private readonly List<KeyValuePair<ConsoleObject, Vec2>> consoleObjects = new List<KeyValuePair<ConsoleObject, Vec2>>();
        private readonly List<GameEvent> events = new List<GameEvent>();
        private int nextNpcId = 1;
        private int nextSideExitId = 1;

        /// <summary>The typed console's text (U.INC-T22); null takes the original's own copy.</summary>
        public ConsoleText ConsoleText { get; set; }

        public GameWorld(ConsoleText consoleText = null)
        {
            ConsoleText = consoleText ?? new ConsoleText();
        }

        public IReadOnlyList<PlayerEntity> Players => players;
        public IReadOnlyList<NpcEntity> Npcs => npcs;

        /// <summary>The console objects and where they stand (original px).</summary>
        public IReadOnlyList<KeyValuePair<ConsoleObject, Vec2>> ConsoleObjects => consoleObjects;

        /// <summary>Events produced since the last <see cref="TakeEvents"/>, oldest first.</summary>
        public IReadOnlyList<GameEvent> PendingEvents => events;

        // --- Setup (the authority's own) -----------------------------------------------------

        /// <summary>Seats a player. Throws if <paramref name="id"/> is already in the world.</summary>
        public PlayerEntity AddPlayer(PlayerId id, PlayerSetup setup)
        {
            if (FindPlayer(id) != null) throw new ArgumentException("Player " + id + " is already in the world.", nameof(id));

            var player = new PlayerEntity(id, setup);
            player.Locomotion.OnBobPeak += () => events.Add(new PlayerFootstep(id));
            player.Weapon.OnSound += sound => events.Add(new WeaponSounded(id, sound));
            players.Add(player);
            return player;
        }

        /// <summary>
        /// Unseats a player (an online seat released, U.INC-T43). Their side-exit marks go with them, so
        /// a later player in the same seat starts fresh. Returns whether they were seated.
        /// </summary>
        public bool RemovePlayer(PlayerId id)
        {
            PlayerEntity player = FindPlayer(id);
            if (player == null) return false;

            players.Remove(player);
            foreach (SideExit exit in sideExits) exit.TakenBy.Remove(id);
            return true;
        }

        public PlayerEntity FindPlayer(PlayerId id)
        {
            foreach (PlayerEntity player in players)
            {
                if (player.Id == id) return player;
            }

            return null;
        }

        /// <summary>Adds a patrolling NPC and returns its entity (with the id every command about it uses).</summary>
        public NpcEntity AddNpc(NpcSetup setup)
        {
            var npc = new NpcEntity(new NpcId(nextNpcId++), setup);
            npc.Patrol.ConversationStarted += facing =>
            {
                // obj_npc's arrival turns the player to face it (unless aiming).
                PlayerEntity watched = FindPlayer(npc.Watching);
                if (watched != null && !watched.MovementLocked) watched.Locomotion.TurnToFace(facing);
                events.Add(new ConversationStarted(npc.Id));
            };
            npcs.Add(npc);
            return npc;
        }

        public NpcEntity FindNpc(NpcId id)
        {
            foreach (NpcEntity npc in npcs)
            {
                if (npc.Id == id) return npc;
            }

            return null;
        }

        public bool RemoveNpc(NpcId id)
        {
            NpcEntity npc = FindNpc(id);
            return npc != null && npcs.Remove(npc);
        }

        /// <summary>obj_barrier: a 10px wall at <paramref name="stripX"/> the player's box (<paramref name="playerWidth"/>) cannot walk through.</summary>
        public void AddBarrier(float stripX, float playerWidth)
        {
            barriers.Add(new Barrier { StripX = stripX, PlayerWidth = playerWidth });
        }

        /// <summary>obj_exit_side: a strip at <paramref name="stripX"/> that, touched, puts the player on autowalk and produces <see cref="SideExitReached"/> once. Returns its id.</summary>
        public int AddSideExit(float stripX, float playerWidth)
        {
            int id = nextSideExitId++;
            sideExits.Add(new SideExit { Id = id, StripX = stripX, PlayerWidth = playerWidth });
            return id;
        }

        /// <summary>Makes an object visible to the typed console at <paramref name="position"/> (original px). Replaces an equal object.</summary>
        public void RegisterConsoleObject(ConsoleObject obj, Vec2 position)
        {
            int at = IndexOfConsoleObject(obj);
            var entry = new KeyValuePair<ConsoleObject, Vec2>(obj, position);
            if (at >= 0) consoleObjects[at] = entry;
            else consoleObjects.Add(entry);
        }

        public bool UnregisterConsoleObject(ConsoleObject obj)
        {
            int at = IndexOfConsoleObject(obj);
            if (at < 0) return false;
            consoleObjects.RemoveAt(at);
            return true;
        }

        private int IndexOfConsoleObject(ConsoleObject obj)
        {
            for (int i = 0; i < consoleObjects.Count; i++)
            {
                if (consoleObjects[i].Key.Equals(obj)) return i;
            }

            return -1;
        }

        // --- Commands ------------------------------------------------------------------------

        /// <summary>
        /// Applies a player's command at once and returns whether it took effect (an unseated sender,
        /// an unknown NPC, a holstered gun or an out-of-reach door ignore it). Anything it sets
        /// going comes out as events.
        /// </summary>
        public bool Submit(GameCommand command)
        {
            if (command == null) return false;
            PlayerEntity player = FindPlayer(command.Player);
            if (player == null) return false;

            switch (command)
            {
                case SetControls c:
                    player.Controls = c.Controls;
                    return true;

                case ToggleGun _:
                    player.Weapon.ToggleGun();
                    return true;

                case PressAim _:
                    return player.Weapon.AimPressed();

                case ReleaseAim _:
                    player.Weapon.AimReleased();
                    return true;

                case PressFire _:
                    if (!player.Weapon.FirePressed(player.Locomotion.Facing, out var spawn)) return false;
                    events.Add(new BulletFired(player.Id, spawn));
                    return true;

                case ReleaseFire _:
                    player.Weapon.FireReleased();
                    return true;

                case PlacePlayer c:
                    player.Locomotion.SetInitialPositionX(c.X);
                    player.Locomotion.TurnToFace(c.Facing);
                    player.AutoWalk = false;
                    events.Add(new PlayerPlaced(player.Id));
                    return true;

                case TeleportPlayer c:
                    player.Locomotion.SetInitialPositionX(c.X);
                    return true;

                case TurnPlayer c:
                    return player.Locomotion.TurnToFace(c.Facing);

                case SetAutoWalk c:
                    player.AutoWalk = c.AutoWalk;
                    return true;

                case UseDoor c:
                    if (!RoomExitRules.CanUseDoor(player.Locomotion.PositionX, c.DoorX)) return false;
                    player.Locomotion.ForceIdle(); // player.mode = "idle"
                    events.Add(new DoorUsed(player.Id, c.DoorX));
                    return true;

                case TalkToNpc c:
                    NpcEntity talker = FindNpc(c.Npc);
                    if (talker == null) return false;
                    talker.Patrol.StartTalking(player.Locomotion.PositionX, c.StandOff);
                    return true;

                case ShoveNpc c:
                    NpcEntity shoved = FindNpc(c.Npc);
                    if (shoved == null) return false;
                    shoved.Patrol.Knockback(c.Distance);
                    events.Add(new NpcShoved(shoved.Id, player.Id, c.Distance));
                    return true;

                case RunConsoleCommand c:
                    RunConsole(player.Id, c);
                    return true;

                default:
                    return false;
            }
        }

        private void RunConsole(PlayerId player, RunConsoleCommand command)
        {
            var interpreter = new CommandInterpreter(() => Vicinity.Within(command.Origin, command.Radius, consoleObjects), ConsoleText);
            interpreter.Rebooted += obj => events.Add(new ConsoleObjectRebooted(player, obj));
            interpreter.Overwritten += obj => events.Add(new ConsoleObjectOverwritten(player, obj));
            events.Add(new ConsoleOutput(player, interpreter.Execute(command.Line)));
        }

        // --- The frame -----------------------------------------------------------------------

        /// <summary>Advances the world one frame of <paramref name="deltaTime"/> seconds.</summary>
        public void Step(float deltaTime)
        {
            foreach (PlayerEntity player in players)
            {
                PlayerControls keys = player.Controls;
                bool moveLeft = keys.MoveLeft;
                bool moveRight = keys.MoveRight;
                bool sprint = keys.Sprint;

                if (player.AutoWalk)
                {
                    moveLeft = player.Locomotion.Facing == PlayerLocomotionState.FacingDirection.Left;
                    moveRight = !moveLeft;
                    sprint = false;
                }
                else if (player.MovementLocked)
                {
                    player.Locomotion.ForceIdle();
                    moveLeft = moveRight = sprint = false;
                }

                player.Locomotion.Step(deltaTime, moveLeft, moveRight, sprint);
            }

            foreach (PlayerEntity player in players)
            {
                foreach (Barrier barrier in barriers)
                {
                    float x = player.Locomotion.PositionX;
                    float blocked = RoomExitRules.Block(x, barrier.PlayerWidth, barrier.StripX);
                    if (blocked != x) player.Locomotion.SetInitialPositionX(blocked);
                }

                foreach (SideExit exit in sideExits)
                {
                    if (exit.TakenBy.Contains(player.Id)) continue;
                    if (!RoomExitRules.Overlaps(player.Locomotion.PositionX, exit.PlayerWidth, exit.StripX)) continue;

                    exit.TakenBy.Add(player.Id);
                    player.AutoWalk = true;
                    events.Add(new SideExitReached(player.Id, exit.Id));
                }
            }

            foreach (NpcEntity npc in npcs)
            {
                PlayerEntity watched = FindPlayer(npc.Watching);
                if (watched != null && watched.Locomotion.CurrentMode == PlayerLocomotionState.WalkMode.Walking)
                {
                    npc.Patrol.PlayerWalked();
                }

                npc.Patrol.Step(deltaTime);

                if (watched != null)
                {
                    npc.Suspicion.Step(
                        deltaTime,
                        npc.Patrol.PositionX, npc.Patrol.Facing,
                        watched.Locomotion.PositionX, watched.Locomotion.Facing,
                        watched.Controls.ViewWidth, watched.Controls.LookingDown);
                }
            }

            foreach (PlayerEntity player in players)
            {
                player.Weapon.Step(deltaTime, player.Locomotion.CurrentMode, player.Locomotion.Facing,
                    player.Controls.ShiftHeld, player.Controls.MouseBelowCentre);
            }
        }

        /// <summary>Returns the events produced since the last call, oldest first, and clears them.</summary>
        public GameEvent[] TakeEvents()
        {
            GameEvent[] taken = events.ToArray();
            events.Clear();
            return taken;
        }
    }
}

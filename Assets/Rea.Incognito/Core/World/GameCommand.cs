using Incognito.Player;

namespace Incognito.World
{
    /// <summary>
    /// A player's request to change the game, sent to the one authority that owns the
    /// <see cref="GameWorld"/> (<see cref="GameWorld.Submit"/>). Every command carries the id of the
    /// player it comes from and is plain immutable data (ids, numbers, strings), so a networked
    /// player can carry it to the authority unchanged. Presentation never changes the world any
    /// other way; what a command does comes back out as <see cref="GameEvent"/>s.
    /// </summary>
    public abstract class GameCommand
    {
        protected GameCommand(PlayerId player)
        {
            Player = player;
        }

        public PlayerId Player { get; }
    }

    /// <summary>What a player is holding and where their view is, sent each frame (held keys, the cursor's height, the camera's width).</summary>
    public readonly struct PlayerControls
    {
        public PlayerControls(bool moveLeft, bool moveRight, bool sprint, bool shiftHeld, float mouseBelowCentre,
            bool lookingDown, float viewWidth)
        {
            MoveLeft = moveLeft;
            MoveRight = moveRight;
            Sprint = sprint;
            ShiftHeld = shiftHeld;
            MouseBelowCentre = mouseBelowCentre;
            LookingDown = lookingDown;
            ViewWidth = viewWidth;
        }

        /// <summary>The walk keys, already blanked by the client while the typed console has the keyboard.</summary>
        public bool MoveLeft { get; }
        public bool MoveRight { get; }

        /// <summary>Shift held and free to sprint (not typing in the console).</summary>
        public bool Sprint { get; }

        /// <summary>Shift held at all: the arms swing wider whenever it is, typing or not.</summary>
        public bool ShiftHeld { get; }

        /// <summary>Cursor distance below the middle of the view, original px (GM: mouse_y - room_height/2).</summary>
        public float MouseBelowCentre { get; }

        /// <summary>The cursor is in the lower half of the screen (suspicion's "looking down").</summary>
        public bool LookingDown { get; }

        /// <summary>The player's view width in original px, which sets the suspicion ranges.</summary>
        public float ViewWidth { get; }

        public static PlayerControls Idle => new PlayerControls(false, false, false, false, 0f, false, 1920f);
    }

    /// <summary>Set what the player is holding; applied at the next <see cref="GameWorld.Step"/>.</summary>
    public sealed class SetControls : GameCommand
    {
        public SetControls(PlayerId player, PlayerControls controls) : base(player)
        {
            Controls = controls;
        }

        public PlayerControls Controls { get; }

        public override string ToString() => "SetControls " + Player;
    }

    /// <summary>The draw/holster key (Z).</summary>
    public sealed class ToggleGun : GameCommand
    {
        public ToggleGun(PlayerId player) : base(player)
        {
        }

        public override string ToString() => "ToggleGun " + Player;
    }

    /// <summary>The aim button went down. Takes effect only with the gun drawn.</summary>
    public sealed class PressAim : GameCommand
    {
        public PressAim(PlayerId player) : base(player)
        {
        }

        public override string ToString() => "PressAim " + Player;
    }

    /// <summary>The aim button came up.</summary>
    public sealed class ReleaseAim : GameCommand
    {
        public ReleaseAim(PlayerId player) : base(player)
        {
        }

        public override string ToString() => "ReleaseAim " + Player;
    }

    /// <summary>The fire button went down. Fires only while aiming, producing a <see cref="BulletFired"/>.</summary>
    public sealed class PressFire : GameCommand
    {
        public PressFire(PlayerId player) : base(player)
        {
        }

        public override string ToString() => "PressFire " + Player;
    }

    /// <summary>The fire button came up.</summary>
    public sealed class ReleaseFire : GameCommand
    {
        public ReleaseFire(PlayerId player) : base(player)
        {
        }

        public override string ToString() => "ReleaseFire " + Player;
    }

    /// <summary>obj_room create: puts the player at <see cref="X"/> (original px) facing <see cref="Facing"/>, no longer on autowalk.</summary>
    public sealed class PlacePlayer : GameCommand
    {
        public PlacePlayer(PlayerId player, float x, PlayerLocomotionState.FacingDirection facing) : base(player)
        {
            X = x;
            Facing = facing;
        }

        public float X { get; }
        public PlayerLocomotionState.FacingDirection Facing { get; }

        public override string ToString() => "PlacePlayer " + Player + " " + X;
    }

    /// <summary>Moves the player to <see cref="X"/> (original px) without walking there.</summary>
    public sealed class TeleportPlayer : GameCommand
    {
        public TeleportPlayer(PlayerId player, float x) : base(player)
        {
            X = x;
        }

        public float X { get; }

        public override string ToString() => "TeleportPlayer " + Player + " " + X;
    }

    /// <summary>Turns the player in place (only while idle). Succeeds only if it turned.</summary>
    public sealed class TurnPlayer : GameCommand
    {
        public TurnPlayer(PlayerId player, PlayerLocomotionState.FacingDirection facing) : base(player)
        {
            Facing = facing;
        }

        public PlayerLocomotionState.FacingDirection Facing { get; }

        public override string ToString() => "TurnPlayer " + Player + " " + Facing;
    }

    /// <summary>Starts or stops obj_walker's autowalk: the player keeps walking the way they face, ignoring the keys.</summary>
    public sealed class SetAutoWalk : GameCommand
    {
        public SetAutoWalk(PlayerId player, bool autoWalk) : base(player)
        {
            AutoWalk = autoWalk;
        }

        public bool AutoWalk { get; }

        public override string ToString() => "SetAutoWalk " + Player + " " + AutoWalk;
    }

    /// <summary>obj_exit's W press at the door standing at <see cref="DoorX"/>: within reach it idles the player and produces <see cref="DoorUsed"/>.</summary>
    public sealed class UseDoor : GameCommand
    {
        public UseDoor(PlayerId player, float doorX) : base(player)
        {
            DoorX = doorX;
        }

        public float DoorX { get; }

        public override string ToString() => "UseDoor " + Player + " " + DoorX;
    }

    /// <summary>A talk-mode click on an NPC: it walks over to the player and talks. <see cref="StandOff"/> is its sprite width, original px.</summary>
    public sealed class TalkToNpc : GameCommand
    {
        public TalkToNpc(PlayerId player, NpcId npc, float standOff) : base(player)
        {
            Npc = npc;
            StandOff = standOff;
        }

        public NpcId Npc { get; }
        public float StandOff { get; }

        public override string ToString() => "TalkToNpc " + Player + " " + Npc;
    }

    /// <summary>The player's bullet struck an NPC: it is shoved <see cref="Distance"/> original px (signed).</summary>
    public sealed class ShoveNpc : GameCommand
    {
        public ShoveNpc(PlayerId player, NpcId npc, float distance) : base(player)
        {
            Npc = npc;
            Distance = distance;
        }

        public NpcId Npc { get; }
        public float Distance { get; }

        public override string ToString() => "ShoveNpc " + Player + " " + Npc + " " + Distance;
    }

    /// <summary>Runs a typed console line against the console objects within <see cref="Radius"/> of <see cref="Origin"/> (original px).</summary>
    public sealed class RunConsoleCommand : GameCommand
    {
        public RunConsoleCommand(PlayerId player, string line, Vec2 origin, float radius) : base(player)
        {
            Line = line ?? string.Empty;
            Origin = origin;
            Radius = radius;
        }

        public string Line { get; }
        public Vec2 Origin { get; }
        public float Radius { get; }

        public override string ToString() => "RunConsoleCommand " + Player + " " + Line;
    }
}

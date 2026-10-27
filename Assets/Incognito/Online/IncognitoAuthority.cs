using System.Collections.Generic;
using Incognito.Content;
using Incognito.Settings;
using Incognito.World;
using Unity.Multiplayer;      // NetSession
using Unity.Multiplayer.Core; // Seat, Disconnection
using UnityEngine;

namespace Incognito.Online
{
    /// <summary>
    /// The one authority of an online game (U.INC-T43, rebuild standard point 9): on the server (a listen
    /// server's host or a dedicated server), and only there, it holds the session's Core
    /// <see cref="GameWorld"/>, the same world and the same commands local play's <see cref="GameSession"/>
    /// runs, with every seat's player in it. Each <see cref="IncognitoNetPlayer"/> submits its owner's
    /// input here as Core commands carrying its seat's <see cref="PlayerId"/>; the authority steps the world
    /// once a frame and writes every player's state into that player's replicated fields. Clients never
    /// have one: they send input and draw what arrives. One per server session (a test process runs a host
    /// and its clients side by side), made on first use by <see cref="For"/> and gone when the session ends.
    /// </summary>
    public sealed class IncognitoAuthority : MonoBehaviour
    {
        private static readonly Dictionary<NetSession, IncognitoAuthority> bySession = new Dictionary<NetSession, IncognitoAuthority>();

        private readonly Dictionary<PlayerId, IncognitoNetPlayer> players = new Dictionary<PlayerId, IncognitoNetPlayer>();
        private NetSession session;

        /// <summary>The state this authority owns. Change it only through <see cref="Submit"/>.</summary>
        public GameWorld World { get; private set; }

        /// <summary>The room the session plays in (<see cref="OnlineTuning.Room"/>).</summary>
        public RoomDefinition Room { get; private set; }

        /// <summary>The players seated in <see cref="World"/> with a live network object, by id.</summary>
        public IReadOnlyDictionary<PlayerId, IncognitoNetPlayer> Players => players;

        /// <summary>Events the world produced, played to <see cref="Played"/> on the server (bullets, footsteps).</summary>
        public event System.Action<GameEvent> Played;

        /// <summary>
        /// The authority of <paramref name="netSession"/>, made the first time its server asks. Only server
        /// code calls this (an entity's own <c>IsServer</c>: a listen server's own player spawns while the host
        /// is still starting); a client never has an authority.
        /// </summary>
        public static IncognitoAuthority For(NetSession netSession)
        {
            if (netSession == null) throw new System.ArgumentNullException(nameof(netSession));
            if (bySession.TryGetValue(netSession, out var existing) && existing != null) return existing;

            var go = new GameObject("IncognitoAuthority");
            var authority = go.AddComponent<IncognitoAuthority>();
            authority.Attach(netSession);
            bySession[netSession] = authority;
            return authority;
        }

        /// <summary>The authority <paramref name="netSession"/> already has, or null (never makes one).</summary>
        public static IncognitoAuthority Find(NetSession netSession) =>
            netSession != null && bySession.TryGetValue(netSession, out var authority) && authority != null ? authority : null;

        /// <summary>The room an online session plays in, from the validated tuning and content.</summary>
        public static RoomDefinition OnlineRoom()
        {
            string id = GameSettings.Tuning.Online.Room;
            foreach (RoomDefinition room in GameSettings.Content.Rooms)
            {
                if (room.Id == id) return room;
            }

            throw new SettingsValidationException("game-tuning.json Online.Room names no room in game-content.json: " + id);
        }

        private void Attach(NetSession netSession)
        {
            session = netSession;
            World = new GameWorld(GameSettings.Content.Console);
            Room = OnlineRoom();
            session.SeatReleased += OnSeatReleased;
            session.Ended += OnEnded;
        }

        /// <summary>Seats <paramref name="player"/>'s seat in the world (a returning seat keeps its player) and returns its id.</summary>
        internal PlayerId Join(IncognitoNetPlayer player, int seatIndex)
        {
            PlayerId id = OnlineSeats.PlayerFor(seatIndex);
            if (World.FindPlayer(id) == null)
            {
                SettingsRegistry tuning = GameSettings.Tuning;
                World.AddPlayer(id, new PlayerSetup
                {
                    StartX = tuning.Online.StartFor(Room, seatIndex),
                    Locomotion = tuning.Locomotion,
                    Weapon = tuning.Weapon,
                });
            }

            players[id] = player;
            player.Publish(World, id);
            return id;
        }

        internal void Leave(IncognitoNetPlayer player)
        {
            foreach (var pair in players)
            {
                if (pair.Value != player) continue;
                players.Remove(pair.Key);
                return;
            }
        }

        /// <summary>Runs a seated player's command (its owner's input, as the client sent it). Returns whether it took effect.</summary>
        public bool Submit(GameCommand command)
        {
            bool applied = World.Submit(command);
            PlayEvents();
            return applied;
        }

        private void Update()
        {
            // A session disposed without raising Ended (a test fixture tearing down) leaves nothing behind either.
            if (session == null || !session.IsActive)
            {
                Destroy(gameObject);
                return;
            }

            Step(Time.deltaTime);
        }

        /// <summary>The one frame: the world steps as local play's does, then every player's replicated state is written.</summary>
        public void Step(float deltaTime)
        {
            World.Step(deltaTime);
            PlayEvents();
            foreach (var pair in players)
            {
                if (pair.Value != null) pair.Value.Publish(World, pair.Key);
            }
        }

        private void PlayEvents()
        {
            foreach (GameEvent e in World.TakeEvents()) Played?.Invoke(e);
        }

        private void OnSeatReleased(Seat seat)
        {
            PlayerId id = OnlineSeats.PlayerFor(seat.Index);
            players.Remove(id);
            World.RemovePlayer(id);
        }

        private void OnEnded(Disconnection why)
        {
            if (this != null) Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (session != null)
            {
                session.SeatReleased -= OnSeatReleased;
                session.Ended -= OnEnded;
                if (bySession.TryGetValue(session, out var registered) && registered == this) bySession.Remove(session);
            }

            players.Clear();
        }
    }
}

using System;
using UnityEngine;

namespace Incognito.World
{
    /// <summary>
    /// The one authority of a scene: owns the Core <see cref="GameWorld"/>, steps it once per frame
    /// and plays what it produced to whoever subscribed to <see cref="Played"/>. Views (the player, NPCs,
    /// doors, the console) read the world's state and send <see cref="GameCommand"/>s through
    /// <see cref="Submit"/>; none of them changes the world any other way and none talks to another
    /// view's state directly. A networked game would run this on the host and carry each player's
    /// commands to it, the same <see cref="PlayerId"/> on both sides.
    ///
    /// Runs in LateUpdate before every view's LateUpdate, so a frame's input (sent from Update) is
    /// stepped and then drawn in the same frame.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class GameSession : MonoBehaviour
    {
        /// <summary>The local seat: single-player games have exactly this one player.</summary>
        public static readonly PlayerId LocalPlayer = new PlayerId(1);

        public GameWorld World { get; } = new GameWorld();

        /// <summary>Raised for each <see cref="GameEvent"/> the world produces, in order.</summary>
        public event Action<GameEvent> Played;

        private bool playing;

        /// <summary>
        /// The session views of this scene use when none was assigned: the one in the scene. The scene
        /// builders assign it explicitly; this keeps a hand-assembled scene working.
        /// </summary>
        public static GameSession Find(GameSession assigned) =>
            assigned != null ? assigned : FindAnyObjectByType<GameSession>();

        /// <summary>Sends a player's command to the world and plays what it produced. Returns whether it took effect.</summary>
        public bool Submit(GameCommand command)
        {
            bool applied = World.Submit(command);
            Play();
            return applied;
        }

        private void LateUpdate()
        {
            World.Step(Time.deltaTime);
            Play();
        }

        // Plays pending events in order. A handler that submits a command adds to the queue the loop
        // is already draining, so events never play out of order or re-enter.
        private void Play()
        {
            if (playing) return;
            playing = true;
            try
            {
                while (World.PendingEvents.Count > 0)
                {
                    foreach (GameEvent e in World.TakeEvents()) Played?.Invoke(e);
                }
            }
            finally
            {
                playing = false;
            }
        }
    }
}

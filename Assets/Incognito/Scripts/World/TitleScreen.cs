using Incognito.Audio;
using Incognito.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Incognito.World
{
    /// <summary>
    /// obj_titlescreen + the title's obj_music: any key or the left mouse button runs
    /// start_game() — straight to Bleecher Street (street00), entering from the right edge
    /// (globals: next_room = street00, enter_from = "right") with the street fading in from black.
    /// Escape and F1 are <see cref="DebugHotkeys"/>' (every room). The title plays the global default track,
    /// music_oppressivegloom (its obj_music's whispers line was commented out).
    /// </summary>
    public sealed class TitleScreen : MonoBehaviour
    {
        [SerializeField] private string firstRoom = "Street00";
        [SerializeField] private AudioClip music;
        [SerializeField] [Range(0f, 1f)] private float musicVolume = 1f;

        private bool started;

        public void Configure(string firstRoom, AudioClip music)
        {
            this.firstRoom = firstRoom;
            this.music = music;
        }

        private void Start()
        {
            MusicPlayer.Play(music, musicVolume);
        }

        private void Update()
        {
            if (started) return;

            if (Input.anyKeyDown || Input.GetMouseButton(0)) StartGame();
        }

        /// <summary>start_game(): the player is created facing left, then room_goto(next_room).</summary>
        public void StartGame()
        {
            started = true;
            RoomTransition.Request(firstRoom, RoomEntrance.Right, 0f, PlayerLocomotionState.FacingDirection.Left);
            SceneManager.LoadScene(firstRoom);
        }
    }
}

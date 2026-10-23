using Incognito.Audio;
using Incognito.Player;
using Incognito.Typing;
using UnityEngine;

namespace Incognito.World
{
    /// <summary>
    /// One room scene's obj_room + obj_music (RECONSTRUCTION.md §7–8): places the player from the
    /// pending <see cref="RoomTransition"/> (enter_from), starts the room's music track, types the
    /// room's opening text if it has one (street00's intro poem), and draws the room's name in the
    /// top-left corner (obj_room's draw-GUI event; get_room_name.gml). Runs before the camera's
    /// Start so the camera snaps to where the player was placed.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class Room : MonoBehaviour
    {
        [SerializeField] private string displayName = string.Empty;
        [Tooltip("Room width in original pixels (room_width).")]
        [SerializeField] private float roomWidth = 1920f;
        [Tooltip("player_width = sprite_get_width(spr_player).")]
        [SerializeField] private float playerWidth = 512f;
        [SerializeField] private PlayerLocomotion player;

        [Header("Music (obj_music current_track)")]
        [SerializeField] private AudioClip music;
        [SerializeField] [Range(0f, 1f)] private float musicVolume = 1f;

        [Header("Opening text (an obj_typewriter placed in the room)")]
        [TextArea(3, 12)]
        [SerializeField] private string openingText = string.Empty;
        [Tooltip("Where it types, original px from the room's top-left.")]
        [SerializeField] private Vector2 openingTextPosition;
        [SerializeField] private Texture2D descriptionBorder;
        [SerializeField] private Camera viewCamera;

        public string DisplayName => displayName;
        public float RoomWidth => roomWidth;
        public float PlayerWidth => playerWidth;
        public PlayerLocomotion Player => player;

        // The 20px inset of draw_text_outline(20, 20, 1, name).
        private const float NameInset = 20f;

        private void Start()
        {
            if (player != null && RoomTransition.TryConsume(out var entrance, out float enterX, out var facing))
            {
                // obj_room create: place by enter_from; the player is idle and no longer autowalking.
                player.Teleport(RoomTransition.EntryX(entrance, roomWidth, playerWidth, enterX));
                player.State.TurnToFace(facing);
                player.AutoWalk = false;
            }

            MusicPlayer.Play(music, musicVolume);

            if (!string.IsNullOrEmpty(openingText))
            {
                const float px = PlayerLocomotion.WorldUnitsPerOriginalPixel;
                var world = new Vector3(openingTextPosition.x * px, (OutlinedText.ReferenceHeight - openingTextPosition.y) * px, 0f);
                DescriptionTypewriter.Show(world, openingText, viewCamera != null ? viewCamera : Camera.main, descriptionBorder);
            }
        }

        private void OnGUI()
        {
            float s = OutlinedText.Scale;
            OutlinedText.Draw(new Rect(NameInset * s, NameInset * s, Screen.width, 60f * s), displayName, TextAnchor.UpperLeft, 1f);
        }
    }
}

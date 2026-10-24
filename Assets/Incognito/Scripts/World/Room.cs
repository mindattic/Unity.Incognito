using Incognito.Audio;
using Incognito.Player;
using Incognito.Typing;
using Incognito.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Incognito.World
{
    /// <summary>
    /// One room scene's obj_room + obj_music (RECONSTRUCTION.md §7–8): places the player from the
    /// pending <see cref="RoomTransition"/> (enter_from), starts the room's music track, types the
    /// room's opening text if it has one (street00's intro poem), and draws the room's name in the
    /// top-left corner (obj_room's draw-GUI event; get_room_name.gml) with a UI Toolkit label
    /// (U.INC-T26 — was IMGUI). Runs before the camera's Start so the camera snaps to where the
    /// player was placed.
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

        /// <summary>Sets every field RoomComposer derives from a room's <c>RoomDefinition</c>
        /// (U.INC-T26), so a room built at runtime is wired exactly as the editor builder wired it.</summary>
        public void Configure(string displayName, float roomWidth, float playerWidth, PlayerLocomotion player,
            AudioClip music, float musicVolume, string openingText, Vector2 openingTextPosition,
            Texture2D descriptionBorder, Camera viewCamera)
        {
            this.displayName = displayName;
            this.roomWidth = roomWidth;
            this.playerWidth = playerWidth;
            this.player = player;
            this.music = music;
            this.musicVolume = musicVolume;
            this.openingText = openingText;
            this.openingTextPosition = openingTextPosition;
            this.descriptionBorder = descriptionBorder;
            this.viewCamera = viewCamera;
        }

        // The 20px inset of draw_text_outline(20, 20, 1, name).
        private const float NameInset = 20f;

        private Label nameLabel;

        private void Awake()
        {
            HudRoot.EnsureInstalled();
            nameLabel = new Label { pickingMode = PickingMode.Ignore };
            nameLabel.style.position = Position.Absolute;
            nameLabel.style.left = NameInset;
            nameLabel.style.top = NameInset;
            OutlinedText.Style(nameLabel);
            HudRoot.RoomInfo.Add(nameLabel);
        }

        private void Start()
        {
            if (player != null && RoomTransition.TryConsume(out var entrance, out float enterX, out var facing))
            {
                // obj_room create: place by enter_from; the player is idle and no longer autowalking.
                player.Send(new PlacePlayer(player.Id, RoomTransition.EntryX(entrance, roomWidth, playerWidth, enterX), facing));
                player.ApplyView(); // so the camera sees the entrance this frame
            }

            MusicPlayer.Play(music, musicVolume);

            if (!string.IsNullOrEmpty(openingText))
            {
                const float px = PlayerLocomotion.WorldUnitsPerOriginalPixel;
                var world = new Vector3(openingTextPosition.x * px, (OutlinedText.ReferenceHeight - openingTextPosition.y) * px, 0f);
                DescriptionTypewriter.Show(world, openingText, viewCamera != null ? viewCamera : Camera.main, descriptionBorder);
            }

            nameLabel.text = displayName;
        }

        private void OnDestroy()
        {
            nameLabel?.RemoveFromHierarchy();
        }
    }
}

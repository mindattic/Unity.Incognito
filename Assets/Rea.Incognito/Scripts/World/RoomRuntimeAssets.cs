using System;
using UnityEngine;

namespace Incognito.World
{
    /// <summary>
    /// The few art/audio assets a room's <see cref="RoomComposer"/> needs that aren't already part
    /// of the sandbox player rig: background tiles, prop sprites, the exit icon, the description
    /// border and the per-room sound clips. The editor's Incognito/Build Room Scenes
    /// (<see cref="EditorTools.RoomSceneBuilder"/>) resolves these once per room (importing the
    /// original art and audio under Assets/Incognito/{Art,Audio}/Original) and writes the result
    /// here as plain object references — never the room's layout itself, which
    /// <see cref="RoomBootstrap"/> reads back out of game-content.json at Awake (U.INC-T26).
    /// </summary>
    [Serializable]
    public sealed class RoomRuntimeAssets
    {
        [Tooltip("One sprite per RoomDefinition.BackgroundSlices entry, in the same order.")]
        public Sprite[] BackgroundSlices = Array.Empty<Sprite>();
        public Sprite LamppostSprite;
        public Sprite LamplightSprite;
        public Sprite MailboxSprite;
        public Sprite RainSprite;
        public Sprite ExitIconSprite;
        public Texture2D DescriptionBorder;

        public AudioClip Music;
        [Tooltip("sfx_step_concrete01..16.")]
        public AudioClip[] Footsteps = Array.Empty<AudioClip>();
        public AudioClip Whoosh;
        public AudioClip ReloadClip;
        public AudioClip HolsterClip;
        public AudioClip FireClip;
        public AudioClip TypeClip;
        public AudioClip DoorOpenClip;
        [Tooltip("sfx_flick: obj_mouse_gui's program-icon click (U.INC-T28).")]
        public AudioClip FlickClip;
    }
}

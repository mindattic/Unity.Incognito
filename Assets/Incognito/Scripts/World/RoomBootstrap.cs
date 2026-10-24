using System;
using Incognito.CameraRig;
using Incognito.Content;
using Incognito.Eye;
using Incognito.Hacking;
using Incognito.Player;
using Incognito.Settings;
using Incognito.Terminal;
using Incognito.Weapon;
using UnityEngine;

namespace Incognito.World
{
    /// <summary>
    /// Builds one room scene's data-driven content at Awake, by id, out of
    /// <see cref="GameSettings.Content"/>'s validated <see cref="RoomDefinition"/>s — a single
    /// runtime bootstrap, not a scene full of individually-authored props and exits (U.INC-T26).
    /// The committed room scenes (Assets/Incognito/Scenes/Rooms/*.unity) hold only a copy of the
    /// sandbox player rig plus one "Room Bootstrap" game object carrying this component with its
    /// <see cref="RoomRuntimeAssets"/> — the few static art/audio references the editor's
    /// Incognito/Build Room Scenes (<see cref="EditorTools.RoomSceneBuilder"/>) resolves — so the
    /// room's actual layout (background tiles, lampposts, mailboxes, exits) is read back out of
    /// game-content.json every time the scene is entered: in Play mode and in a build — this runs
    /// at Awake, not <c>[ExecuteAlways]</c>, so opening a room scene in the Editor without pressing
    /// Play shows only the bare player rig, not the built room; use Play mode (or the sandbox
    /// scene, which is fully authored) to look at a room's content. Regenerating a scene can never
    /// let it drift from the data, because nothing but this component's asset references is baked
    /// into the file.
    ///
    /// Runs before <see cref="Room"/>'s own Start (which places the player from a pending
    /// <see cref="RoomTransition"/>) via its earlier execution order, since <see cref="RoomComposer"/>
    /// is what creates that scene's <see cref="Room"/> component in the first place.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class RoomBootstrap : MonoBehaviour
    {
        [Tooltip("The RoomDefinition.Id this scene builds (also the scene's own name).")]
        [SerializeField] private string roomId = string.Empty;
        [SerializeField] private RoomRuntimeAssets assets = new RoomRuntimeAssets();

        public string RoomId => roomId;
        public RoomRuntimeAssets Assets => assets;

        /// <summary>Sets what the editor builder resolved for this room; called once, at generation
        /// time, never at runtime.</summary>
        public void Configure(string roomId, RoomRuntimeAssets assets)
        {
            this.roomId = roomId;
            this.assets = assets ?? new RoomRuntimeAssets();
        }

        private void Awake()
        {
            RoomDefinition spec = GameSettings.Content.FindRoom(roomId)
                ?? throw new InvalidOperationException($"game-content.json has no room with Id '{roomId}' for RoomBootstrap to build.");

            var player = FindAnyObjectByType<PlayerLocomotion>();
            var framing = FindAnyObjectByType<CloseFramingCamera>();
            var cam = framing != null ? framing.GetComponent<Camera>() : null;
            var cursor = FindAnyObjectByType<EyeCursor>();
            var weapon = FindAnyObjectByType<PlayerWeapon>();
            var prompt = FindAnyObjectByType<CommandPrompt>();
            var hacking = FindAnyObjectByType<HackingOverlay>();

            RoomComposer.Compose(spec, assets, player, framing, cam, cursor, weapon, prompt, hacking);
        }
    }
}

using Incognito.Controls;
using Incognito.Player;
using Incognito.Terminal;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Incognito.World
{
    /// <summary>
    /// obj_debug (created by start_game.gml, persistent, so live in every room): Escape quits,
    /// F1 toggles fullscreen, F12 toggles a readout of the room, player x and mouse position.
    /// Escape closes the console first (it runs before this, so an open prompt keeps the key).
    /// Installed once, before the first scene loads, and kept across scene changes.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class DebugHotkeys : MonoBehaviour
    {
        private bool showReadout;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            var host = new GameObject("DebugHotkeys");
            DontDestroyOnLoad(host);
            host.AddComponent<DebugHotkeys>();
        }

        /// <summary>The F12 readout text.</summary>
        public static string FormatReadout(string room, float playerX, Vector2 mouse)
        {
            return $"room: {room}\nplayerX: {playerX:0}\nmouse: {mouse.x:0}, {mouse.y:0}";
        }

        private void Update()
        {
            if (GameInput.Current.EscapePressed && !CommandPrompt.CapturesKeyboard) Application.Quit();
            if (GameInput.Current.DebugFullscreenTogglePressed) Screen.fullScreen = !Screen.fullScreen;
            if (GameInput.Current.DebugReadoutTogglePressed) showReadout = !showReadout;
        }

        private void OnGUI()
        {
            if (!showReadout) return;
            var player = FindFirstObjectByType<PlayerLocomotion>();
            float x = player != null ? player.transform.position.x : 0f;
            GUI.Label(new Rect(8, 8, 400, 80),
                FormatReadout(SceneManager.GetActiveScene().name, x, GameInput.PointerScreenPosition));
        }
    }
}

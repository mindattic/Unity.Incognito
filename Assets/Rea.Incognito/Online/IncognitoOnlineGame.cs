using Incognito.Display;
using Incognito.Settings;
using Unity.Multiplayer;    // NetBootstrap
using Unity.Multiplayer.UI; // NetFrontEnd, MenuPlacement
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Incognito.Online
{
    /// <summary>
    /// The online scene's one game component (U.INC-T43). The menus are the Unity.Multiplayer kit's drop-in
    /// front end (Host, Join by IP, Join by code, the lobby with its join code and roster, reconnect, the Esc
    /// menu); this adds Incognito's own items to it: <c>Play solo</c>, which leaves for the local game exactly
    /// as it plays without the kit, and how to play. It sizes the session from the tuning's online section
    /// and letterboxes the view as local play does. Runs on every peer: a dedicated server and a bot run this
    /// scene too, where the front end builds no menus.
    /// </summary>
    [DefaultExecutionOrder(-800)] // before NetBootstrap.Start, which may start a headless server at once
    public sealed class IncognitoOnlineGame : MonoBehaviour
    {
        /// <summary>The local game's first scene (Build Settings' first), which <see cref="PlaySolo"/> opens.</summary>
        public const string SoloScene = "Titlescreen";

        public const string PlaySoloLabel = "Play solo";

        /// <summary>The online room's width, original px (what the owner's camera is kept inside).</summary>
        public static float RoomWidth => IncognitoAuthority.OnlineRoom().Width;

        private void Start()
        {
            // A host or a client must keep its connection running while its window is in the background; only
            // the online scene turns this on, so the local game pauses when unfocused exactly as before.
            Application.runInBackground = true;
            if (!Application.isBatchMode) GameDisplay.EnsureInstalled();

            var bootstrap = NetBootstrap.Instance;
            if (bootstrap == null || bootstrap.Session == null)
            {
                Debug.LogError("[Incognito] The online scene has no NetBootstrap; run Incognito > Build Online Scene.", this);
                return;
            }

            if (!bootstrap.Session.IsActive)
                bootstrap.Configure(c => c.MaxPlayers = GameSettings.Tuning.Online.MaxPlayers);

            var frontEnd = NetFrontEnd.Instance;
            if (frontEnd == null) return;

            frontEnd.AddMainMenuItems(s => s.AddButton(PlaySoloLabel, PlaySolo));
            frontEnd.AddScreen("How to play", s =>
            {
                s.AddText("Walk with A and D or the arrow keys, sprint with Shift.");
                s.AddText("Z draws and holsters the gun; hold the right mouse button to aim and click to fire.");
                s.AddText("Esc opens the menu: invite players, change your name and colour, or leave.");
            }, MenuPlacement.Both);
            frontEnd.GameStarted += () => Debug.Log("[Incognito] Online as " + frontEnd.Session.Identity.DisplayName + ".");
        }

        /// <summary>
        /// Leaves the online scene for the local game. The kit's NetworkManager survives scene loads, so it goes
        /// first: local play runs without any of the kit, as it always has.
        /// </summary>
        public static void PlaySolo()
        {
            var bootstrap = NetBootstrap.Instance;
            if (bootstrap != null)
            {
                if (bootstrap.Session != null && bootstrap.Session.IsActive) bootstrap.Leave();
                Destroy(bootstrap.gameObject);
            }

            Application.runInBackground = false; // the project's own setting, as the local game has always run
            SceneManager.LoadScene(SoloScene, LoadSceneMode.Single);
        }
    }
}

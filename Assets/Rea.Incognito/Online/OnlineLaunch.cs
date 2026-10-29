using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Incognito.Online
{
    /// <summary>
    /// How a build reaches the online scene (U.INC-T43) without changing the original's own screens: the
    /// title, rooms and keys stay exactly as the original had them, and online play is opened from the launch
    /// line instead. <c>-online</c> (play.ps1 -Online) opens the online scene's menus; the kit's own launch
    /// switches, <c>-mode</c> (a dedicated server or a headless host) and <c>-bot</c> (mp-host game smoke),
    /// open it too, so a server or bot started from the one Windows player runs the online scene.
    /// </summary>
    public static class OnlineLaunch
    {
        /// <summary>The online scene's name in Build Settings.</summary>
        public const string OnlineScene = "Online";

        /// <summary>Whether <paramref name="args"/> ask for the online scene.</summary>
        public static bool Requested(string[] args)
        {
            if (args == null) return false;
            foreach (string arg in args)
            {
                if (string.Equals(arg, "-online", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(arg, "-mode", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(arg, "-bot", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OpenWhenAsked()
        {
            // Never in the editor (a test run's own command line names -runTests, not these, but Play mode
            // must open whatever scene is open) and never when the online scene is already the one loaded.
            if (Application.isEditor || !Requested(Environment.GetCommandLineArgs())) return;
            if (SceneManager.GetActiveScene().name == OnlineScene) return;
            SceneManager.LoadScene(OnlineScene, LoadSceneMode.Single);
        }
    }
}

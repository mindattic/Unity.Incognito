using System.Collections;
using Incognito.Hacking;
using Incognito.Terminal;
using Incognito.Typing;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Incognito.Tests
{
    /// <summary>
    /// U.INC-T26 end-to-end check: the UI Toolkit HUD (<see cref="Incognito.UI.HudRoot"/> and the
    /// 7 former OnGUI classes) runs without error in an actual Play session — opening and typing in
    /// the console, toggling the hacking overlay, and a floating description typing itself out to
    /// completion — not just compiling.
    /// </summary>
    public class HudSmokeTests
    {
        [UnityTest]
        public IEnumerator The_console_overlay_and_a_typed_description_all_run_without_error()
        {
            SceneManager.LoadScene("Street00");
            yield return null;
            yield return null;
            yield return null;

            var prompt = Object.FindAnyObjectByType<CommandPrompt>();
            Assert.IsNotNull(prompt, "street00 has its CommandPrompt");
            var overlay = Object.FindAnyObjectByType<HackingOverlay>();
            Assert.IsNotNull(overlay, "street00 has its HackingOverlay");
            Assert.IsNotNull(overlay.FlickSound, "the room wired obj_mouse_gui's sfx_flick to the program icon (U.INC-T28)");
            Assert.AreEqual("sfx_flick", overlay.FlickSound.name);

            // Open, type and submit a line, then close.
            prompt.Open();
            for (int i = 0; i < 10; i++) yield return null;
            Assert.IsTrue(prompt.IsShowing, "the console opened");

            prompt.Submit("help");
            yield return null;
            Assert.AreEqual(string.Empty, prompt.InputText, "the input line clears after Enter");

            prompt.Close();
            for (int i = 0; i < 30; i++) yield return null;
            Assert.IsFalse(prompt.IsShowing, "the console closed");

            // Toggle the hacking overlay's backdrop on, then off.
            overlay.ToggleInverted();
            for (int i = 0; i < 10; i++) yield return null;
            Assert.IsTrue(overlay.Session.Inverted, "the overlay inverted the cursor");

            overlay.ToggleInverted();
            for (int i = 0; i < 40; i++) yield return null;
            Assert.IsFalse(overlay.Session.Inverted, "the overlay restored the player's view");

            // A floating description types itself out and destroys itself.
            var typewriter = DescriptionTypewriter.Show(new Vector3(10f, 10f, 0f), "hello", Camera.main, null);
            float waited = 0f;
            while (typewriter != null && waited < 5f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.IsTrue(waited < 5f, "the typewriter finished typing and destroyed itself");
        }
    }
}

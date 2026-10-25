using System.Collections;
using System.IO;
using System.Linq;
using Incognito.Npc;
using Incognito.Terminal;
using Incognito.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Incognito.Tests
{
    /// <summary>
    /// U.INC-T31, played in a real room: every HUD text is drawn in its original font at the
    /// original's size (fnt_large's 26.7px for obj_room's name and obj_npc's suspicion, fnt_system's
    /// 13.3px for obj_console, fnt_basic's 20px for obj_float_text) with the original's outline,
    /// the console keeps its output's GM '##' blank line and indent, and floating text floats up,
    /// fades and destroys itself. With a graphics device, it also saves a screenshot of the room's
    /// text to Logs/ for a by-eye check.
    /// </summary>
    public class OriginalTextPlayTests
    {
        private const float Large = 16f * 120f / 72f;
        private const float Basic = 12f * 120f / 72f;
        private const float System = 8f * 120f / 72f;

        private static Label FindLabel(VisualElement layer, string text) =>
            layer.Query<Label>().ToList().FirstOrDefault(l => l.text == text);

        [UnityTest]
        public IEnumerator Room_npc_console_and_floating_text_use_the_original_fonts_and_outlines()
        {
            SceneManager.LoadScene("Street00");
            for (int i = 0; i < 5; i++) yield return null;

            // obj_room: fnt_large, draw_text_outline(20, 20, 1, name).
            Label roomName = FindLabel(HudRoot.RoomInfo, "Bleecher Street");
            Assert.IsNotNull(roomName, "the room's name is drawn");
            Assert.AreEqual(Large, roomName.resolvedStyle.fontSize, 0.01f);
            Assert.AreEqual(1f, roomName.resolvedStyle.unityTextOutlineWidth);
            Assert.AreEqual(Color.black, roomName.resolvedStyle.unityTextOutlineColor);
            Assert.AreEqual(Color.white, roomName.resolvedStyle.color);
            Assert.AreEqual(20f, roomName.resolvedStyle.left, 0.5f, "inset 20px (to within the panel's pixel snapping)");
            Assert.AreEqual(20f, roomName.resolvedStyle.top, 0.5f);

            // obj_npc: fnt_large, draw_text_outline(x, y - 200, 1, suspicion + "%").
            var npc = Object.FindAnyObjectByType<NpcController>();
            Assert.IsNotNull(npc, "street00 keeps its NPC");
            StringAssert.EndsWith("%", npc.SuspicionText);
            Label suspicion = FindLabel(HudRoot.WorldLabels, npc.SuspicionText);
            Assert.IsNotNull(suspicion, "the suspicion label is a UI Toolkit label");
            Assert.AreEqual(Large, suspicion.resolvedStyle.fontSize, 0.01f);
            Assert.AreEqual(1f, suspicion.resolvedStyle.unityTextOutlineWidth);

            // obj_console: fnt_system, plain draw_text; the help text's GM "##" blank line and indent survive.
            var prompt = Object.FindAnyObjectByType<CommandPrompt>();
            prompt.Open();
            prompt.Submit("help");
            for (int i = 0; i < 10; i++) yield return null;
            StringAssert.StartsWith("help\n\n   list:", prompt.Output);
            Label output = FindLabel(HudRoot.ConsolePanel, prompt.Output);
            Assert.IsNotNull(output, "the console draws its output");
            Assert.AreEqual(System, output.resolvedStyle.fontSize, 0.01f);
            Assert.AreEqual(0f, output.resolvedStyle.unityTextOutlineWidth, "obj_console's text is not outlined");
            Assert.AreEqual(WhiteSpace.PreWrap, output.resolvedStyle.whiteSpace, "the indent's spaces are kept");

            // show_float_text: fnt_basic, centred, 1px outline, floats up and fades out.
            var cam = Camera.main;
            Vector3 at = cam.transform.position + new Vector3(0f, 0f, 10f);
            FloatText floating = FloatText.Show(at, "show_float_text", cam);
            yield return null;
            yield return null;
            Label floatLabel = FindLabel(HudRoot.WorldLabels, "show_float_text");
            Assert.IsNotNull(floatLabel);
            Assert.AreEqual(Basic, floatLabel.resolvedStyle.fontSize, 0.01f);
            Assert.AreEqual(1f, floatLabel.resolvedStyle.unityTextOutlineWidth);
            float firstTop = floatLabel.resolvedStyle.top;

            yield return CaptureIfRendering("t31-original-text.png");

            float waited = 0f;
            float lastTop = firstTop;
            while (floating != null && waited < 6f)
            {
                if (floatLabel.panel != null) lastTop = floatLabel.resolvedStyle.top;
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.IsTrue(floating == null, "the floating text faded out and destroyed itself");
            Assert.Less(lastTop, firstTop, "it floated up");
            Assert.IsNull(floatLabel.panel, "its label left the HUD with it");

            prompt.Close();
        }

        // Batch mode never reaches WaitForEndOfFrame, so ScreenCapture can't be used: instead the
        // camera and the HUD panel both render into one 1920x1080 texture for a frame, read back.
        private static IEnumerator CaptureIfRendering(string file)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) yield break;
            Camera cam = Camera.main;
            UIDocument hud = Object.FindAnyObjectByType<UIDocument>();
            if (cam == null || hud == null) yield break;

            var target = new RenderTexture(1920, 1080, 24);
            PanelSettings panel = hud.panelSettings;
            bool clear = panel.clearColor;
            cam.targetTexture = target;
            panel.targetTexture = target;
            panel.clearColor = false; // draw the HUD over the camera's picture
            yield return null;
            yield return null;

            var shot = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            shot.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            shot.Apply();
            RenderTexture.active = previous;

            cam.targetTexture = null;
            panel.targetTexture = null;
            panel.clearColor = clear;

            string dir = Path.Combine(Application.dataPath, "..", "Logs");
            Directory.CreateDirectory(dir);
            File.WriteAllBytes(Path.Combine(dir, file), shot.EncodeToPNG());
            Object.Destroy(shot);
            target.Release();
            Object.Destroy(target);
        }
    }
}

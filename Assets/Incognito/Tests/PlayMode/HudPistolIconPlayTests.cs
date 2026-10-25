using System.Collections;
using Incognito.Hacking;
using Incognito.Weapon;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Incognito.Tests
{
    /// <summary>
    /// U.INC-T30, played in a real room: the HUD pistol icon (obj_icon_pistol) rises to
    /// room_height - 96 at the bottom-right, a click on it toggles the gun exactly like Z (the
    /// world's aim mode and the icon's selected sprite both follow), and obj_mouse_gui's console
    /// click selects the open console and sounds sfx_flick.
    /// </summary>
    public class HudPistolIconPlayTests
    {
        [UnityTest]
        public IEnumerator The_pistol_icon_rises_and_a_click_draws_and_holsters_the_gun()
        {
            SceneManager.LoadScene("Street00");
            yield return null;
            yield return null;

            var icon = Object.FindAnyObjectByType<HudPistolIcon>();
            Assert.IsNotNull(icon, "street00 has the HUD pistol icon");
            var weapon = Object.FindAnyObjectByType<PlayerWeapon>();
            Assert.IsNotNull(weapon);

            for (int i = 0; i < 120 && icon.Slide.Current != ProgramIconSlide.Mode.Idle; i++) yield return null;
            Assert.AreEqual(1080f - 96f, icon.Slide.Y, "risen to endY = room_height - sprite_height");

            float scale = Screen.height / 1080f;
            Assert.AreEqual(Screen.width - (40f + 96f) * scale, icon.ScreenRect.x, 0.01f, "40px in from the right edge");
            Vector2 centre = new Vector2(icon.ScreenRect.center.x, Screen.height - icon.ScreenRect.center.y);
            Assert.IsTrue(HudPistolIcon.IsOverIcon(centre));
            Assert.IsTrue(HackingOverlay.IsOverGui(centre), "a click on the icon is GUI, not the world behind it");

            Assert.AreEqual(PlayerWeaponState.AimMode.Off, weapon.State.Mode);
            Assert.AreEqual("spr_icon_pistol", icon.CurrentTexture.name);

            icon.Click();
            for (int i = 0; i < 3; i++) yield return null;
            Assert.AreEqual(PlayerWeaponState.AimMode.Equip, weapon.State.Mode, "toggle_gun drew the gun");
            Assert.AreEqual("spr_icon_pistol_selected", icon.CurrentTexture.name);

            icon.Click();
            for (int i = 0; i < 3; i++) yield return null;
            Assert.AreEqual(PlayerWeaponState.AimMode.Off, weapon.State.Mode, "toggle_gun holstered it");
            Assert.AreEqual("spr_icon_pistol", icon.CurrentTexture.name);
        }

        [UnityTest]
        public IEnumerator A_console_click_selects_it_and_flicks()
        {
            SceneManager.LoadScene("Street00");
            yield return null;
            yield return null;

            var overlay = Object.FindAnyObjectByType<HackingOverlay>();
            Assert.IsNotNull(overlay);
            overlay.Prompt.Open();
            for (int i = 0; i < 10; i++) yield return null;

            int before = overlay.FlickCount;
            overlay.GuiClicked(GuiClick.Console);
            yield return null;
            Assert.AreEqual(before + 1, overlay.FlickCount, "obj_mouse_gui's console branch plays sfx_flick");
            Assert.IsTrue(overlay.Prompt.IsShowing, "selecting the console leaves it open");

            overlay.GuiClicked(GuiClick.PistolIcon);
            Assert.AreEqual(before + 1, overlay.FlickCount, "the pistol branch plays no flick of its own");

            overlay.Prompt.Close();
        }
    }
}

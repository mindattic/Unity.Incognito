using Incognito.Controls;
using Incognito.Hacking;
using Incognito.Player;
using Incognito.UI;
using Incognito.World;
using UnityEngine;
using UnityEngine.UIElements;

namespace Incognito.Weapon
{
    /// <summary>
    /// The HUD pistol icon (obj_icon_pistol, RECONSTRUCTION.md §1 and §5, U.INC-T30): a 96x96 GUI
    /// button at <c>x = room_width - sprite_get_width(spr_icon_pistol) - 40</c> (start_game.gml, so
    /// 40px in from the right edge of the 1920px view) that eases up from just below the screen to
    /// <c>endY = room_height - sprite_height</c> with obj_program's step code
    /// (<see cref="ProgramIconSlide"/>). It shows <c>spr_icon_pistol_selected</c> while the gun is
    /// out (toggle_gun: <c>"off"</c> -> <c>"equip"</c>) and <c>spr_icon_pistol</c> once holstered,
    /// read from this player's <see cref="PlayerWeaponState"/> in the world. A left click on it
    /// (obj_mouse_gui) sends <see cref="ToggleGun"/>, exactly what Z sends, so the draw/holster
    /// sounds and arm poses come from the same rule.
    ///
    /// Fixed defect: the original created the icon at <c>y = room_height</c> and only obj_gui_menu's
    /// (and the never-created obj_stealth's) dead "scanning" branch ever set it "onscreen", so the
    /// icon never rose and obj_mouse_gui's pistol click could never be reached. The rebuild raises
    /// it when play starts and, as the persistent GM instance would, keeps it up from room to room.
    /// A click on it is GUI (<see cref="HackingOverlay.IsOverGui"/>), so it doesn't also look at or
    /// hack an NPC behind it; firing is not GUI-gated in the original, so a click while aiming still
    /// fires before it holsters, as there.
    /// </summary>
    public sealed class HudPistolIcon : MonoBehaviour
    {
        /// <summary>spr_icon_pistol is 96x96, origin 0,0.</summary>
        public const float IconSize = 96f;

        /// <summary>start_game.gml: <c>room_width - sprite_get_width(spr_icon_pistol) - 40</c>.</summary>
        public const float RightMargin = 40f;

        [Tooltip("The player whose gun this icon toggles and shows.")]
        [SerializeField] private PlayerLocomotion player;

        [Header("Original art")]
        [Tooltip("spr_icon_pistol (96x96): the gun is holstered.")]
        [SerializeField] private Texture2D icon;
        [Tooltip("spr_icon_pistol_selected (96x96): the gun is out.")]
        [SerializeField] private Texture2D iconSelected;

        private static HudPistolIcon active;

        // The GM instance is persistent: once it has risen it stays up in every later room.
        private static bool risenThisRun;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            active = null;
            risenThisRun = false;
        }

        public PlayerLocomotion Player { get => player; set => player = value; }

        public ProgramIconSlide Slide { get; } = new ProgramIconSlide(IconSize);

        /// <summary>The gun is out (any aim mode but "off"): the icon shows spr_icon_pistol_selected.</summary>
        public bool Selected => player != null && player.Entity != null && player.Entity.Weapon.Mode != PlayerWeaponState.AimMode.Off;

        /// <summary>The texture currently drawn.</summary>
        public Texture2D CurrentTexture => Selected ? iconSelected : icon;

        /// <summary>Whether <paramref name="screenPoint"/> (bottom-left origin) is over the
        /// on-screen pistol icon.</summary>
        public static bool IsOverIcon(Vector2 screenPoint) => active != null && active.Contains(screenPoint);

        private VisualElement element;

        private void OnEnable()
        {
            active = this;
            HudRoot.EnsureInstalled();
            element = new VisualElement { pickingMode = PickingMode.Ignore };
            element.style.position = Position.Absolute;
            element.style.width = IconSize;
            element.style.height = IconSize;
            element.style.right = RightMargin;
            element.style.backgroundSize = new BackgroundSize(Length.Percent(100f), Length.Percent(100f));
            element.style.display = DisplayStyle.None;
            HudRoot.HudIcons.Add(element);
        }

        private void OnDisable()
        {
            if (active == this) active = null;
            element?.RemoveFromHierarchy();
        }

        private void Start()
        {
            if (risenThisRun) Slide.Settle();
            else Slide.SlideIn();
            risenThisRun = true;
        }

        private void Update()
        {
            if (GameInput.Current.LeftClickPressed)
            {
                Vector2 p = GameInput.PointerScreenPosition;
                var hit = MouseGui.Resolve(HackingOverlay.IsOverProgramIcon(p), HackingOverlay.IsOverConsole(p), Contains(p));
                if (hit == GuiClick.PistolIcon) Click();
            }

            Slide.Step(Time.deltaTime);
            Apply();
        }

        /// <summary>obj_mouse_gui's pistol branch: <c>toggle_gun()</c>, sent as this player's
        /// <see cref="ToggleGun"/> command.</summary>
        public void Click()
        {
            if (player != null && player.Session != null) player.Send(new ToggleGun(player.Id));
        }

        /// <summary>The icon's screen rect in GUI coordinates (top-left origin): anchored 40px in
        /// from the right edge, at the slide's y, scaled like the HUD panel (height-matched).</summary>
        public Rect ScreenRect
        {
            get
            {
                float scale = Screen.height / 1080f;
                return new Rect(Screen.width - (RightMargin + IconSize) * scale, Slide.Y * scale, IconSize * scale, IconSize * scale);
            }
        }

        private bool Contains(Vector2 screenPoint)
        {
            return Slide.Visible && ScreenRect.Contains(new Vector2(screenPoint.x, Screen.height - screenPoint.y));
        }

        private void Apply()
        {
            if (element == null) return;
            Texture2D texture = CurrentTexture;
            if (Slide.Visible && texture != null)
            {
                element.style.backgroundImage = new StyleBackground(texture);
                element.style.top = Slide.Y;
                element.style.display = DisplayStyle.Flex;
            }
            else
            {
                element.style.display = DisplayStyle.None;
            }
        }
    }
}

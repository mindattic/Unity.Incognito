using Incognito.Eye;
using Incognito.Player;
using UnityEngine;

namespace Incognito.Weapon
{
    /// <summary>
    /// Drives <see cref="PlayerWeaponState"/> from input and applies it to the two arm sprites,
    /// fired bullets and sounds. See docs/RECONSTRUCTION.md §1. Z toggles draw/holster, the right
    /// mouse button held aims (walking locked), the left button fires while aiming.
    /// Also raises the U.INC-T5 <c>Aiming</c> flag on the eye cursor (reticle) and the player's
    /// eyeball (fixed gaze). The HUD pistol icon that also calls ToggleGun belongs to U.INC-T7.
    /// Runs before <see cref="PlayerLocomotion"/> so the movement lock applies the same frame.
    /// </summary>
    [DefaultExecutionOrder(-10)]
    [RequireComponent(typeof(PlayerLocomotion))]
    public sealed class PlayerWeapon : MonoBehaviour
    {
        [Header("Arms (children of the player)")]
        [Tooltip("arm1: drawn in front of the body.")]
        [SerializeField] private SpriteRenderer supportArm;
        [Tooltip("arm2: drawn behind the body; the bullet leaves from this arm.")]
        [SerializeField] private SpriteRenderer gunArm;
        [Tooltip("Arm pivot relative to the player origin while facing left, original px, y up " +
                 "(obj_arm: x = player.x + 60, y = player.y + 220). Mirrored when facing right.")]
        [SerializeField] private Vector2 shoulderOffset = new Vector2(60f, -220f);

        [Header("Pose sprites")]
        [SerializeField] private Sprite restArm;
        [SerializeField] private Sprite supportAimRaise;
        [SerializeField] private Sprite supportAimHold;
        [SerializeField] private Sprite gunDrawing;
        [SerializeField] private Sprite gunAim;
        [SerializeField] private Sprite gunFire;

        [Header("Bullet")]
        [SerializeField] private Sprite bulletSprite;
        [SerializeField] private int bulletSortingOrder = -2;
        [Tooltip("Original px per step (shoot_bullet.gml: speed = 60).")]
        [SerializeField] private float bulletSpeed = 60f;
        [Tooltip("World-space room rectangle; bullets leaving it are removed.")]
        [SerializeField] private Rect room = new Rect(0f, 0f, 360f, 33.75f);
        [Tooltip("Optional effect spawned at the muzzle (the original's ef_smoke).")]
        [SerializeField] private GameObject muzzleEffect;

        [Header("Audio (optional until U.INC-T11 imports the original sounds)")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip reloadClip;
        [SerializeField] private AudioClip holsterClip;
        [SerializeField] private AudioClip whooshClip;
        [SerializeField] private AudioClip fireClip;

        [Header("Input")]
        [SerializeField] private KeyCode toggleKey = KeyCode.Z;

        [Header("Eyes (U.INC-T5): reticle cursor and fixed aiming gaze")]
        [SerializeField] private EyeCursor eyeCursor;
        [SerializeField] private PlayerEyeball playerEyeball;

        public PlayerWeaponState State { get; private set; }

        private PlayerLocomotion locomotion;

        private void Awake()
        {
            locomotion = GetComponent<PlayerLocomotion>();
            State = new PlayerWeaponState();
            State.OnSound += PlaySound;
        }

        private void Update()
        {
            var facing = locomotion.State.Facing;

            if (Input.GetKeyDown(toggleKey)) State.ToggleGun();
            if (Input.GetMouseButtonDown(1)) State.AimPressed();
            if (Input.GetMouseButtonUp(1)) State.AimReleased();
            if (Input.GetMouseButtonDown(0) && State.FirePressed(facing, out var spawn)) Fire(spawn);
            if (Input.GetMouseButtonUp(0)) State.FireReleased();

            locomotion.MovementLocked = State.IsAiming;
            if (eyeCursor != null) eyeCursor.Aiming = State.IsAiming;
            if (playerEyeball != null) playerEyeball.Aiming = State.IsAiming;
        }

        // LateUpdate: after locomotion has settled this frame's walk mode, facing and bob.
        private void LateUpdate()
        {
            bool sprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            State.Step(Time.deltaTime, locomotion.State.CurrentMode, locomotion.State.Facing, sprint, MouseBelowCentre());
            ApplyArms();
        }

        private void ApplyArms()
        {
            bool facingRight = locomotion.State.Facing == PlayerLocomotionState.FacingDirection.Right;
            const float px = PlayerLocomotion.WorldUnitsPerOriginalPixel;
            var local = new Vector3((facingRight ? -shoulderOffset.x : shoulderOffset.x) * px, shoulderOffset.y * px, 0f);

            ApplyArm(supportArm, local, facingRight, State.SupportArm.Angle, SupportSprite());
            ApplyArm(gunArm, local, facingRight, State.GunArm.Angle, GunSprite());
        }

        private static void ApplyArm(SpriteRenderer arm, Vector3 local, bool facingRight, float angle, Sprite sprite)
        {
            if (arm == null) return;
            arm.transform.localPosition = new Vector3(local.x, local.y, arm.transform.localPosition.z);
            arm.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            arm.flipX = facingRight;
            if (sprite != null) arm.sprite = sprite;
        }

        private Sprite SupportSprite() => State.SupportPose switch
        {
            PlayerWeaponState.SupportArmPose.AimRaise => supportAimRaise,
            PlayerWeaponState.SupportArmPose.AimHold => supportAimHold,
            _ => restArm,
        };

        private Sprite GunSprite() => State.GunPose switch
        {
            PlayerWeaponState.GunArmPose.Drawing => gunDrawing,
            PlayerWeaponState.GunArmPose.Aim => gunAim,
            PlayerWeaponState.GunArmPose.Fire => gunFire,
            _ => restArm,
        };

        private void Fire(PlayerWeaponState.BulletSpawn spawn)
        {
            const float px = PlayerLocomotion.WorldUnitsPerOriginalPixel;
            Vector3 pivot = gunArm != null ? gunArm.transform.position : transform.position;
            var muzzle = new Vector2(pivot.x + spawn.Offset.x * px, pivot.y + spawn.Offset.y * px);

            var go = new GameObject("Bullet", typeof(SpriteRenderer));
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = bulletSprite;
            sr.sortingOrder = bulletSortingOrder;
            go.AddComponent<Bullet>().Launch(muzzle, spawn.Direction, bulletSpeed * px, room);

            if (muzzleEffect != null) Instantiate(muzzleEffect, new Vector3(muzzle.x, muzzle.y, 0f), Quaternion.identity);
        }

        private void PlaySound(PlayerWeaponState.WeaponSound sound)
        {
            if (audioSource == null) return;
            AudioClip clip = sound switch
            {
                PlayerWeaponState.WeaponSound.Reload => reloadClip,
                PlayerWeaponState.WeaponSound.Holster => holsterClip,
                PlayerWeaponState.WeaponSound.Whoosh => whooshClip,
                _ => fireClip,
            };
            if (clip != null) audioSource.PlayOneShot(clip);
        }

        /// <summary>Cursor distance below the view's middle, in original px (GM: mouse_y - room_height/2).
        /// The original room was exactly one view tall, so this is screen-relative.</summary>
        private float MouseBelowCentre()
        {
            if (Screen.height <= 0) return 0f;
            float fromTop = 1f - Input.mousePosition.y / Screen.height;
            return (fromTop - 0.5f) * State.ViewHeight;
        }
    }
}

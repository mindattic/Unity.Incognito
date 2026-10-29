using Incognito.Controls;
using Incognito.Player;
using Incognito.World;
using UnityEngine;

namespace Incognito.Weapon
{
    /// <summary>
    /// Sends the weapon input as commands (<see cref="ToggleGun"/>, <see cref="PressAim"/>,
    /// <see cref="PressFire"/> and their releases) and plays what the world produces: it applies the
    /// world's <see cref="PlayerWeaponState"/> to the two arm sprites and spawns a bullet view and a
    /// <see cref="MuzzleSmoke"/> puff for each <see cref="BulletFired"/>, and plays each
    /// <see cref="WeaponSounded"/>. See docs/RECONSTRUCTION.md §1.
    /// Z toggles draw/holster, the right mouse button held aims (walking locked), the left button fires
    /// while aiming. The eye cursor and eyeball read the aim from the world themselves (U.INC-T5).
    /// The HUD pistol icon that also toggles the gun is <see cref="HudPistolIcon"/> (U.INC-T30).
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

        [Header("Muzzle smoke")]
        [SerializeField] private string muzzleSmokeSortingLayer = "Default";
        [Tooltip("effect_create_below drew ef_smoke below every instance in the room, not just the " +
                 "player: fixed well below the body (0), both arms (-4..1) and the bullet (-2), " +
                 "rather than relative to any one of them.")]
        [SerializeField] private int muzzleSmokeSortingOrder = -50;

        [Header("Audio (optional until U.INC-T11 imports the original sounds)")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip reloadClip;
        [SerializeField] private AudioClip holsterClip;
        [SerializeField] private AudioClip whooshClip;
        [SerializeField] private AudioClip fireClip;

        /// <summary>This player's weapon in the world; read it, never write it.</summary>
        public PlayerWeaponState State => locomotion.Entity?.Weapon;

        /// <summary>Sets the room-specific sound clips and bullet-despawn rect (U.INC-T26's
        /// RoomComposer); the arm/bullet sprites and support audio source stay the sandbox rig's own.</summary>
        public void Configure(AudioClip reloadClip, AudioClip holsterClip, AudioClip whooshClip, AudioClip fireClip, Rect room)
        {
            this.reloadClip = reloadClip;
            this.holsterClip = holsterClip;
            this.whooshClip = whooshClip;
            this.fireClip = fireClip;
            this.room = room;
        }

        private PlayerLocomotion locomotion;
        private GameSession session;

        private void Awake()
        {
            locomotion = GetComponent<PlayerLocomotion>();
        }

        // Start, not OnEnable: the seat (and so the session) is set in PlayerLocomotion.Awake.
        private void Start()
        {
            session = locomotion.Session;
            if (session != null) session.Played += OnPlayed;
        }

        private void OnDestroy()
        {
            if (session != null) session.Played -= OnPlayed;
        }

        private void Update()
        {
            if (session == null) return;
            var id = locomotion.Id;

            if (GameInput.Current.DrawPressed) session.Submit(new ToggleGun(id));
            if (GameInput.Current.AimPressed) session.Submit(new PressAim(id));
            if (GameInput.Current.AimReleased) session.Submit(new ReleaseAim(id));
            if (GameInput.Current.LeftClickPressed) session.Submit(new PressFire(id));
            if (GameInput.Current.FireReleased) session.Submit(new ReleaseFire(id));
        }

        // After the session has stepped the world this frame.
        private void LateUpdate()
        {
            if (State != null) ApplyArms();
        }

        private void OnPlayed(GameEvent e)
        {
            switch (e)
            {
                case BulletFired fired when fired.Player == locomotion.Id:
                    Fire(fired.Spawn);
                    break;
                case WeaponSounded sounded when sounded.Player == locomotion.Id:
                    PlaySound(sounded.Sound);
                    break;
            }
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
            go.AddComponent<Bullet>().Launch(muzzle, spawn.Direction, bulletSpeed * px, room, locomotion.Id);

            // shoot_bullet.gml: effect_create_below(ef_smoke, originX, originY, 1, c_black) — a
            // fixed sorting order below every actor sprite (body, arms, NPCs, the bullet), the same
            // "below every instance" depth the original's effect_create_below gave it (U.INC-T41).
            MuzzleSmoke.Puff(new Vector3(muzzle.x, muzzle.y, 0f), muzzleSmokeSortingLayer, muzzleSmokeSortingOrder);
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
    }
}

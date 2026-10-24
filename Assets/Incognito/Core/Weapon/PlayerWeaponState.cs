using System;
using Incognito.Player;
using Incognito.Settings;

namespace Incognito.Weapon
{
    /// <summary>
    /// Pure, engine-loop-free simulation of the original weapon handling (RECONSTRUCTION.md §1):
    /// obj_walker's aimmode state machine and mouse events, toggle_gun.gml, shoot_bullet.gml and
    /// both obj_arm instances. Units are original GameMaker pixels; angles are image_angle degrees
    /// (counter-clockwise on screen). Offsets returned here are y-up (Unity sense).
    ///
    /// Inputs, as in the original:
    ///  - <see cref="ToggleGun"/> (Z key, or the HUD pistol icon): Off -> Equip (draw, sfx_reload),
    ///    anything else -> Off (holster, sfx_holster).
    ///  - <see cref="AimPressed"/> (right button down): Equip -> Aiming (sfx_whoosh, arms zeroed,
    ///    support arm raises, 5 steps later both arms settle into the aim pose). Walking is locked
    ///    while aiming.
    ///  - <see cref="AimReleased"/> (right button up): Aiming -> Equip.
    ///  - <see cref="FirePressed"/> (left button down, aiming only): fire pose + one bullet.
    ///  - <see cref="FireReleased"/> (left button up, aiming only): back to the aim pose.
    /// </summary>
    public sealed class PlayerWeaponState
    {
        public enum AimMode { Off, Equip, Aiming }

        /// <summary>arm1 (drawn in front of the body): spr_arm / spr_arm_left_aim1 / spr_arm_left_aim2.</summary>
        public enum SupportArmPose { Rest, AimRaise, AimHold }

        /// <summary>arm2 (drawn behind the body): spr_arm / spr_arm_right_drawing / spr_arm_right_aim / spr_arm_right_fire.</summary>
        public enum GunArmPose { Rest, Drawing, Aim, Fire }

        public enum WeaponSound { Reload, Holster, Whoosh, Fire }

        /// <summary>Where a new bullet starts, relative to the gun arm's pivot (original px, y up),
        /// and which way it travels (degrees, counter-clockwise from +x).</summary>
        public readonly struct BulletSpawn
        {
            public readonly Vec2 Offset;
            public readonly float Direction;

            public BulletSpawn(Vec2 offset, float direction)
            {
                Offset = offset;
                Direction = direction;
            }
        }

        public const float ReferenceFps = PlayerLocomotionState.ReferenceFps;

        /// <summary>Original: alarm[0] = 5 between the right-click and the settled aim pose.</summary>
        public float AimRaiseSteps = 5f;

        /// <summary>Original room/view height; the aim angle is measured from its middle.</summary>
        public float ViewHeight = 1080f;

        /// <summary>Original: image_angle = (mouse_y - room_height/2) / 30.</summary>
        public float AimDivisor = 30f;

        /// <summary>shoot_bullet.gml: Len = 500 along the arm from its pivot...</summary>
        public float MuzzleReach = 500f;

        /// <summary>...then 88px up (unrotated).</summary>
        public float MuzzleRise = 88f;

        /// <summary>obj_arm defaults, and obj_walker's walk-key values.</summary>
        public float WalkSwingRange = 3f;
        public float WalkRotSpeed = 0.06f;

        /// <summary>obj_walker's Shift-held values.</summary>
        public float SprintSwingRange = 6f;
        public float SprintRotSpeed = 0.24f;

        public AimMode Mode { get; private set; } = AimMode.Off;
        public SupportArmPose SupportPose { get; private set; } = SupportArmPose.Rest;
        public GunArmPose GunPose { get; private set; } = GunArmPose.Rest;

        /// <summary>arm1: swing_forward starts true.</summary>
        public ArmSwingState SupportArm { get; } = new ArmSwingState(startForward: true);

        /// <summary>arm2: swing_forward starts false, so the arms swing in opposition.</summary>
        public ArmSwingState GunArm { get; } = new ArmSwingState(startForward: false);

        public bool IsAiming => Mode == AimMode.Aiming;
        public bool IsRaising => raiseStepsLeft > 0f;

        public event Action<WeaponSound> OnSound;

        private float raiseStepsLeft;
        private float rotSpeed;

        /// <summary>
        /// Builds from a validated <see cref="WeaponTuning"/> (U.INC-T22); null takes the
        /// original's own values, same as every field's own default above.
        /// </summary>
        public PlayerWeaponState(WeaponTuning tuning = null)
        {
            WeaponTuning t = tuning ?? new WeaponTuning();
            AimRaiseSteps = t.AimRaiseSteps;
            ViewHeight = t.ViewHeight;
            AimDivisor = t.AimDivisor;
            MuzzleReach = t.MuzzleReach;
            MuzzleRise = t.MuzzleRise;
            WalkSwingRange = t.WalkSwingRange;
            WalkRotSpeed = t.WalkRotSpeed;
            SprintSwingRange = t.SprintSwingRange;
            SprintRotSpeed = t.SprintRotSpeed;

            rotSpeed = WalkRotSpeed;
        }

        /// <summary>toggle_gun.gml.</summary>
        public void ToggleGun()
        {
            raiseStepsLeft = 0f;
            if (Mode == AimMode.Off)
            {
                Mode = AimMode.Equip;
                GunPose = GunArmPose.Drawing;
                OnSound?.Invoke(WeaponSound.Reload);
            }
            else
            {
                Mode = AimMode.Off;
                SupportPose = SupportArmPose.Rest;
                GunPose = GunArmPose.Rest;
                OnSound?.Invoke(WeaponSound.Holster);
            }
        }

        /// <summary>obj_walker global right-pressed. Returns true when aiming starts.</summary>
        public bool AimPressed()
        {
            if (Mode != AimMode.Equip) return false;

            Mode = AimMode.Aiming;
            SupportPose = SupportArmPose.AimRaise;
            SupportArm.Set(0f);
            GunArm.Set(0f);
            raiseStepsLeft = AimRaiseSteps;
            OnSound?.Invoke(WeaponSound.Whoosh);
            return true;
        }

        /// <summary>obj_walker global right-released.</summary>
        public void AimReleased()
        {
            if (Mode != AimMode.Aiming) return;

            Mode = AimMode.Equip;
            GunPose = GunArmPose.Drawing;
            SupportPose = SupportArmPose.Rest;
            // The original left alarm[0] running, so a quick tap snapped the arms into the aim
            // pose while no longer aiming. Cancelled here (an incidental GM quirk, not a mechanic).
            raiseStepsLeft = 0f;
        }

        /// <summary>obj_walker global left-pressed + shoot_bullet.gml. Fires only while aiming.</summary>
        public bool FirePressed(PlayerLocomotionState.FacingDirection facing, out BulletSpawn spawn)
        {
            if (Mode != AimMode.Aiming)
            {
                spawn = default;
                return false;
            }

            GunPose = GunArmPose.Fire;
            spawn = ComputeSpawn(facing, GunArm.Angle);
            OnSound?.Invoke(WeaponSound.Fire);
            return true;
        }

        /// <summary>obj_walker global left-released.</summary>
        public void FireReleased()
        {
            if (Mode == AimMode.Aiming) GunPose = GunArmPose.Aim;
        }

        /// <summary>
        /// One frame. <paramref name="mouseBelowCentre"/> is the cursor's distance below the middle
        /// of the view in original pixels (GM: mouse_y - room_height/2; negative = above).
        /// </summary>
        public void Step(
            float deltaTime,
            PlayerLocomotionState.WalkMode walkMode,
            PlayerLocomotionState.FacingDirection facing,
            bool sprint,
            float mouseBelowCentre)
        {
            float steps = CoreMath.Max(deltaTime, 0f) * ReferenceFps;

            if (raiseStepsLeft > 0f)
            {
                raiseStepsLeft -= steps;
                if (raiseStepsLeft <= 0f)
                {
                    raiseStepsLeft = 0f;
                    SupportPose = SupportArmPose.AimHold;
                    GunPose = GunArmPose.Aim;
                }
            }

            if (Mode == AimMode.Aiming)
            {
                float angle = AimAngle(facing, mouseBelowCentre);
                SupportArm.Set(angle);
                GunArm.Set(angle);
                return;
            }

            switch (walkMode)
            {
                case PlayerLocomotionState.WalkMode.Walking:
                    // The original only re-tuned the swing on the left-walk key (a copy-paste
                    // gap); applied to both directions here.
                    float range = sprint ? SprintSwingRange : WalkSwingRange;
                    rotSpeed = sprint ? SprintRotSpeed : WalkRotSpeed;
                    SupportArm.Swing(steps, range, rotSpeed);
                    GunArm.Swing(steps, range, rotSpeed);
                    break;
                case PlayerLocomotionState.WalkMode.Idle:
                    SupportArm.Relax(steps, rotSpeed);
                    GunArm.Relax(steps, rotSpeed);
                    break;
                // Stopping: the original's arm step has no branch for it — the arms hold.
            }
        }

        /// <summary>obj_arm while aiming: the arm points at the cursor, mirrored by facing.</summary>
        public float AimAngle(PlayerLocomotionState.FacingDirection facing, float mouseBelowCentre)
        {
            float half = ViewHeight * 0.5f;
            float m = CoreMath.Clamp(mouseBelowCentre, -half, half);
            return facing == PlayerLocomotionState.FacingDirection.Left ? m / AimDivisor : -m / AimDivisor;
        }

        /// <summary>shoot_bullet.gml, converted to y-up.</summary>
        public BulletSpawn ComputeSpawn(PlayerLocomotionState.FacingDirection facing, float armAngle)
        {
            bool left = facing == PlayerLocomotionState.FacingDirection.Left;
            float reach = left ? -MuzzleReach : MuzzleReach;
            float rad = armAngle * CoreMath.Deg2Rad;
            var offset = new Vec2(reach * CoreMath.Cos(rad), reach * CoreMath.Sin(rad) + MuzzleRise);
            float direction = left ? armAngle - 180f : armAngle;
            return new BulletSpawn(offset, direction);
        }
    }
}

// NUnit 4 moved the classic Assert; the tests were written against it (Unity Test Framework).
using Assert = NUnit.Framework.Legacy.ClassicAssert;
using CollectionAssert = NUnit.Framework.Legacy.CollectionAssert;
using StringAssert = NUnit.Framework.Legacy.StringAssert;
using System.Collections.Generic;
using Incognito.Player;
using Incognito.Weapon;
using NUnit.Framework;

namespace Incognito.Tests
{
    public class PlayerWeaponStateTests
    {
        private const float Dt = 1f / 60f; // one original step
        private const PlayerLocomotionState.FacingDirection Left = PlayerLocomotionState.FacingDirection.Left;
        private const PlayerLocomotionState.FacingDirection Right = PlayerLocomotionState.FacingDirection.Right;
        private const PlayerLocomotionState.WalkMode Idle = PlayerLocomotionState.WalkMode.Idle;
        private const PlayerLocomotionState.WalkMode Walking = PlayerLocomotionState.WalkMode.Walking;
        private const PlayerLocomotionState.WalkMode Stopping = PlayerLocomotionState.WalkMode.Stopping;

        private static PlayerWeaponState Aiming(out List<PlayerWeaponState.WeaponSound> sounds)
        {
            var state = new PlayerWeaponState();
            var heard = new List<PlayerWeaponState.WeaponSound>();
            state.OnSound += heard.Add;
            state.ToggleGun();
            state.AimPressed();
            sounds = heard;
            return state;
        }

        [Test]
        public void Starts_holstered_with_resting_arms()
        {
            var state = new PlayerWeaponState();
            Assert.AreEqual(PlayerWeaponState.AimMode.Off, state.Mode);
            Assert.AreEqual(PlayerWeaponState.SupportArmPose.Rest, state.SupportPose);
            Assert.AreEqual(PlayerWeaponState.GunArmPose.Rest, state.GunPose);
            Assert.IsFalse(state.IsAiming);
        }

        [Test]
        public void Toggle_draws_then_holsters_with_their_sounds()
        {
            var state = new PlayerWeaponState();
            var sounds = new List<PlayerWeaponState.WeaponSound>();
            state.OnSound += sounds.Add;

            state.ToggleGun();
            Assert.AreEqual(PlayerWeaponState.AimMode.Equip, state.Mode);
            Assert.AreEqual(PlayerWeaponState.GunArmPose.Drawing, state.GunPose);

            state.ToggleGun();
            Assert.AreEqual(PlayerWeaponState.AimMode.Off, state.Mode);
            Assert.AreEqual(PlayerWeaponState.GunArmPose.Rest, state.GunPose);

            CollectionAssert.AreEqual(
                new[] { PlayerWeaponState.WeaponSound.Reload, PlayerWeaponState.WeaponSound.Holster }, sounds);
        }

        [Test]
        public void Aim_requires_the_gun_to_be_drawn()
        {
            var state = new PlayerWeaponState();
            Assert.IsFalse(state.AimPressed());
            Assert.AreEqual(PlayerWeaponState.AimMode.Off, state.Mode);
        }

        [Test]
        public void Aim_raises_the_support_arm_then_settles_after_five_steps()
        {
            var state = Aiming(out var sounds);
            Assert.IsTrue(state.IsAiming);
            Assert.AreEqual(PlayerWeaponState.WeaponSound.Whoosh, sounds[sounds.Count - 1]);
            Assert.AreEqual(PlayerWeaponState.SupportArmPose.AimRaise, state.SupportPose);
            Assert.AreEqual(PlayerWeaponState.GunArmPose.Drawing, state.GunPose);

            for (int i = 0; i < 4; i++) state.Step(Dt, Idle, Left, false, 0f);
            Assert.AreEqual(PlayerWeaponState.SupportArmPose.AimRaise, state.SupportPose, "still raising after 4 steps");

            state.Step(Dt, Idle, Left, false, 0f);
            Assert.AreEqual(PlayerWeaponState.SupportArmPose.AimHold, state.SupportPose);
            Assert.AreEqual(PlayerWeaponState.GunArmPose.Aim, state.GunPose);
        }

        [Test]
        public void Releasing_aim_returns_to_equip_and_cancels_the_raise()
        {
            var state = Aiming(out _);
            state.AimReleased();
            for (int i = 0; i < 10; i++) state.Step(Dt, Idle, Left, false, 0f);

            Assert.AreEqual(PlayerWeaponState.AimMode.Equip, state.Mode);
            Assert.AreEqual(PlayerWeaponState.GunArmPose.Drawing, state.GunPose);
            Assert.AreEqual(PlayerWeaponState.SupportArmPose.Rest, state.SupportPose);
        }

        [Test]
        public void Holstering_while_aiming_drops_straight_to_off()
        {
            var state = Aiming(out _);
            state.ToggleGun();
            Assert.AreEqual(PlayerWeaponState.AimMode.Off, state.Mode);
            Assert.AreEqual(PlayerWeaponState.GunArmPose.Rest, state.GunPose);
            Assert.AreEqual(PlayerWeaponState.SupportArmPose.Rest, state.SupportPose);
        }

        [Test]
        public void Fire_only_while_aiming()
        {
            var state = new PlayerWeaponState();
            Assert.IsFalse(state.FirePressed(Left, out _));
            state.ToggleGun();
            Assert.IsFalse(state.FirePressed(Left, out _), "drawn but not aiming must not fire");
        }

        [Test]
        public void Fire_switches_to_fire_pose_and_release_back_to_aim()
        {
            var state = Aiming(out var sounds);
            for (int i = 0; i < 5; i++) state.Step(Dt, Idle, Left, false, 0f);

            Assert.IsTrue(state.FirePressed(Left, out _));
            Assert.AreEqual(PlayerWeaponState.GunArmPose.Fire, state.GunPose);
            Assert.AreEqual(PlayerWeaponState.WeaponSound.Fire, sounds[sounds.Count - 1]);

            state.FireReleased();
            Assert.AreEqual(PlayerWeaponState.GunArmPose.Aim, state.GunPose);
        }

        [Test]
        public void Aim_angle_follows_mouse_and_mirrors_with_facing()
        {
            var state = Aiming(out _);

            // 300px below centre, facing left: (mouse_y - 540) / 30 = +10 deg.
            state.Step(Dt, Idle, Left, false, 300f);
            Assert.AreEqual(10f, state.GunArm.Angle, 1e-4f);
            Assert.AreEqual(10f, state.SupportArm.Angle, 1e-4f);

            // Facing right mirrors it: (540 - mouse_y) / 30 = -10 deg.
            state.Step(Dt, Idle, Right, false, 300f);
            Assert.AreEqual(-10f, state.GunArm.Angle, 1e-4f);

            // Clamped to the view: never more than half the view (540/30 = 18 deg).
            state.Step(Dt, Idle, Left, false, 5000f);
            Assert.AreEqual(18f, state.GunArm.Angle, 1e-4f);
        }

        [Test]
        public void Level_shot_leaves_500px_ahead_and_88px_up_travelling_the_facing_way()
        {
            var state = new PlayerWeaponState();

            var left = state.ComputeSpawn(Left, 0f);
            Assert.AreEqual(-500f, left.Offset.x, 1e-3f);
            Assert.AreEqual(88f, left.Offset.y, 1e-3f);
            Assert.AreEqual(-180f, left.Direction, 1e-4f);

            var right = state.ComputeSpawn(Right, 0f);
            Assert.AreEqual(500f, right.Offset.x, 1e-3f);
            Assert.AreEqual(0f, right.Direction, 1e-4f);
        }

        [Test]
        public void Aiming_down_sends_the_bullet_down_either_way_you_face()
        {
            var state = new PlayerWeaponState();

            // Mouse below centre -> facing left angle +10, facing right angle -10.
            var left = state.ComputeSpawn(Left, state.AimAngle(Left, 300f));
            var right = state.ComputeSpawn(Right, state.AimAngle(Right, 300f));

            var leftTravel = new BulletState(Vec2.Zero, left.Direction, 60f).VelocityPerStep;
            var rightTravel = new BulletState(Vec2.Zero, right.Direction, 60f).VelocityPerStep;

            Assert.Less(leftTravel.x, 0f);
            Assert.Less(leftTravel.y, 0f);
            Assert.Greater(rightTravel.x, 0f);
            Assert.Less(rightTravel.y, 0f);
            Assert.Less(left.Offset.y, 88f, "muzzle drops with the arm");
        }

        [Test]
        public void Arms_swing_in_opposition_while_walking_and_relax_when_idle()
        {
            var state = new PlayerWeaponState();
            float supportMin = 0f, gunMax = 0f;

            for (int i = 0; i < 40; i++)
            {
                state.Step(Dt, Walking, Left, false, 0f);
                supportMin = CoreMath.Min(supportMin, state.SupportArm.Angle);
                gunMax = CoreMath.Max(gunMax, state.GunArm.Angle);
            }

            Assert.Less(supportMin, 0f, "arm1 starts swinging forward (clockwise)");
            Assert.Greater(gunMax, 0f, "arm2 starts swinging back (counter-clockwise)");

            for (int i = 0; i < 200; i++) state.Step(Dt, Idle, Left, false, 0f);
            Assert.AreEqual(0f, state.SupportArm.Angle);
            Assert.AreEqual(0f, state.GunArm.Angle);
        }

        [Test]
        public void Walk_swing_stays_within_its_range_and_sprint_widens_it()
        {
            var walk = new PlayerWeaponState();
            var sprint = new PlayerWeaponState();
            float walkPeak = 0f, sprintPeak = 0f;

            for (int i = 0; i < 600; i++)
            {
                walk.Step(Dt, Walking, Left, false, 0f);
                sprint.Step(Dt, Walking, Left, true, 0f);
                walkPeak = CoreMath.Max(walkPeak, CoreMath.Abs(walk.GunArm.Angle));
                sprintPeak = CoreMath.Max(sprintPeak, CoreMath.Abs(sprint.GunArm.Angle));
            }

            Assert.AreEqual(3f, walkPeak, 0.1f);
            Assert.AreEqual(6f, sprintPeak, 0.3f);
        }

        [Test]
        public void Arms_hold_while_stopping()
        {
            var state = new PlayerWeaponState();
            for (int i = 0; i < 10; i++) state.Step(Dt, Walking, Left, false, 0f);
            float held = state.GunArm.Angle;

            state.Step(Dt, Stopping, Left, false, 0f);
            Assert.AreEqual(held, state.GunArm.Angle);
        }

        [Test]
        public void Entering_aim_zeroes_the_arms()
        {
            var state = new PlayerWeaponState();
            for (int i = 0; i < 10; i++) state.Step(Dt, Walking, Left, false, 0f);
            Assert.AreNotEqual(0f, state.GunArm.Angle);

            state.ToggleGun();
            state.AimPressed();
            Assert.AreEqual(0f, state.GunArm.Angle);
            Assert.AreEqual(0f, state.SupportArm.Angle);
        }

        [Test]
        public void Locomotion_force_idle_stops_dead()
        {
            var loco = new PlayerLocomotionState();
            loco.Step(Dt, moveLeft: false, moveRight: true, sprint: false);
            loco.Step(Dt, moveLeft: false, moveRight: true, sprint: false);

            loco.ForceIdle();
            Assert.AreEqual(Idle, loco.CurrentMode);
            Assert.AreEqual(0f, loco.HorizontalSpeed);
            Assert.AreEqual(0f, loco.BobOffset);
        }
    }
}

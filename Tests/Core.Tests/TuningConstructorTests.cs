// NUnit 4 moved the classic Assert; the tests were written against it (Unity Test Framework).
using Assert = NUnit.Framework.Legacy.ClassicAssert;
using Incognito.CameraRig;
using Incognito.Player;
using Incognito.Settings;
using Incognito.Weapon;
using Incognito.World;
using NUnit.Framework;

namespace Incognito.Tests
{
    /// <summary>
    /// U.INC-T22: every *State class that takes a validated Tuning in its constructor applies it,
    /// and a null tuning still falls back to the original's own values (so existing callers and
    /// object-initializer tests are unaffected).
    /// </summary>
    public class TuningConstructorTests
    {
        [Test]
        public void Locomotion_state_applies_a_custom_tuning()
        {
            var state = new PlayerLocomotionState(new LocomotionTuning { WalkSpeed = 9f, SprintMultiplier = 3f });
            Assert.AreEqual(9f, state.WalkSpeed);
            Assert.AreEqual(3f, state.SprintMultiplier);
        }

        [Test]
        public void Locomotion_state_with_null_tuning_keeps_the_originals_values()
        {
            var state = new PlayerLocomotionState(null);
            Assert.AreEqual(5f, state.BobAmplitude);
            Assert.AreEqual(4f, state.WalkSpeed);
        }

        [Test]
        public void Camera_state_applies_a_custom_tuning()
        {
            var state = new CloseFramingCameraState(new CameraTuning { ViewWidth = 800f, MaxVerticalPan = 10f });
            Assert.AreEqual(800f, state.ViewWidth);
            Assert.AreEqual(10f, state.MaxVerticalPan);
        }

        [Test]
        public void Weapon_state_applies_a_custom_tuning()
        {
            var state = new PlayerWeaponState(new WeaponTuning { MuzzleReach = 123f, AimDivisor = 2f });
            var spawn = state.ComputeSpawn(PlayerLocomotionState.FacingDirection.Right, 0f);
            Assert.AreEqual(123f, spawn.Offset.x, 1e-3f);
        }

        [Test]
        public void Rain_state_applies_a_custom_tuning()
        {
            var tuning = new RainTuning { DropsPerStep = 2, RoomHeight = 500f, DriftMin = -1, DriftMax = -1, FallMin = 10, FallMax = 10, Alpha = 0.5f };
            var rain = new RainState(tuning: tuning);
            rain.Step(1, 0f, 100f);

            Assert.AreEqual(2, rain.Drops.Count);
            Assert.AreEqual(0.5f, rain.Alpha);
            foreach (var drop in rain.Drops)
            {
                Assert.AreEqual(-1f, drop.DX);
                Assert.AreEqual(10f, drop.DY);
            }
        }
    }
}

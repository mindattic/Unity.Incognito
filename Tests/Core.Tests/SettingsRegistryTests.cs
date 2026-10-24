// NUnit 4 moved the classic Assert; the tests were written against it (Unity Test Framework).
using Assert = NUnit.Framework.Legacy.ClassicAssert;
using Incognito.Npc;
using Incognito.Settings;
using NUnit.Framework;

namespace Incognito.Tests
{
    public class LocomotionTuningTests
    {
        [Test]
        public void Default_values_validate()
        {
            Assert.DoesNotThrow(() => new LocomotionTuning().Validate());
        }

        [Test]
        public void Negative_bob_amplitude_is_rejected()
        {
            var tuning = new LocomotionTuning { BobAmplitude = -1f };
            Assert.Throws<SettingsValidationException>(() => tuning.Validate());
        }

        [Test]
        public void Sprint_multiplier_below_one_is_rejected()
        {
            var tuning = new LocomotionTuning { SprintMultiplier = 0.5f };
            Assert.Throws<SettingsValidationException>(() => tuning.Validate());
        }

        [Test]
        public void Friction_outside_zero_one_is_rejected()
        {
            Assert.Throws<SettingsValidationException>(() => new LocomotionTuning { Friction = 0f }.Validate());
            Assert.Throws<SettingsValidationException>(() => new LocomotionTuning { Friction = 1f }.Validate());
        }
    }

    public class CameraTuningTests
    {
        [Test]
        public void Default_values_validate()
        {
            Assert.DoesNotThrow(() => new CameraTuning().Validate());
        }

        [Test]
        public void Zero_or_negative_view_width_is_rejected()
        {
            Assert.Throws<SettingsValidationException>(() => new CameraTuning { ViewWidth = 0f }.Validate());
            Assert.Throws<SettingsValidationException>(() => new CameraTuning { ViewWidth = -10f }.Validate());
        }

        [Test]
        public void Negative_deadzone_is_rejected()
        {
            Assert.Throws<SettingsValidationException>(() => new CameraTuning { HorizontalDeadzone = -1f }.Validate());
        }

        [Test]
        public void Zero_or_negative_pan_speed_is_allowed_as_instant()
        {
            Assert.DoesNotThrow(() => new CameraTuning { HorizontalPanSpeed = 0f }.Validate());
            Assert.DoesNotThrow(() => new CameraTuning { HorizontalPanSpeed = -1f }.Validate());
        }
    }

    public class WeaponTuningTests
    {
        [Test]
        public void Default_values_validate()
        {
            Assert.DoesNotThrow(() => new WeaponTuning().Validate());
        }

        [Test]
        public void Zero_aim_divisor_is_rejected()
        {
            Assert.Throws<SettingsValidationException>(() => new WeaponTuning { AimDivisor = 0f }.Validate());
        }

        [Test]
        public void Non_positive_swing_range_is_rejected()
        {
            Assert.Throws<SettingsValidationException>(() => new WeaponTuning { WalkSwingRange = 0f }.Validate());
            Assert.Throws<SettingsValidationException>(() => new WeaponTuning { SprintSwingRange = -1f }.Validate());
        }
    }

    public class RainTuningTests
    {
        [Test]
        public void Default_values_validate()
        {
            Assert.DoesNotThrow(() => new RainTuning().Validate());
        }

        [Test]
        public void Drift_min_above_max_is_rejected()
        {
            Assert.Throws<SettingsValidationException>(() => new RainTuning { DriftMin = 0, DriftMax = -1 }.Validate());
        }

        [Test]
        public void Fall_min_above_max_is_rejected()
        {
            Assert.Throws<SettingsValidationException>(() => new RainTuning { FallMin = 60, FallMax = 40 }.Validate());
        }

        [Test]
        public void Alpha_outside_zero_one_is_rejected()
        {
            Assert.Throws<SettingsValidationException>(() => new RainTuning { Alpha = 1.5f }.Validate());
            Assert.Throws<SettingsValidationException>(() => new RainTuning { Alpha = -0.1f }.Validate());
        }
    }

    public class SuspicionTuningValidationTests
    {
        [Test]
        public void Default_values_validate()
        {
            Assert.DoesNotThrow(() => new SuspicionTuning().Validate());
        }

        [Test]
        public void Min_above_max_is_rejected()
        {
            var tuning = new SuspicionTuning { MinSuspicion = 100f, MaxSuspicion = 0f };
            Assert.Throws<SettingsValidationException>(() => tuning.Validate());
        }

        [Test]
        public void Mid_range_below_near_range_is_rejected()
        {
            var tuning = new SuspicionTuning { NearRangeViews = 1f, MidRangeViews = 0.5f };
            Assert.Throws<SettingsValidationException>(() => tuning.Validate());
        }
    }

    public class SettingsRegistryTests
    {
        [Test]
        public void Default_registry_validates()
        {
            Assert.DoesNotThrow(() => SettingsRegistry.Default.Validate());
        }

        [Test]
        public void A_bad_category_fails_the_whole_registry()
        {
            var registry = new SettingsRegistry { Rain = new RainTuning { Alpha = 5f } };
            Assert.Throws<SettingsValidationException>(() => registry.Validate());
        }

        [Test]
        public void A_missing_category_is_rejected()
        {
            var registry = new SettingsRegistry { Camera = null };
            Assert.Throws<SettingsValidationException>(() => registry.Validate());
        }
    }
}

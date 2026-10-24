// NUnit 4 moved the classic Assert; the tests were written against it (Unity Test Framework).
using Assert = NUnit.Framework.Legacy.ClassicAssert;
using CollectionAssert = NUnit.Framework.Legacy.CollectionAssert;
using StringAssert = NUnit.Framework.Legacy.StringAssert;
using Incognito.Npc;
using Incognito.Player;
using NUnit.Framework;

namespace Incognito.Tests
{
    public class SuspicionMeterTests
    {
        private const float Dt = 1f / 60f; // one original step
        private const float View = 1920f;  // view_wview
        private const PlayerLocomotionState.FacingDirection Left = PlayerLocomotionState.FacingDirection.Left;
        private const PlayerLocomotionState.FacingDirection Right = PlayerLocomotionState.FacingDirection.Right;

        // NPC at 0 facing right; player at playerX facing left = genuinely facing each other.
        private static void StepFacing(SuspicionMeter meter, float playerX, bool lookingDown = false, int steps = 1)
        {
            for (int i = 0; i < steps; i++)
                meter.Step(Dt, 0f, Right, playerX, Left, View, lookingDown);
        }

        [Test]
        public void Close_eye_contact_raises_suspicion_fastest()
        {
            var meter = new SuspicionMeter();
            StepFacing(meter, 500f, steps: 10);
            // How close (0.2) + facing, not looking down (+0.1) = 0.3/step.
            Assert.AreEqual(0.3f, meter.Rate, 0.0001f);
            Assert.AreEqual(3f, meter.Suspicion, 0.001f);
        }

        [Test]
        public void Mid_range_eye_contact_raises_suspicion_slower()
        {
            var meter = new SuspicionMeter();
            StepFacing(meter, 1500f, steps: 10);
            Assert.AreEqual(0.2f, meter.Rate, 0.0001f); // 0.1 + 0.1
            Assert.AreEqual(2f, meter.Suspicion, 0.001f);
        }

        [Test]
        public void Range_bands_are_half_view_and_full_view_inclusive()
        {
            var near = new SuspicionMeter();
            StepFacing(near, View / 2f);
            Assert.AreEqual(0.3f, near.Rate, 0.0001f);

            var mid = new SuspicionMeter();
            StepFacing(mid, View);
            Assert.AreEqual(0.2f, mid.Rate, 0.0001f);
        }

        [Test]
        public void Looking_down_while_facing_bleeds_suspicion_off()
        {
            var meter = new SuspicionMeter();
            StepFacing(meter, 500f, steps: 100);
            float before = meter.Suspicion;

            StepFacing(meter, 500f, lookingDown: true, steps: 10);
            Assert.AreEqual(0.2f - 0.2f, meter.Rate, 0.0001f); // close but looking down: net 0
            Assert.AreEqual(before, meter.Suspicion, 0.001f);

            StepFacing(meter, 1500f, lookingDown: true, steps: 10);
            Assert.AreEqual(0.1f - 0.2f, meter.Rate, 0.0001f);
            Assert.Less(meter.Suspicion, before);
        }

        [Test]
        public void Not_facing_each_other_reduces_the_rate()
        {
            var meter = new SuspicionMeter();
            // Both facing right: the NPC is looking at the player's back.
            meter.Step(Dt, 0f, Right, 500f, Right, View, false);
            Assert.AreEqual(0.2f - 0.1f, meter.Rate, 0.0001f);
        }

        [Test]
        public void Out_of_range_the_rate_keeps_falling_every_step()
        {
            var meter = new SuspicionMeter();
            StepFacing(meter, 500f, steps: 200); // 60%
            Assert.AreEqual(60f, meter.Suspicion, 0.01f);

            // Far away and back to back: rate -= 0.1 (far) and -= 0.1 (not facing) per step.
            for (int i = 0; i < 5; i++) meter.Step(Dt, 0f, Left, 5000f, Right, View, false);
            Assert.AreEqual(0.3f - 5 * 0.2f, meter.Rate, 0.0001f);
            // Accelerating drain: 0.1 + -0.1 + -0.3 + -0.5 + -0.7 = -1.5.
            Assert.AreEqual(58.5f, meter.Suspicion, 0.01f);
        }

        [Test]
        public void Suspicion_is_clamped_to_zero_and_one_hundred()
        {
            var meter = new SuspicionMeter();
            StepFacing(meter, 500f, steps: 1000);
            Assert.AreEqual(100f, meter.Suspicion);
            Assert.AreEqual(100, meter.DisplayPercent);

            for (int i = 0; i < 1000; i++) meter.Step(Dt, 0f, Left, 5000f, Right, View, false);
            Assert.AreEqual(0f, meter.Suspicion);
            Assert.AreEqual(0, meter.DisplayPercent);
        }

        [Test]
        public void Coming_back_into_range_resets_the_far_decay()
        {
            var meter = new SuspicionMeter();
            for (int i = 0; i < 100; i++) meter.Step(Dt, 0f, Left, 5000f, Right, View, false);
            Assert.Less(meter.Rate, -10f);

            StepFacing(meter, 500f);
            Assert.AreEqual(0.3f, meter.Rate, 0.0001f);
        }

        [Test]
        public void Facing_each_other_requires_each_to_face_the_other()
        {
            // Player right of NPC.
            Assert.IsTrue(SuspicionMeter.FacingEachOther(0f, Right, 100f, Left));
            Assert.IsFalse(SuspicionMeter.FacingEachOther(0f, Left, 100f, Right)); // back to back
            Assert.IsFalse(SuspicionMeter.FacingEachOther(0f, Right, 100f, Right));
            // Player left of NPC.
            Assert.IsTrue(SuspicionMeter.FacingEachOther(100f, Left, 0f, Right));
            Assert.IsFalse(SuspicionMeter.FacingEachOther(100f, Right, 0f, Left));
            // Same spot: opposite facings count.
            Assert.IsTrue(SuspicionMeter.FacingEachOther(0f, Left, 0f, Right));
        }

        [Test]
        public void Step_is_frame_rate_independent_in_range()
        {
            var fine = new SuspicionMeter();
            var coarse = new SuspicionMeter();
            for (int i = 0; i < 60; i++) fine.Step(Dt, 0f, Right, 500f, Left, View, false);
            for (int i = 0; i < 30; i++) coarse.Step(Dt * 2f, 0f, Right, 500f, Left, View, false);
            Assert.AreEqual(fine.Suspicion, coarse.Suspicion, 0.01f);
        }

        [Test]
        public void Tuning_is_data()
        {
            var meter = new SuspicionMeter(new SuspicionTuning { NearRate = 1f, EyeContactRateBonus = 0f });
            StepFacing(meter, 500f, steps: 10);
            Assert.AreEqual(10f, meter.Suspicion, 0.001f);
        }
    }
}

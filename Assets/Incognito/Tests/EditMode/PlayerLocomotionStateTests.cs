using NUnit.Framework;
using Incognito.Player;

namespace Incognito.Tests
{
    public class PlayerLocomotionStateTests
    {
        private const float Dt = 1f / 60f; // one original-style step

        private static PlayerLocomotionState NewState() => new PlayerLocomotionState
        {
            BobAmplitude = 5f,
            BobRampSpeed = 0.3f,
            WalkSpeed = 4f,
            SprintMultiplier = 2f,
        };

        [Test]
        public void Idle_by_default()
        {
            var state = NewState();
            Assert.AreEqual(PlayerLocomotionState.WalkMode.Idle, state.CurrentMode);
            Assert.AreEqual(PlayerLocomotionState.FacingDirection.Left, state.Facing);
            Assert.AreEqual(0f, state.BobOffset);
        }

        [Test]
        public void Walking_right_snaps_speed_and_sets_facing()
        {
            var state = NewState();
            state.Step(Dt, moveLeft: false, moveRight: true, sprint: false);

            Assert.AreEqual(PlayerLocomotionState.WalkMode.Walking, state.CurrentMode);
            Assert.AreEqual(PlayerLocomotionState.FacingDirection.Right, state.Facing);
            Assert.AreEqual(state.WalkSpeed, state.HorizontalSpeed, 0.0001f);
        }

        [Test]
        public void Walking_left_snaps_negative_speed_and_sets_facing()
        {
            var state = NewState();
            state.Step(Dt, moveLeft: true, moveRight: false, sprint: false);

            Assert.AreEqual(PlayerLocomotionState.FacingDirection.Left, state.Facing);
            Assert.AreEqual(-state.WalkSpeed, state.HorizontalSpeed, 0.0001f);
        }

        [Test]
        public void Sprint_ramps_up_to_double_speed_instead_of_snapping()
        {
            var state = NewState();

            // First step un-sprinted so HorizontalSpeed starts at WalkSpeed, matching the
            // original (sprint ramps from whatever dx already is, it doesn't reset it).
            state.Step(Dt, moveLeft: false, moveRight: true, sprint: false);
            float afterSnap = state.HorizontalSpeed;
            Assert.AreEqual(state.WalkSpeed, afterSnap, 0.0001f);

            state.Step(Dt, moveLeft: false, moveRight: true, sprint: true);
            Assert.Greater(state.HorizontalSpeed, afterSnap, "sprint should ramp above the walk snap speed");
            Assert.Less(state.HorizontalSpeed, state.WalkSpeed * state.SprintMultiplier);

            // Many more steps should approach but never exceed the sprint cap.
            for (int i = 0; i < 1000; i++)
            {
                state.Step(Dt, moveLeft: false, moveRight: true, sprint: true);
            }
            Assert.AreEqual(state.WalkSpeed * state.SprintMultiplier, state.HorizontalSpeed, 0.0001f);
        }

        [Test]
        public void Releasing_input_decays_to_idle_via_friction()
        {
            var state = NewState();
            state.Step(Dt, moveLeft: false, moveRight: true, sprint: false);
            Assert.AreEqual(PlayerLocomotionState.WalkMode.Walking, state.CurrentMode);

            PlayerLocomotionState.WalkMode modeAfterRelease = PlayerLocomotionState.WalkMode.Walking;
            for (int i = 0; i < 500 && modeAfterRelease != PlayerLocomotionState.WalkMode.Idle; i++)
            {
                state.Step(Dt, moveLeft: false, moveRight: false, sprint: false);
                modeAfterRelease = state.CurrentMode;
            }

            Assert.AreEqual(PlayerLocomotionState.WalkMode.Idle, state.CurrentMode);
            Assert.AreEqual(0f, state.HorizontalSpeed);
        }

        [Test]
        public void Bob_oscillates_between_amplitude_bounds_while_walking()
        {
            var state = NewState();
            float min = 0f, max = 0f;

            for (int i = 0; i < 200; i++)
            {
                state.Step(Dt, moveLeft: false, moveRight: true, sprint: false);
                min = UnityEngine.Mathf.Min(min, state.BobOffset);
                max = UnityEngine.Mathf.Max(max, state.BobOffset);
            }

            Assert.AreEqual(state.BobAmplitude, max, 0.0001f);
            Assert.AreEqual(-state.BobAmplitude, min, 0.0001f);
        }

        [Test]
        public void Bob_peak_event_fires_when_bob_reaches_the_top()
        {
            var state = NewState();
            int peaks = 0;
            state.OnBobPeak += () => peaks++;

            for (int i = 0; i < 200; i++)
            {
                state.Step(Dt, moveLeft: false, moveRight: true, sprint: false);
            }

            Assert.Greater(peaks, 0, "expected at least one bob peak (footstep hook) over 200 steps");
        }

        [Test]
        public void Bob_resets_when_not_walking()
        {
            var state = NewState();
            state.Step(Dt, moveLeft: false, moveRight: true, sprint: false);
            state.Step(Dt, moveLeft: false, moveRight: true, sprint: false);
            Assert.Greater(state.BobOffset, 0f);

            state.Step(Dt, moveLeft: false, moveRight: false, sprint: false);
            Assert.AreEqual(0f, state.BobOffset);
        }

        [Test]
        public void Position_advances_in_the_facing_direction()
        {
            var state = NewState();
            state.SetInitialPositionX(0f);
            for (int i = 0; i < 10; i++)
            {
                state.Step(Dt, moveLeft: false, moveRight: true, sprint: false);
            }
            Assert.Greater(state.PositionX, 0f);
        }
    }
}

// NUnit 4 moved the classic Assert; the tests were written against it (Unity Test Framework).
using Assert = NUnit.Framework.Legacy.ClassicAssert;
using CollectionAssert = NUnit.Framework.Legacy.CollectionAssert;
using StringAssert = NUnit.Framework.Legacy.StringAssert;
using Incognito.Npc;
using Incognito.Player;
using NUnit.Framework;

namespace Incognito.Tests
{
    public class NpcPatrolStateTests
    {
        private const float Dt = 1f / 60f; // one original step

        private static NpcPatrolState NewState(float x, params float[] route)
        {
            var state = new NpcPatrolState();
            state.SetInitialPositionX(x);
            state.SetRoute(route);
            return state;
        }

        [Test]
        public void Starts_idle_and_walks_after_the_two_step_start_alarm()
        {
            var state = NewState(2400f, 12000f);
            Assert.AreEqual(NpcPatrolState.PatrolMode.Idle, state.CurrentMode);

            state.Step(Dt);
            Assert.AreEqual(NpcPatrolState.PatrolMode.Idle, state.CurrentMode);
            Assert.AreEqual(2400f, state.PositionX, 0.0001f);

            state.Step(Dt);
            Assert.AreEqual(NpcPatrolState.PatrolMode.Walking, state.CurrentMode);
            Assert.AreEqual(2405f, state.PositionX, 0.0001f); // walk_speed = 5 per step
        }

        [Test]
        public void Walks_toward_the_waypoint_and_faces_it()
        {
            var right = NewState(1000f, 3000f);
            var left = NewState(1000f, 0f);
            for (int i = 0; i < 10; i++) { right.Step(Dt); left.Step(Dt); }

            Assert.AreEqual(PlayerLocomotionState.FacingDirection.Right, right.Facing);
            Assert.Greater(right.PositionX, 1000f);
            Assert.AreEqual(PlayerLocomotionState.FacingDirection.Left, left.Facing);
            Assert.Less(left.PositionX, 1000f);
        }

        [Test]
        public void Near_matches_a_near_b_inclusive_range()
        {
            Assert.IsTrue(NpcPatrolState.Near(120f, 100f, 20f));
            Assert.IsTrue(NpcPatrolState.Near(80f, 100f, 20f));
            Assert.IsFalse(NpcPatrolState.Near(120.5f, 100f, 20f));
            Assert.IsFalse(NpcPatrolState.Near(79.5f, 100f, 20f));
        }

        [Test]
        public void Loops_the_route_turning_at_each_waypoint()
        {
            var state = NewState(1000f, 1500f, 1000f);
            bool turnedLeft = false;
            float maxX = 0f;
            for (int i = 0; i < 400; i++)
            {
                state.Step(Dt);
                maxX = CoreMath.Max(maxX, state.PositionX);
                if (state.Facing == PlayerLocomotionState.FacingDirection.Left) turnedLeft = true;
            }

            Assert.IsTrue(turnedLeft);
            Assert.AreEqual(NpcPatrolState.PatrolMode.Walking, state.CurrentMode);
            // Turns once within 20px of 1500 and never overshoots past the arrive window.
            Assert.GreaterOrEqual(maxX, 1480f);
            Assert.LessOrEqual(maxX, 1525f);
        }

        [Test]
        public void Single_waypoint_route_coasts_to_a_stop_with_friction()
        {
            var state = NewState(1000f, 1100f);
            bool sawStopping = false;
            for (int i = 0; i < 200; i++)
            {
                state.Step(Dt);
                if (state.CurrentMode == NpcPatrolState.PatrolMode.Stopping) sawStopping = true;
            }

            Assert.IsTrue(sawStopping);
            Assert.AreEqual(NpcPatrolState.PatrolMode.Idle, state.CurrentMode);
            Assert.AreEqual(0f, state.HorizontalSpeed);
            // hspeed *= 0.9 from 5 until < 1 adds ~ 5*(0.9 + ... ) < 45px past the arrive point.
            Assert.Less(state.PositionX, 1100f + 20f + 50f);
        }

        [Test]
        public void Bobs_like_the_player_while_walking()
        {
            var state = NewState(0f, 100000f);
            float min = 0f, max = 0f;
            for (int i = 0; i < 200; i++)
            {
                state.Step(Dt);
                min = CoreMath.Min(min, state.BobOffset);
                max = CoreMath.Max(max, state.BobOffset);
            }

            Assert.AreEqual(5f, max, 0.0001f);
            Assert.AreEqual(-5f, min, 0.0001f);
        }

        [Test]
        public void No_route_never_moves()
        {
            var state = NewState(500f);
            for (int i = 0; i < 60; i++) state.Step(Dt);
            Assert.AreEqual(500f, state.PositionX);
            Assert.AreEqual(NpcPatrolState.PatrolMode.Idle, state.CurrentMode);
        }
    }
}

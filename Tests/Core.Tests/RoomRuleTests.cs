// NUnit 4 moved the classic Assert; the tests were written against it (Unity Test Framework).
using Assert = NUnit.Framework.Legacy.ClassicAssert;
using CollectionAssert = NUnit.Framework.Legacy.CollectionAssert;
using StringAssert = NUnit.Framework.Legacy.StringAssert;
using Incognito.Player;
using Incognito.World;
using NUnit.Framework;

namespace Incognito.Tests
{
    public class RoomFadeStateTests
    {
        [Test]
        public void A_room_starts_black_and_fades_out_at_0_03_per_step_then_turns_off()
        {
            var fade = new RoomFadeState();
            Assert.AreEqual(1f, fade.Alpha);
            Assert.AreEqual(RoomFadeState.FadeMode.FadeOut, fade.Mode);

            fade.Step(1);
            Assert.AreEqual(0.97f, fade.Alpha, 1e-5f);

            fade.Step(34); // ceil(1 / 0.03) = 34 steps to clear
            Assert.AreEqual(0f, fade.Alpha, 1e-5f);
            fade.Step(1);
            Assert.AreEqual(RoomFadeState.FadeMode.Off, fade.Mode);
            Assert.IsFalse(fade.ReadyToLoad);
        }

        [Test]
        public void Fading_in_loads_the_next_room_only_once_opaque()
        {
            var fade = new RoomFadeState();
            fade.Step(40);
            fade.FadeIn();

            fade.Step(33);
            Assert.Less(fade.Alpha, 1f);
            Assert.IsFalse(fade.ReadyToLoad);

            fade.Step(1);
            Assert.AreEqual(1f, fade.Alpha, 1e-5f);
            fade.Step(1); // else room_goto(next_room)
            Assert.IsTrue(fade.ReadyToLoad);
        }
    }

    public class RoomExitRulesTests
    {
        [Test]
        public void Side_exits_trigger_when_the_player_box_touches_the_strip()
        {
            // street00's right exit at 11552; the player enters from the right at 11008.
            Assert.IsFalse(RoomExitRules.Overlaps(11008f, 512f, 11552f));
            Assert.IsFalse(RoomExitRules.Overlaps(11296f, 512f, 11552f)); // right edge exactly on the strip
            Assert.IsTrue(RoomExitRules.Overlaps(11297f, 512f, 11552f));

            // street00's left exit at -32 (spanning -32..-22): entering from the left at 512 is clear.
            Assert.IsFalse(RoomExitRules.Overlaps(512f, 512f, -32f));
            Assert.IsTrue(RoomExitRules.Overlaps(233f, 512f, -32f));
        }

        [Test]
        public void The_shop_barrier_is_a_wall_from_either_side()
        {
            Assert.AreEqual(2000f, RoomExitRules.Block(2000f, 512f, 800f)); // clear of it
            Assert.AreEqual(810f + 256f, RoomExitRules.Block(1000f, 512f, 800f)); // pushed back right
            Assert.AreEqual(800f - 256f, RoomExitRules.Block(700f, 512f, 800f));  // pushed back left
        }

        [Test]
        public void The_door_answers_W_only_within_300px()
        {
            Assert.IsTrue(RoomExitRules.CanUseDoor(9500f, 9504f));
            Assert.IsTrue(RoomExitRules.CanUseDoor(9804f, 9504f));
            Assert.IsFalse(RoomExitRules.CanUseDoor(9805f, 9504f));
        }

        [Test]
        public void The_door_icon_fades_by_distance()
        {
            // Close: fades up 0.01 per step to opaque.
            float a = 0f;
            for (int i = 0; i < 150; i++) a = RoomExitRules.DoorAlphaStep(a, 9504f, 9504f);
            Assert.AreEqual(1f, a, 1e-4f);

            // Glimpsed (300–600px): settles around a quarter.
            for (int i = 0; i < 150; i++) a = RoomExitRules.DoorAlphaStep(a, 9504f + 450f, 9504f);
            Assert.AreEqual(0.25f, a, 0.011f);

            // Far: fades away.
            for (int i = 0; i < 50; i++) a = RoomExitRules.DoorAlphaStep(a, 0f, 9504f);
            Assert.AreEqual(0f, a, 1e-4f);
        }

        [Test]
        public void The_door_icon_drifts_by_its_accumulated_hover()
        {
            var hover = new DoorHoverState();
            hover.Step(2);
            Assert.AreEqual(0f, hover.OffsetY); // alarm every 3 steps
            hover.Step(1);
            Assert.AreEqual(1f, hover.OffsetY); // hoverY 1
            hover.Step(3 * 4);
            Assert.AreEqual(1f + 2f + 3f + 4f + 5f, hover.OffsetY); // hoverY ramps to 5, added each time

            float min = float.MaxValue, max = float.MinValue;
            for (int i = 0; i < 3 * 200; i++)
            {
                hover.Step(1);
                min = System.Math.Min(min, hover.OffsetY);
                max = System.Math.Max(max, hover.OffsetY);
            }
            Assert.LessOrEqual(max - min, 40f); // a bounded drift, not a runaway
        }
    }

    public class RainStateTests
    {
        [Test]
        public void Six_drops_spawn_per_step_across_the_view_above_the_room()
        {
            var rolls = new System.Collections.Generic.List<(int, int)>();
            var rain = new RainState((min, max) => { rolls.Add((min, max)); return min; });
            rain.Step(1, 8000f, 1920f);

            Assert.AreEqual(rain.DropsPerStep, rain.Drops.Count);
            CollectionAssert.Contains(rolls, (8000, 9920));  // x across the view
            CollectionAssert.Contains(rolls, (-1080, -4));   // y up to a room above
            CollectionAssert.Contains(rolls, (-6, -3));      // drift left
            CollectionAssert.Contains(rolls, (40, 50));      // fall speed

            var d = rain.Drops[0];
            Assert.AreEqual(8000f - 6f, d.X); // spawned then moved once
            Assert.AreEqual(-1080f + 40f, d.Y);
        }

        [Test]
        public void Drops_are_removed_once_below_the_room()
        {
            var rain = new RainState((min, max) => max); // spawn at y = -4, fall 50 per step
            rain.Step(1, 0f, 1920f);
            Assert.AreEqual(6, rain.Drops.Count);
            rain.Step(21, 0f, 1920f); // first batch: -4 + 50*22 = 1096 > 1080
            Assert.AreEqual(6 * 21, rain.Drops.Count);
        }
    }

    public class StepClockTests
    {
        [Test]
        public void Step_clock_counts_whole_60hz_steps_and_carries_the_rest()
        {
            var clock = new StepClock();
            Assert.AreEqual(0, clock.Advance(1f / 120f));
            Assert.AreEqual(1, clock.Advance(1f / 120f + 1e-5f));
            Assert.AreEqual(2, clock.Advance(2f / 60f));
        }
    }
}

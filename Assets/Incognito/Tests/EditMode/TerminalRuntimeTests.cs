using System.Linq;
using Incognito.Terminal;
using NUnit.Framework;
using UnityEngine;

namespace Incognito.Tests
{
    public class VicinityTests
    {
        [Test]
        public void Vicinity_keeps_only_objects_within_radius()
        {
            var a = new ConsoleObject(ConsoleObjectType.Sheep, "near");
            var b = new ConsoleObject(ConsoleObjectType.Sheep, "edge");
            var c = new ConsoleObject(ConsoleObjectType.Sheep, "far");
            var candidates = new[]
            {
                (new Vector2(1f, 0f), a),
                (new Vector2(10f, 0f), b),
                (new Vector2(10.5f, 0f), c),
            };

            var within = Vicinity.Within(Vector2.zero, 10f, candidates).ToList();
            CollectionAssert.AreEqual(new[] { a, b }, within);
        }
    }

    public class CommandPromptSlideTests
    {
        [Test]
        public void Starts_hidden_off_the_left_edge()
        {
            var slide = new CommandPromptSlide(580f);
            Assert.AreEqual(CommandPromptSlide.Mode.Off, slide.Current);
            Assert.AreEqual(-580f, slide.X);
            Assert.IsFalse(slide.IsOpen);
        }

        [Test]
        public void Opening_eases_in_by_one_plus_a_tenth_of_the_remaining_distance()
        {
            var slide = new CommandPromptSlide(580f);
            slide.Open();
            slide.StepOnce();
            Assert.AreEqual(-580f + 1f + 58f, slide.X); // 1 + ceil(580 * 0.1)
        }

        [Test]
        public void Opening_settles_at_zero_then_idles()
        {
            var slide = new CommandPromptSlide(580f);
            slide.Open();
            for (int i = 0; i < 200 && slide.Current != CommandPromptSlide.Mode.Idle; i++) slide.StepOnce();

            Assert.AreEqual(CommandPromptSlide.Mode.Idle, slide.Current);
            Assert.AreEqual(0f, slide.X);
            Assert.IsTrue(slide.IsOpen);
        }

        [Test]
        public void Closing_slides_out_at_twenty_pixels_per_step_then_turns_off()
        {
            var slide = new CommandPromptSlide(580f);
            slide.Open();
            for (int i = 0; i < 200 && slide.Current != CommandPromptSlide.Mode.Idle; i++) slide.StepOnce();

            slide.Close();
            slide.StepOnce();
            Assert.AreEqual(-20f, slide.X);

            for (int i = 0; i < 100 && slide.Current != CommandPromptSlide.Mode.Off; i++) slide.StepOnce();
            Assert.AreEqual(CommandPromptSlide.Mode.Off, slide.Current);
            Assert.AreEqual(-580f, slide.X);
        }

        [Test]
        public void Step_runs_whole_sixty_fps_steps_and_carries_the_remainder()
        {
            var a = new CommandPromptSlide(580f);
            var b = new CommandPromptSlide(580f);
            a.Open();
            b.Open();

            a.Step(1.5f / 60f); // one whole step, half carried
            a.Step(0.75f / 60f); // completes the second (with a quarter step left over)
            b.StepOnce();
            b.StepOnce();

            Assert.AreEqual(b.X, a.X, 0.0001f);
        }
    }
}

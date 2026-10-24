// NUnit 4 moved the classic Assert; the tests were written against it (Unity Test Framework).
using Assert = NUnit.Framework.Legacy.ClassicAssert;
using Incognito.Typing;
using NUnit.Framework;

namespace Incognito.Tests
{
    public class TypewriterStateTests
    {
        private const float TwoSteps = 2f / 60f;

        [Test]
        public void One_character_is_revealed_every_two_steps()
        {
            var t = new TypewriterState("abc");
            t.Step(TwoSteps / 2f);
            Assert.AreEqual("", t.Shown);
            t.Step(TwoSteps / 2f);
            Assert.AreEqual("a", t.Shown);
            t.Step(TwoSteps);
            Assert.AreEqual("ab", t.Shown);
            Assert.IsFalse(t.FadingOut);
        }

        [Test]
        public void The_tick_that_reveals_the_last_character_already_fades_one_notch()
        {
            var t = new TypewriterState("ab");
            t.Step(TwoSteps * 2f);
            Assert.AreEqual("ab", t.Shown);
            Assert.IsTrue(t.FadingOut);
            Assert.AreEqual(0.99f, t.Alpha, 1e-5f);
        }

        [Test]
        public void It_fades_to_nothing_then_is_done_and_stops_changing()
        {
            var t = new TypewriterState("a");
            for (int i = 0; i < 200 && !t.Done; i++) t.Step(TwoSteps);
            Assert.IsTrue(t.Done);
            Assert.AreEqual(0f, t.Alpha);

            t.Step(1f);
            Assert.AreEqual(0f, t.Alpha);
            Assert.AreEqual("a", t.Shown);
        }

        [Test]
        public void Empty_or_null_text_just_fades_out()
        {
            var t = new TypewriterState(null);
            Assert.AreEqual("", t.Text);
            t.Step(TwoSteps);
            Assert.IsTrue(t.FadingOut);
            Assert.AreEqual(0.99f, t.Alpha, 1e-5f);
        }
    }
}

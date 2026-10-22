using System.Collections.Generic;
using Incognito.Eye;
using Incognito.Player;
using NUnit.Framework;
using UnityEngine;

namespace Incognito.Tests
{
    public class EyeballStateTests
    {
        private const float Dt = 1f / 60f; // one original step
        private const PlayerLocomotionState.FacingDirection Left = PlayerLocomotionState.FacingDirection.Left;
        private const PlayerLocomotionState.FacingDirection Right = PlayerLocomotionState.FacingDirection.Right;

        private static EyeballState NeverBlinks() => new EyeballState((min, max) => max);
        private static EyeballState AlwaysBlinks() => new EyeballState((min, max) => min);

        [Test]
        public void Socket_flips_with_facing_using_the_original_offsets()
        {
            var state = NeverBlinks();
            state.Step(Dt, Left, 0f, 0f, false);
            Assert.AreEqual(-108f, state.SocketOffsetX, 0.001f); // -(512/2) + 148
            state.Step(Dt, Right, 0f, 0f, false);
            Assert.AreEqual(93f, state.SocketOffsetX, 0.001f);
        }

        [Test]
        public void Pupil_is_damped_parallax_not_one_to_one()
        {
            var state = NeverBlinks();
            // pupiloffsetX = 7 + (mouse_x - player.x)/160; pupilY = -3 + mouse_y/160.
            state.Step(Dt, Left, 0f, 0f, false);
            Assert.AreEqual(new Vector2Int(7, -3), state.PupilOffset);

            state.Step(Dt, Left, 960f, 1080f, false); // half a view right, bottom of screen
            Assert.AreEqual(new Vector2Int(13, 4), state.PupilOffset); // 7+6, round(-3+6.75)

            state.Step(Dt, Left, -960f, 540f, false);
            Assert.AreEqual(new Vector2Int(1, 0), state.PupilOffset); // 7-6, round(-3+3.375)
        }

        [Test]
        public void Pupil_tracking_does_not_depend_on_facing()
        {
            var state = NeverBlinks();
            state.Step(Dt, Left, 320f, 160f, false);
            var leftPupil = state.PupilOffset;
            state.Step(Dt, Right, 320f, 160f, false);
            Assert.AreEqual(leftPupil, state.PupilOffset);
        }

        [Test]
        public void Aiming_snaps_the_pupil_x_by_facing_but_keeps_vertical_tracking()
        {
            var state = NeverBlinks();
            state.Step(Dt, Left, 960f, 1080f, true);
            Assert.AreEqual(new Vector2Int(-1, 4), state.PupilOffset);
            state.Step(Dt, Right, -960f, 1080f, true);
            Assert.AreEqual(new Vector2Int(15, 4), state.PupilOffset);
        }

        [Test]
        public void Blink_rolls_irandom_range_0_to_160_each_idle_step()
        {
            var rolls = new List<(int, int)>();
            var state = new EyeballState((min, max) => { rolls.Add((min, max)); return max; });
            for (int i = 0; i < 5; i++) state.Step(Dt, Left, 0f, 0f, false);

            Assert.AreEqual(5, rolls.Count);
            Assert.That(rolls, Has.All.EqualTo((0, 160)));
            Assert.IsFalse(state.IsBlinking);
            Assert.AreEqual(0f, state.EyelidDrop);
        }

        [Test]
        public void A_blink_drops_the_lid_2px_per_step_then_lifts_it_back()
        {
            var state = AlwaysBlinks();
            state.Step(Dt, Left, 0f, 0f, false); // the roll hits: blinking starts, lid not yet moved
            Assert.IsTrue(state.IsBlinking);
            Assert.AreEqual(0f, state.EyelidDrop);

            for (int i = 1; i <= 10; i++)
            {
                state.Step(Dt, Left, 0f, 0f, false);
                Assert.AreEqual(2f * i, state.EyelidDrop, 0.001f);
            }

            state.Step(Dt, Left, 0f, 0f, false); // fully shut: turns around without moving
            Assert.AreEqual(20f, state.EyelidDrop, 0.001f);

            for (int i = 9; i >= 0; i--)
            {
                state.Step(Dt, Left, 0f, 0f, false);
                Assert.AreEqual(2f * i, state.EyelidDrop, 0.001f);
            }
            Assert.IsTrue(state.IsBlinking);

            state.Step(Dt, Left, 0f, 0f, false); // back open: blink ends
            Assert.IsFalse(state.IsBlinking);
            Assert.AreEqual(0f, state.EyelidDrop);
        }

        [Test]
        public void No_dice_are_rolled_while_a_blink_is_in_progress()
        {
            int rolls = 0;
            var state = new EyeballState((min, max) => { rolls++; return min; });
            for (int i = 0; i < 22; i++) state.Step(Dt, Left, 0f, 0f, false);
            Assert.AreEqual(1, rolls); // one roll started it; the 21 blink steps roll nothing
        }

        [Test]
        public void Blink_runs_in_whole_original_steps_at_any_frame_rate()
        {
            var state = AlwaysBlinks();
            state.Step(Dt * 0.5f, Left, 0f, 0f, false); // half a step: nothing happens yet
            Assert.IsFalse(state.IsBlinking);
            state.Step(Dt * 0.6f, Left, 0f, 0f, false); // completes step 1: blink starts
            Assert.IsTrue(state.IsBlinking);
            state.Step(Dt * 3f, Left, 0f, 0f, false); // a slow frame catches up 3 steps
            Assert.AreEqual(6f, state.EyelidDrop, 0.001f);
        }

        [Test]
        public void Default_random_blinks_about_once_per_161_steps()
        {
            var rng = new System.Random(1234);
            var state = new EyeballState((min, max) => rng.Next(min, max + 1));
            int blinks = 0;
            bool wasBlinking = false;
            const int steps = 60 * 60 * 10; // ten minutes of play
            for (int i = 0; i < steps; i++)
            {
                state.Step(Dt, Left, 0f, 0f, false);
                if (state.IsBlinking && !wasBlinking) blinks++;
                wasBlinking = state.IsBlinking;
            }
            // Each blink costs 1 roll + 21 lid steps; expected interval ~ 161 + 21 steps.
            float expected = steps / (161f + 21f);
            Assert.That(blinks, Is.InRange(expected * 0.8f, expected * 1.2f));
        }
    }

    public class EyeCursorSpritesTests
    {
        [Test]
        public void Each_mode_shows_its_own_sprite()
        {
            Assert.AreEqual(EyeCursorSprite.Eye, EyeCursorSprites.For(EyeCursorMode.Look, false));
            Assert.AreEqual(EyeCursorSprite.Talk, EyeCursorSprites.For(EyeCursorMode.Talk, false));
            Assert.AreEqual(EyeCursorSprite.Pointer, EyeCursorSprites.For(EyeCursorMode.Pointer, false));
        }

        [Test]
        public void Aiming_overrides_every_mode_with_the_reticle()
        {
            Assert.AreEqual(EyeCursorSprite.Reticle, EyeCursorSprites.For(EyeCursorMode.Look, true));
            Assert.AreEqual(EyeCursorSprite.Reticle, EyeCursorSprites.For(EyeCursorMode.Talk, true));
            Assert.AreEqual(EyeCursorSprite.Reticle, EyeCursorSprites.For(EyeCursorMode.Pointer, true));
        }
    }
}

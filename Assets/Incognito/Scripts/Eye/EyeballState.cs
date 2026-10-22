using System;
using Incognito.Player;
using UnityEngine;

namespace Incognito.Eye
{
    /// <summary>
    /// Pure, engine-loop-free simulation of the original in-world eyeball, obj_eye
    /// (RECONSTRUCTION.md §3): a socket that flips with facing, a pupil that tracks the mouse with a
    /// damped /160 parallax (snapped while aiming), and a randomized blink that drops the eyelid
    /// 2px per step over the eye and back. All distances are original GameMaker pixels with y
    /// pointing DOWN, exactly as the .gml wrote them; the MonoBehaviour converts to world units.
    /// </summary>
    public sealed class EyeballState
    {
        // Original GM8 constants were tuned per 60fps step.
        public const float ReferenceFps = 60f;

        // obj_eye Step: facing left -> offsetX = -(player.sprite_width/2) + 148 with the 512px
        // spr_player; facing right -> offsetX = 93. offsetY = 0 either way.
        public float SocketOffsetLeft = -(512f / 2f) + 148f;
        public float SocketOffsetRight = 93f;

        // pupiloffsetX = 7 + ((mouse_x - player.x) / 160); pupilY = y - 3 + (mouse_y / 160).
        public float PupilBaseOffsetX = 7f;
        public float PupilBaseOffsetY = -3f;
        public float ParallaxDivisor = 160f;

        // While aiming, pupiloffsetX snaps to -1 (left) / 15 (right): staring down the sights.
        public float AimPupilOffsetLeft = -1f;
        public float AimPupilOffsetRight = 15f;

        // eyelid_height = sprite_get_height(spr_eyelid_left) = 20; eyelidDY moves 2 per step;
        // a blink starts when irandom_range(0, eyelid_delay) == 0 with eyelid_delay = 160.
        public float EyelidHeight = 20f;
        public float EyelidSpeed = 2f;
        public int BlinkDelay = 160;

        /// <summary>Socket x relative to the player's sprite origin (original px).</summary>
        public float SocketOffsetX { get; private set; }

        /// <summary>Pupil centre relative to the socket (original px, y down), pixel-snapped.</summary>
        public Vector2Int PupilOffset { get; private set; }

        /// <summary>How far the eyelid has dropped over the eye: 0 = open, EyelidHeight = shut.</summary>
        public float EyelidDrop { get; private set; }

        public bool IsBlinking { get; private set; }

        private bool eyelidGoingDown = true;
        private float stepAccumulator;
        private readonly Func<int, int, int> randomRangeInclusive;

        /// <param name="randomRangeInclusive">GM's irandom_range(min, max), both ends inclusive.
        /// Defaults to System.Random; tests inject a stub to make the blink deterministic.</param>
        public EyeballState(Func<int, int, int> randomRangeInclusive = null)
        {
            if (randomRangeInclusive == null)
            {
                var rng = new System.Random();
                randomRangeInclusive = (min, max) => rng.Next(min, max + 1);
            }
            this.randomRangeInclusive = randomRangeInclusive;
            SocketOffsetX = SocketOffsetLeft;
        }

        /// <param name="mouseOffsetX">mouse_x - player.x, in original px.</param>
        /// <param name="mouseY">mouse_y: the cursor's height from the top of the (view-tall)
        /// room, in original px.</param>
        public void Step(float deltaTime, PlayerLocomotionState.FacingDirection facing,
                         float mouseOffsetX, float mouseY, bool aiming)
        {
            bool left = facing == PlayerLocomotionState.FacingDirection.Left;
            SocketOffsetX = left ? SocketOffsetLeft : SocketOffsetRight;

            // The pupil is a pure function of this frame's inputs, so it tracks every frame.
            float pupilX = aiming
                ? (left ? AimPupilOffsetLeft : AimPupilOffsetRight)
                : PupilBaseOffsetX + mouseOffsetX / ParallaxDivisor;
            float pupilY = PupilBaseOffsetY + mouseY / ParallaxDivisor;
            PupilOffset = new Vector2Int(Mathf.RoundToInt(pupilX), Mathf.RoundToInt(pupilY));

            // The blink is a discrete per-step state machine with a per-step dice roll, so it
            // runs in whole original steps to keep both the 1-in-161 odds and the 2px lid speed
            // identical at any frame rate.
            stepAccumulator += Mathf.Max(deltaTime, 0f) * ReferenceFps;
            // (A hair of tolerance so 60 x (1/60f) float round-off still counts as a step.)
            while (stepAccumulator >= 1f - 1e-4f)
            {
                stepAccumulator -= 1f;
                BlinkStep();
            }
        }

        private void BlinkStep()
        {
            if (!IsBlinking)
            {
                if (randomRangeInclusive(0, BlinkDelay) == 0) IsBlinking = true;
                return;
            }

            if (eyelidGoingDown)
            {
                if (EyelidDrop < EyelidHeight) EyelidDrop += EyelidSpeed;
                else eyelidGoingDown = false;
            }
            else
            {
                if (EyelidDrop > 0f)
                {
                    EyelidDrop -= EyelidSpeed;
                }
                else
                {
                    EyelidDrop = 0f;
                    eyelidGoingDown = true;
                    IsBlinking = false;
                }
            }
        }
    }
}

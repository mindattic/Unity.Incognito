using System;
using Incognito.Motion;
using Incognito.Settings;

namespace Incognito.Player
{
    /// <summary>
    /// Pure, engine-loop-free simulation of the original obj_walker locomotion
    /// (RECONSTRUCTION.md §1): a triangle-wave walk bob, snap-to-speed walking with a
    /// ramped sprint, and friction-based stopping. Kept separate from MonoBehaviour so
    /// it can be driven directly by EditMode tests without a scene or Play mode.
    /// </summary>
    public sealed class PlayerLocomotionState
    {
        public enum WalkMode { Idle, Walking, Stopping }
        public enum FacingDirection { Left, Right }

        // Original GM8 constants were tuned per 60fps step; ReferenceFps lets Step()
        // accept a real Time.deltaTime and still match that per-step feel.
        public const float ReferenceFps = 60f;

        public float BobAmplitude = 5f;
        public float BobRampSpeed = 0.3f;
        public float WalkSpeed = 4f;
        public float SprintMultiplier = 2f;
        public float SprintAccelStep = 0.1f;
        public float Friction = 0.9f;
        public float StopThreshold = 1f;

        public WalkMode CurrentMode { get; private set; } = WalkMode.Idle;
        public FacingDirection Facing { get; private set; } = FacingDirection.Left;
        public float BobOffset => bob.Offset;
        public float HorizontalSpeed { get; private set; }
        public float PositionX { get; private set; }

        /// <summary>Sets the starting X without going through Step's velocity integration.</summary>
        public void SetInitialPositionX(float x) => PositionX = x;

        /// <summary>Fires when the bob reaches its top and starts descending — the original's
        /// play_footstep() call site. No subscriber is wired up by this ticket (T3); the audio
        /// pipeline (T11+) hooks in here.</summary>
        public event Action OnBobPeak
        {
            add => bob.OnPeak += value;
            remove => bob.OnPeak -= value;
        }

        private readonly TriangleWaveBob bob = new TriangleWaveBob();

        /// <summary>
        /// Builds from a validated <see cref="LocomotionTuning"/> (U.INC-T22); null takes the
        /// original's own values, same as every field's own default above (so the parameterless
        /// object-initializer form tests use still works unchanged).
        /// </summary>
        public PlayerLocomotionState(LocomotionTuning tuning = null)
        {
            LocomotionTuning t = tuning ?? new LocomotionTuning();
            BobAmplitude = t.BobAmplitude;
            BobRampSpeed = t.BobRampSpeed;
            WalkSpeed = t.WalkSpeed;
            SprintMultiplier = t.SprintMultiplier;
            SprintAccelStep = t.SprintAccelStep;
            Friction = t.Friction;
            StopThreshold = t.StopThreshold;
        }

        /// <summary>Original aim entry (obj_walker right-pressed): mode = "idle", dx = 0, and the
        /// idle branch zeroes walkY. Used while aiming locks movement (U.INC-T9).</summary>
        public void ForceIdle()
        {
            CurrentMode = WalkMode.Idle;
            HorizontalSpeed = 0f;
            bob.Reset();
        }

        /// <summary>
        /// Turns the player in place, as a talking NPC that arrives does (obj_npc's stop-and-face
        /// block, U.INC-T6). Only while idle, so it never fights the walk keys. True if applied.
        /// </summary>
        public bool TurnToFace(FacingDirection facing)
        {
            if (CurrentMode != WalkMode.Idle) return false;
            Facing = facing;
            return true;
        }

        public void Step(float deltaTime, bool moveLeft, bool moveRight, bool sprint)
        {
            float steps = CoreMath.Max(deltaTime, 0f) * ReferenceFps;
            bool pressingRight = moveRight && !moveLeft;
            bool pressingLeft = moveLeft && !moveRight;

            if (pressingRight)
            {
                CurrentMode = WalkMode.Walking;
                Facing = FacingDirection.Right;
                Accelerate(WalkSpeed, sprint, steps);
            }
            else if (pressingLeft)
            {
                CurrentMode = WalkMode.Walking;
                Facing = FacingDirection.Left;
                Accelerate(-WalkSpeed, sprint, steps);
            }
            else if (CurrentMode == WalkMode.Walking || CurrentMode == WalkMode.Stopping)
            {
                CurrentMode = WalkMode.Stopping;
                HorizontalSpeed *= CoreMath.Pow(Friction, steps);
                if (CoreMath.Abs(HorizontalSpeed) < StopThreshold)
                {
                    HorizontalSpeed = 0f;
                    CurrentMode = WalkMode.Idle;
                }
            }

            PositionX += HorizontalSpeed * steps;

            if (CurrentMode == WalkMode.Walking)
            {
                Bob(steps);
            }
            else
            {
                bob.Reset();
            }
        }

        private void Accelerate(float target, bool sprint, float steps)
        {
            if (!sprint)
            {
                // Original: dx = walk_speed (an instant snap, not a ramp) while holding a
                // direction key without Shift.
                HorizontalSpeed = target;
                return;
            }

            // Original: holding Shift ramps dx by +/-0.1 per step toward walk_speed*2,
            // continuing from whatever dx already was (so it can start mid-ramp).
            float cap = target >= 0 ? WalkSpeed * SprintMultiplier : -WalkSpeed * SprintMultiplier;
            float step = SprintAccelStep * steps * CoreMath.Sign(target);
            HorizontalSpeed += step;
            HorizontalSpeed = target >= 0
                ? CoreMath.Min(HorizontalSpeed, cap)
                : CoreMath.Max(HorizontalSpeed, cap);
        }

        private void Bob(float steps)
        {
            bob.Amplitude = BobAmplitude;
            bob.RampSpeed = BobRampSpeed;
            bob.Advance(steps);
        }
    }
}

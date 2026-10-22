using System;
using Incognito.Player;
using UnityEngine;

namespace Incognito.Npc
{
    /// <summary>
    /// obj_npc's suspicion tuning (RECONSTRUCTION.md §4), kept as serialized data rather than
    /// code (AGENTS.md law 2). Rates are percent per original 60fps step; ranges are fractions of
    /// the view width (the original compared against view_wview/2 and view_wview).
    /// </summary>
    [Serializable]
    public sealed class SuspicionTuning
    {
        [Tooltip("How Close? Rate while within NearRangeViews of the view width.")]
        public float NearRate = 0.2f;
        [Tooltip("How Close? Rate while within MidRangeViews of the view width.")]
        public float MidRate = 0.1f;
        [Tooltip("How Close? Beyond mid range the rate is not reset, it decays by this per step.")]
        public float FarRateDecay = 0.1f;

        [Tooltip("Facing each other and the player is not looking down.")]
        public float EyeContactRateBonus = 0.1f;
        [Tooltip("Facing each other but the player is looking down.")]
        public float LookingDownRatePenalty = 0.2f;
        [Tooltip("Not facing each other.")]
        public float NotFacingRatePenalty = 0.1f;

        public float NearRangeViews = 0.5f;
        public float MidRangeViews = 1f;

        [Tooltip("Rebuild decision: the original never clamped suspicion (it went negative).")]
        public float MinSuspicion = 0f;
        public float MaxSuspicion = 100f;
    }

    /// <summary>
    /// Pure port of obj_npc's per-step suspicion accumulator: <c>suspicion += suspicion_rate</c>,
    /// with the rate rebuilt every step from three cues — how close (horizontal distance only),
    /// whether NPC and player face each other, and whether the player is "looking down" (cursor
    /// in the lower half of the screen). See RECONSTRUCTION.md §4 for the verbatim original.
    /// </summary>
    public sealed class SuspicionMeter
    {
        public const float ReferenceFps = 60f;

        public SuspicionTuning Tuning { get; }

        /// <summary>Running suspicion, in percent.</summary>
        public float Suspicion { get; private set; }

        /// <summary>The original's suspicion_rate (percent per step).</summary>
        public float Rate { get; private set; }

        /// <summary>The percentage the original drew over the NPC's head: round(suspicion).</summary>
        public int DisplayPercent => Mathf.RoundToInt(Suspicion);

        public SuspicionMeter(SuspicionTuning tuning = null)
        {
            Tuning = tuning ?? new SuspicionTuning();
        }

        public void Step(
            float deltaTime,
            float npcX, PlayerLocomotionState.FacingDirection npcFacing,
            float playerX, PlayerLocomotionState.FacingDirection playerFacing,
            float viewWidth, bool playerLookingDown)
        {
            float steps = Mathf.Max(deltaTime, 0f) * ReferenceFps;
            var t = Tuning;

            // Are they facing each other? / is Player looking down?
            float facingAdjust;
            if (FacingEachOther(npcX, npcFacing, playerX, playerFacing))
            {
                facingAdjust = playerLookingDown ? -t.LookingDownRatePenalty : t.EyeContactRateBonus;
            }
            else
            {
                facingAdjust = -t.NotFacingRatePenalty;
            }

            // How Close? (point_distance(player.x, 0, x, 0): horizontal only)
            float distance = Mathf.Abs(playerX - npcX);
            if (distance <= viewWidth * t.NearRangeViews)
            {
                Rate = t.NearRate + facingAdjust;
            }
            else if (distance <= viewWidth * t.MidRangeViews)
            {
                Rate = t.MidRate + facingAdjust;
            }
            else
            {
                // Out of range the original never resets the rate: it keeps subtracting each
                // step, so suspicion drains ever faster the longer you stay away.
                Rate += (-t.FarRateDecay + facingAdjust) * steps;
            }

            Suspicion = Mathf.Clamp(Suspicion + Rate * steps, t.MinSuspicion, t.MaxSuspicion);
        }

        /// <summary>
        /// True when each one faces toward the other. The original only checked that the two
        /// facings were opposite, which also counted standing back to back; the rebuild requires
        /// real mutual facing (BIBLE.md, Decisions made). At the same X, opposite facings count.
        /// </summary>
        public static bool FacingEachOther(
            float npcX, PlayerLocomotionState.FacingDirection npcFacing,
            float playerX, PlayerLocomotionState.FacingDirection playerFacing)
        {
            if (npcFacing == playerFacing) return false;
            if (playerX > npcX) return npcFacing == PlayerLocomotionState.FacingDirection.Right;
            if (playerX < npcX) return npcFacing == PlayerLocomotionState.FacingDirection.Left;
            return true;
        }
    }
}

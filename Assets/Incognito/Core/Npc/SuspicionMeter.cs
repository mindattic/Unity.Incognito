using System;
using Incognito.Player;
using Incognito.Settings;

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
        /// <summary>How Close? Rate while within NearRangeViews of the view width.</summary>
        public float NearRate = 0.2f;
        /// <summary>How Close? Rate while within MidRangeViews of the view width.</summary>
        public float MidRate = 0.1f;
        /// <summary>How Close? Beyond mid range the rate is not reset, it decays by this per step.</summary>
        public float FarRateDecay = 0.1f;

        /// <summary>Facing each other and the player is not looking down.</summary>
        public float EyeContactRateBonus = 0.1f;
        /// <summary>Facing each other but the player is looking down.</summary>
        public float LookingDownRatePenalty = 0.2f;
        /// <summary>Not facing each other.</summary>
        public float NotFacingRatePenalty = 0.1f;

        public float NearRangeViews = 0.5f;
        public float MidRangeViews = 1f;

        /// <summary>Rebuild decision: the original never clamped suspicion (it went negative).</summary>
        public float MinSuspicion = 0f;
        public float MaxSuspicion = 100f;

        /// <summary>Validated on load (U.INC-T22, AGENTS.md law 4): throws on an unusable value.</summary>
        public void Validate()
        {
            RequireNumber(nameof(NearRate), NearRate);
            RequireNumber(nameof(MidRate), MidRate);
            RequireNumber(nameof(FarRateDecay), FarRateDecay);
            RequireNumber(nameof(EyeContactRateBonus), EyeContactRateBonus);
            RequireNumber(nameof(LookingDownRatePenalty), LookingDownRatePenalty);
            RequireNumber(nameof(NotFacingRatePenalty), NotFacingRatePenalty);
            if (float.IsNaN(NearRangeViews) || NearRangeViews <= 0f)
                throw new SettingsValidationException("SuspicionTuning.NearRangeViews must be positive: " + NearRangeViews);
            if (float.IsNaN(MidRangeViews) || MidRangeViews < NearRangeViews)
                throw new SettingsValidationException("SuspicionTuning.MidRangeViews must be at least NearRangeViews: " + MidRangeViews);
            if (float.IsNaN(MinSuspicion) || float.IsNaN(MaxSuspicion) || MinSuspicion > MaxSuspicion)
                throw new SettingsValidationException("SuspicionTuning.MinSuspicion must not exceed MaxSuspicion: " + MinSuspicion + " > " + MaxSuspicion);
        }

        private static void RequireNumber(string field, float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                throw new SettingsValidationException("SuspicionTuning." + field + " must be a finite number: " + value);
        }
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
        public int DisplayPercent => CoreMath.RoundToInt(Suspicion);

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
            float steps = CoreMath.Max(deltaTime, 0f) * ReferenceFps;
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
            float distance = CoreMath.Abs(playerX - npcX);
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

            Suspicion = CoreMath.Clamp(Suspicion + Rate * steps, t.MinSuspicion, t.MaxSuspicion);
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

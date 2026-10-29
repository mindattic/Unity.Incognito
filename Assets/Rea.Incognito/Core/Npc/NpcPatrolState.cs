using System.Collections.Generic;
using Incognito.Motion;
using Incognito.Player;

namespace Incognito.Npc
{
    /// <summary>
    /// Pure simulation of obj_npc's patrol (RECONSTRUCTION.md §4): after a short start alarm the
    /// NPC walks at a fixed speed toward its waypoint, facing it, with the same triangle-wave bob
    /// as the player; "arrived" is the original's a_near_b(x, waypoint_x, 20). Units are original
    /// pixels and 60fps steps, like <see cref="PlayerLocomotionState"/>.
    /// The original only ever had one waypoint (x = 12000, past the end of street00) and never
    /// stopped on arrival; the rebuild cycles a route of waypoints and, with a single waypoint,
    /// enters the original's (otherwise unreached) "stopping" friction mode there — see BIBLE.md.
    /// Talk mode (U.INC-T6, obj_npc's "talking"): a talk click sends the NPC to stand one body
    /// width beside the player, where it stops, turns to face them and waits; once the player
    /// walks, the NPC resumes its route a set delay after the player's last walking step (the
    /// original's alarm[0] re-armed by obj_walker's held walk keys).
    /// </summary>
    public sealed class NpcPatrolState
    {
        public enum PatrolMode { Idle, Walking, Stopping, Talking }

        public const float ReferenceFps = 60f;

        public float WalkSpeed = 5f;          // globals.gml: walk_speed = 5
        public float ArriveRange = 20f;       // a_near_b(x, waypoint_x, 20)
        public float StartDelaySteps = 2f;    // alarm[0] = rate (2)
        public float Friction = 0.9f;         // hspeed *= 0.9
        public float StopThreshold = 1f;      // abs(hspeed) < 1
        public float TalkResumeDelaySteps = 200f; // obj_walker: with(obj_npc) if talking alarm[0] = 200

        public PatrolMode CurrentMode { get; private set; } = PatrolMode.Idle;
        // Create event: facing = right, image_xscale = -1.
        public PlayerLocomotionState.FacingDirection Facing { get; private set; } = PlayerLocomotionState.FacingDirection.Right;
        public float PositionX { get; private set; }
        public float HorizontalSpeed { get; private set; }
        public float BobOffset => bob.Offset;
        public int WaypointIndex { get; private set; }

        public float? CurrentWaypoint => route.Count > 0 ? route[WaypointIndex] : (float?)null;

        /// <summary>Where a talk click sent the NPC (original pixels), while talking.</summary>
        public float? TalkTarget => CurrentMode == PatrolMode.Talking ? talkTarget : (float?)null;

        /// <summary>True once a talking NPC has arrived, stopped and turned to the player.</summary>
        public bool InConversation { get; private set; }

        /// <summary>Raised once when a talking NPC arrives and faces the player, with the side
        /// the player should turn to so the two face each other.</summary>
        public event System.Action<PlayerLocomotionState.FacingDirection> ConversationStarted;

        private float talkTarget;
        private float partnerX;
        private bool talkArrived;
        private float resumeStepsLeft;

        private readonly TriangleWaveBob bob = new TriangleWaveBob();
        private readonly List<float> route = new List<float>();
        private float startDelayElapsed;
        private bool started;

        public void SetInitialPositionX(float x) => PositionX = x;

        /// <summary>obj_npc's obj_bullet collision (U.INC-T9): x -= / += 100, nothing else — the
        /// patrol carries on from the new spot.</summary>
        public void Knockback(float dx) => PositionX += dx;

        /// <summary>Replaces the patrol route (original-pixel X positions, walked in order and
        /// looped) and heads for its first waypoint.</summary>
        public void SetRoute(IEnumerable<float> waypoints)
        {
            route.Clear();
            if (waypoints != null) route.AddRange(waypoints);
            WaypointIndex = 0;
        }

        /// <summary>
        /// obj_npc left-click in talk mode: mode = "talking" and walk to stand
        /// <paramref name="standOff"/> (the NPC's sprite width) from the player, on the side the
        /// NPC is coming from. The original set waypoint_x on the far side (x &lt; player.x =&gt;
        /// player.x + sprite_width), walking the NPC through the player; see BIBLE.md.
        /// </summary>
        public void StartTalking(float playerX, float standOff)
        {
            started = true; // a talk click also pre-empts the start alarm
            partnerX = playerX;
            talkTarget = PositionX <= playerX ? playerX - standOff : playerX + standOff;
            talkArrived = false;
            InConversation = false;
            resumeStepsLeft = 0f;
            CurrentMode = PatrolMode.Talking;
        }

        /// <summary>
        /// obj_walker's held walk keys: every step the player walks, each talking NPC's alarm[0]
        /// is re-armed, so it resumes its route <see cref="TalkResumeDelaySteps"/> after the
        /// player's last walking step.
        /// </summary>
        public void PlayerWalked()
        {
            if (CurrentMode == PatrolMode.Talking) resumeStepsLeft = TalkResumeDelaySteps;
        }

        /// <summary>The original's a_near_b(a, b, range): inclusive on both ends.</summary>
        public static bool Near(float a, float b, float range) => a <= b + range && a >= b - range;

        public void Step(float deltaTime)
        {
            float steps = CoreMath.Max(deltaTime, 0f) * ReferenceFps;

            if (!started && route.Count > 0)
            {
                // Alarm 0: mode = "walking"; waypoint_x = previous_waypoint_x.
                startDelayElapsed += steps;
                if (startDelayElapsed >= StartDelaySteps)
                {
                    started = true;
                    CurrentMode = PatrolMode.Walking;
                }
            }

            switch (CurrentMode)
            {
                case PatrolMode.Walking:
                    bob.Advance(steps);
                    StepWalking();
                    break;

                case PatrolMode.Stopping:
                    if (CoastToStop(steps)) CurrentMode = PatrolMode.Idle;
                    break;

                case PatrolMode.Talking:
                    StepTalking(steps);
                    break;

                default:
                    HorizontalSpeed = 0f;
                    break;
            }

            // GM applies hspeed after the Step event.
            PositionX += HorizontalSpeed * steps;
        }

        /// <summary>hspeed *= 0.9 until abs(hspeed) &lt; 1; true once stopped.</summary>
        private bool CoastToStop(float steps)
        {
            HorizontalSpeed *= CoreMath.Pow(Friction, steps);
            if (CoreMath.Abs(HorizontalSpeed) >= StopThreshold) return false;
            HorizontalSpeed = 0f;
            return true;
        }

        private void StepTalking(float steps)
        {
            if (resumeStepsLeft > 0f)
            {
                resumeStepsLeft -= steps;
                if (resumeStepsLeft <= 0f)
                {
                    // Alarm 0: mode = "walking"; waypoint_x = previous_waypoint_x.
                    resumeStepsLeft = 0f;
                    InConversation = false;
                    CurrentMode = PatrolMode.Walking;
                    StepWalking();
                    return;
                }
            }

            if (!talkArrived)
            {
                if (!Near(PositionX, talkTarget, ArriveRange))
                {
                    // The walking branch also ran for "talking": bob and head for waypoint_x.
                    bob.Advance(steps);
                    HeadToward(talkTarget);
                    return;
                }

                talkArrived = true;
            }

            if (InConversation || !CoastToStop(steps)) return;

            // The original's (unreached) stop-and-face block: the NPC faces the player and the
            // player is turned to face it.
            InConversation = true;
            Facing = PositionX < partnerX
                ? PlayerLocomotionState.FacingDirection.Right
                : PlayerLocomotionState.FacingDirection.Left;
            ConversationStarted?.Invoke(Facing == PlayerLocomotionState.FacingDirection.Right
                ? PlayerLocomotionState.FacingDirection.Left
                : PlayerLocomotionState.FacingDirection.Right);
        }

        private void StepWalking()
        {
            if (route.Count == 0)
            {
                CurrentMode = PatrolMode.Stopping;
                return;
            }

            if (Near(PositionX, route[WaypointIndex], ArriveRange))
            {
                if (route.Count == 1)
                {
                    CurrentMode = PatrolMode.Stopping;
                    return;
                }

                WaypointIndex = (WaypointIndex + 1) % route.Count;
            }

            HeadToward(route[WaypointIndex]);
        }

        private void HeadToward(float target)
        {
            if (PositionX < target)
            {
                Facing = PlayerLocomotionState.FacingDirection.Right;
                HorizontalSpeed = WalkSpeed;
            }
            else if (PositionX > target)
            {
                Facing = PlayerLocomotionState.FacingDirection.Left;
                HorizontalSpeed = -WalkSpeed;
            }
        }
    }
}

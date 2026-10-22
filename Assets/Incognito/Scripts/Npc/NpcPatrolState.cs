using System.Collections.Generic;
using Incognito.Motion;
using Incognito.Player;
using UnityEngine;

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
    /// The talk-mode walk-to-player (mode "talking") is U.INC-T6 and is not handled here.
    /// </summary>
    public sealed class NpcPatrolState
    {
        public enum PatrolMode { Idle, Walking, Stopping }

        public const float ReferenceFps = 60f;

        public float WalkSpeed = 5f;          // globals.gml: walk_speed = 5
        public float ArriveRange = 20f;       // a_near_b(x, waypoint_x, 20)
        public float StartDelaySteps = 2f;    // alarm[0] = rate (2)
        public float Friction = 0.9f;         // hspeed *= 0.9
        public float StopThreshold = 1f;      // abs(hspeed) < 1

        public PatrolMode CurrentMode { get; private set; } = PatrolMode.Idle;
        // Create event: facing = right, image_xscale = -1.
        public PlayerLocomotionState.FacingDirection Facing { get; private set; } = PlayerLocomotionState.FacingDirection.Right;
        public float PositionX { get; private set; }
        public float HorizontalSpeed { get; private set; }
        public float BobOffset => bob.Offset;
        public int WaypointIndex { get; private set; }

        public float? CurrentWaypoint => route.Count > 0 ? route[WaypointIndex] : (float?)null;


        private readonly TriangleWaveBob bob = new TriangleWaveBob();
        private readonly List<float> route = new List<float>();
        private float startDelayElapsed;
        private bool started;

        public void SetInitialPositionX(float x) => PositionX = x;

        /// <summary>Replaces the patrol route (original-pixel X positions, walked in order and
        /// looped) and heads for its first waypoint.</summary>
        public void SetRoute(IEnumerable<float> waypoints)
        {
            route.Clear();
            if (waypoints != null) route.AddRange(waypoints);
            WaypointIndex = 0;
        }

        /// <summary>The original's a_near_b(a, b, range): inclusive on both ends.</summary>
        public static bool Near(float a, float b, float range) => a <= b + range && a >= b - range;

        public void Step(float deltaTime)
        {
            float steps = Mathf.Max(deltaTime, 0f) * ReferenceFps;

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
                    HorizontalSpeed *= Mathf.Pow(Friction, steps);
                    if (Mathf.Abs(HorizontalSpeed) < StopThreshold)
                    {
                        HorizontalSpeed = 0f;
                        CurrentMode = PatrolMode.Idle;
                    }
                    break;

                default:
                    HorizontalSpeed = 0f;
                    break;
            }

            // GM applies hspeed after the Step event.
            PositionX += HorizontalSpeed * steps;
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

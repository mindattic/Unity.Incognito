
namespace Incognito.Weapon
{
    /// <summary>
    /// One arm's rotation, reproducing the original obj_arm step event (RECONSTRUCTION.md §1):
    /// a hand-rolled linear ping-pong while walking (not trig), a relax back toward 0 when idle,
    /// and a direct set while aiming. <see cref="Angle"/> is GameMaker's image_angle in degrees,
    /// counter-clockwise as seen on screen — the same sense as a Unity Z rotation.
    /// </summary>
    public sealed class ArmSwingState
    {
        /// <summary>Current image_angle, degrees, counter-clockwise.</summary>
        public float Angle { get; private set; }

        /// <summary>True while the arm is rotating clockwise (GM: swing_forward, image_angle -= rot_speed).</summary>
        public bool SwingingForward { get; private set; }

        // GM: the "angle" counter — degrees travelled in the current half-swing.
        private float travelled;

        /// <param name="startForward">GM: arm1 starts swing_forward = true, arm2 is created with
        /// swing_forward = false (start_game.gml), so the two arms swing in opposition.</param>
        public ArmSwingState(bool startForward)
        {
            SwingingForward = startForward;
        }

        /// <summary>One walking step: move <paramref name="rotSpeed"/> per step and flip direction
        /// once more than <paramref name="range"/> degrees have been travelled.</summary>
        public void Swing(float steps, float range, float rotSpeed)
        {
            float delta = rotSpeed * steps;
            Angle += SwingingForward ? -delta : delta;
            travelled += delta;
            if (travelled > range)
            {
                travelled = 0f;
                SwingingForward = !SwingingForward;
            }
        }

        /// <summary>Idle: ease back toward 0 at <paramref name="rotSpeed"/> per step. The original
        /// stepped by a fixed amount and could jitter around 0 forever; this lands on 0.</summary>
        public void Relax(float steps, float rotSpeed)
        {
            Angle = CoreMath.MoveTowards(Angle, 0f, rotSpeed * steps);
        }

        /// <summary>Aiming: the angle is driven directly (mouse), no oscillation.</summary>
        public void Set(float angle)
        {
            Angle = angle;
        }
    }
}

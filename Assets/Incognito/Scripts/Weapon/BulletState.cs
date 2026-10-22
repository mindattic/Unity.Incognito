using UnityEngine;

namespace Incognito.Weapon
{
    /// <summary>
    /// Pure motion of obj_bullet (RECONSTRUCTION.md §1): a straight line at a fixed speed per
    /// step (shoot_bullet.gml: speed = 60), destroyed once it leaves the room. Units are whatever
    /// the caller uses consistently for position, bounds and speed.
    /// </summary>
    public sealed class BulletState
    {
        public const float ReferenceFps = PlayerWeaponState.ReferenceFps;

        public Vector2 Position { get; private set; }

        /// <summary>Displacement per original 60fps step.</summary>
        public Vector2 VelocityPerStep { get; }

        public BulletState(Vector2 position, float directionDegrees, float speedPerStep)
        {
            Position = position;
            float rad = directionDegrees * Mathf.Deg2Rad;
            VelocityPerStep = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * speedPerStep;
        }

        /// <summary>Advances one frame and returns where the bullet was before it moved, so the
        /// caller can sweep the segment for hits (a 60px/step bullet would skip thin targets).</summary>
        public Vector2 Step(float deltaTime)
        {
            Vector2 previous = Position;
            Position += VelocityPerStep * (Mathf.Max(deltaTime, 0f) * ReferenceFps);
            return previous;
        }

        /// <summary>obj_bullet step: destroyed when x/y leave the room.</summary>
        public bool IsOutside(Rect room) =>
            Position.x < room.xMin || Position.x > room.xMax || Position.y < room.yMin || Position.y > room.yMax;

        /// <summary>obj_npc collision with obj_bullet: shoved <paramref name="distance"/> away
        /// from the bullet's side (bullet to the right -> move left, otherwise right).</summary>
        public static float KnockbackX(float bulletX, float targetX, float distance) =>
            bulletX > targetX ? -distance : distance;
    }
}


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

        public Vec2 Position { get; private set; }

        /// <summary>Displacement per original 60fps step.</summary>
        public Vec2 VelocityPerStep { get; }

        public BulletState(Vec2 position, float directionDegrees, float speedPerStep)
        {
            Position = position;
            float rad = directionDegrees * CoreMath.Deg2Rad;
            VelocityPerStep = new Vec2(CoreMath.Cos(rad), CoreMath.Sin(rad)) * speedPerStep;
        }

        /// <summary>Advances one frame and returns where the bullet was before it moved, so the
        /// caller can sweep the segment for hits (a 60px/step bullet would skip thin targets).</summary>
        public Vec2 Step(float deltaTime)
        {
            Vec2 previous = Position;
            Position += VelocityPerStep * (CoreMath.Max(deltaTime, 0f) * ReferenceFps);
            return previous;
        }

        /// <summary>obj_bullet step: destroyed when x/y leave the room.</summary>
        public bool IsOutside(Rect2 room) =>
            Position.x < room.xMin || Position.x > room.xMax || Position.y < room.yMin || Position.y > room.yMax;

        /// <summary>obj_npc collision with obj_bullet: shoved <paramref name="distance"/> away
        /// from the bullet's side (bullet to the right -> move left, otherwise right).</summary>
        public static float KnockbackX(float bulletX, float targetX, float distance) =>
            bulletX > targetX ? -distance : distance;
    }
}

using Incognito.World;
using UnityEngine;

namespace Incognito.Weapon
{
    /// <summary>
    /// A fired bullet: moves by <see cref="BulletState"/>, sweeps each frame's path for a
    /// <see cref="BulletTarget"/>, and removes itself on a hit or once it leaves the room.
    /// Spawned at runtime by <see cref="PlayerWeapon"/>; positions here are world units.
    /// </summary>
    public sealed class Bullet : MonoBehaviour
    {
        public BulletState State { get; private set; }
        public bool IsSpent { get; private set; }

        /// <summary>The player who fired it; a hit is credited to them.</summary>
        public PlayerId Owner { get; private set; }

        private Rect2 room;

        public void Launch(Vector2 worldPosition, float directionDegrees, float worldSpeedPerStep, Rect worldRoom,
            PlayerId owner = default)
        {
            Owner = owner;
            State = new BulletState(new Vec2(worldPosition.x, worldPosition.y), directionDegrees, worldSpeedPerStep);
            room = new Rect2(worldRoom.x, worldRoom.y, worldRoom.width, worldRoom.height);
            transform.SetPositionAndRotation(
                new Vector3(worldPosition.x, worldPosition.y, transform.position.z),
                Quaternion.Euler(0f, 0f, directionDegrees));
        }

        private void Update() => Advance(Time.deltaTime);

        /// <summary>One frame of travel. Public so EditMode tests can drive it without Play mode.</summary>
        public void Advance(float deltaTime)
        {
            if (IsSpent || State == null) return;

            Vec2 start = State.Step(deltaTime);
            Vec2 end = State.Position;
            var from = new Vector2(start.x, start.y);
            var to = new Vector2(end.x, end.y);

            foreach (var hit in Physics2D.LinecastAll(from, to))
            {
                var target = hit.collider.GetComponentInParent<BulletTarget>();
                if (target == null) continue;
                target.TakeHit(hit.point.x, Owner);
                Spend();
                return;
            }

            transform.position = new Vector3(to.x, to.y, transform.position.z);
            if (State.IsOutside(room)) Spend();
        }

        private void Spend()
        {
            IsSpent = true;
            if (Application.isPlaying) Destroy(gameObject);
            else DestroyImmediate(gameObject);
        }
    }
}

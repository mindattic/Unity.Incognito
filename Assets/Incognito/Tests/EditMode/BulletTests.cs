using Incognito.Player;
using Incognito.Weapon;
using NUnit.Framework;
using UnityEngine;

namespace Incognito.Tests
{
    public class BulletTests
    {
        private const float Dt = 1f / 60f;

        [Test]
        public void Bullet_hits_a_target_in_its_path_shoves_it_and_is_spent()
        {
            var targetGo = new GameObject("Target", typeof(BoxCollider2D));
            var bulletGo = new GameObject("Bullet");
            try
            {
                targetGo.transform.position = new Vector3(-10f, 0f, 0f);
                targetGo.GetComponent<BoxCollider2D>().size = new Vector2(0.2f, 4f);
                var target = targetGo.AddComponent<BulletTarget>();
                float shove = 0f;
                target.Hit += dx => shove = dx;
                Physics2D.SyncTransforms();

                // Fired leftward from the right of the target, fast enough to jump past a thin
                // collider in one frame — the swept linecast must still catch it.
                var bullet = bulletGo.AddComponent<Bullet>();
                bullet.Launch(Vector2.zero, 180f, 60f * PlayerLocomotion.WorldUnitsPerOriginalPixel,
                    new Rect(-100f, -100f, 200f, 200f));

                for (int i = 0; i < 20 && !bullet.IsSpent; i++) bullet.Advance(Dt);

                Assert.AreEqual(-100f, shove, "came from the right, so shoved left by 100px");
                Assert.AreEqual(-10f - 100f * PlayerLocomotion.WorldUnitsPerOriginalPixel,
                    targetGo.transform.position.x, 1e-4f);
                Assert.IsTrue(bulletGo == null, "spent bullet is destroyed");
            }
            finally
            {
                Object.DestroyImmediate(targetGo);
                if (bulletGo != null) Object.DestroyImmediate(bulletGo);
            }
        }

        [Test]
        public void Bullet_leaving_the_room_is_spent()
        {
            var bulletGo = new GameObject("Bullet");
            try
            {
                var bullet = bulletGo.AddComponent<Bullet>();
                bullet.Launch(Vector2.zero, 0f, 1f, new Rect(-2.5f, -2.5f, 5f, 5f));
                bullet.Advance(Dt);
                Assert.IsFalse(bullet.IsSpent);
                for (int i = 0; i < 5; i++) bullet.Advance(Dt);
                Assert.IsTrue(bulletGo == null);
            }
            finally
            {
                if (bulletGo != null) Object.DestroyImmediate(bulletGo);
            }
        }
    }
}

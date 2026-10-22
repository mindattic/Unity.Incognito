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
        public void Travels_its_speed_per_step_along_its_direction()
        {
            var bullet = new BulletState(Vector2.zero, 180f, 60f);
            Vector2 before = bullet.Step(Dt);

            Assert.AreEqual(Vector2.zero, before);
            Assert.AreEqual(-60f, bullet.Position.x, 1e-3f);
            Assert.AreEqual(0f, bullet.Position.y, 1e-3f);
        }

        [Test]
        public void Is_outside_once_it_leaves_the_room()
        {
            var room = new Rect(0f, 0f, 1000f, 1080f);
            var bullet = new BulletState(new Vector2(100f, 500f), 180f, 60f);
            Assert.IsFalse(bullet.IsOutside(room));

            bullet.Step(Dt);
            Assert.IsFalse(bullet.IsOutside(room));
            bullet.Step(Dt);
            Assert.IsTrue(bullet.IsOutside(room), "x went below 0");
        }

        [Test]
        public void Knockback_pushes_away_from_the_bullet_side()
        {
            Assert.AreEqual(-100f, BulletState.KnockbackX(bulletX: 50f, targetX: 0f, distance: 100f));
            Assert.AreEqual(100f, BulletState.KnockbackX(bulletX: -50f, targetX: 0f, distance: 100f));
        }

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
        public void Npc_patrol_takes_the_knockback()
        {
            var patrol = new Incognito.Npc.NpcPatrolState();
            patrol.SetInitialPositionX(2400f);
            patrol.Knockback(BulletState.KnockbackX(bulletX: 2450f, targetX: 2400f, distance: 100f));
            Assert.AreEqual(2300f, patrol.PositionX, 1e-4f);
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

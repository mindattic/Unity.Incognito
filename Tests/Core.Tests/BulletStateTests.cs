// NUnit 4 moved the classic Assert; the tests were written against it (Unity Test Framework).
using Assert = NUnit.Framework.Legacy.ClassicAssert;
using CollectionAssert = NUnit.Framework.Legacy.CollectionAssert;
using StringAssert = NUnit.Framework.Legacy.StringAssert;
using Incognito.Player;
using Incognito.Weapon;
using NUnit.Framework;

namespace Incognito.Tests
{
    public class BulletStateTests
    {
        private const float Dt = 1f / 60f;

        [Test]
        public void Travels_its_speed_per_step_along_its_direction()
        {
            var bullet = new BulletState(Vec2.Zero, 180f, 60f);
            Vec2 before = bullet.Step(Dt);

            Assert.AreEqual(Vec2.Zero, before);
            Assert.AreEqual(-60f, bullet.Position.x, 1e-3f);
            Assert.AreEqual(0f, bullet.Position.y, 1e-3f);
        }

        [Test]
        public void Is_outside_once_it_leaves_the_room()
        {
            var room = new Rect2(0f, 0f, 1000f, 1080f);
            var bullet = new BulletState(new Vec2(100f, 500f), 180f, 60f);
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
        public void Npc_patrol_takes_the_knockback()
        {
            var patrol = new Incognito.Npc.NpcPatrolState();
            patrol.SetInitialPositionX(2400f);
            patrol.Knockback(BulletState.KnockbackX(bulletX: 2450f, targetX: 2400f, distance: 100f));
            Assert.AreEqual(2300f, patrol.PositionX, 1e-4f);
        }
    }
}

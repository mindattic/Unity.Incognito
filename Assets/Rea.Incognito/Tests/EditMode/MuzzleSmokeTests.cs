using Incognito.Weapon;
using NUnit.Framework;
using UnityEngine;

namespace Incognito.Tests
{
    public class MuzzleSmokeTests
    {
        [Test]
        public void Puff_spawns_a_particle_per_tuned_count_at_the_given_position()
        {
            var smoke = MuzzleSmoke.Puff(new Vector3(12f, 34f, 0f), "Default", -5);
            try
            {
                Assert.AreEqual(new Vector3(12f, 34f, 0f), smoke.gameObject.transform.position);
                Assert.AreEqual(Incognito.Settings.GameSettings.Tuning.Weapon.MuzzleSmokeParticles,
                    smoke.gameObject.transform.childCount, "one child SpriteRenderer per puff particle");

                var child = smoke.gameObject.transform.GetChild(0);
                var sr = child.GetComponent<SpriteRenderer>();
                Assert.IsNotNull(sr.sprite, "a generated puff sprite, since ef_smoke has no original asset");
                Assert.AreEqual("Default", sr.sortingLayerName);
                Assert.AreEqual(-5, sr.sortingOrder, "drawn one step behind whatever it's given (below the player)");
            }
            finally
            {
                if (smoke != null) Object.DestroyImmediate(smoke.gameObject);
            }
        }

        [Test]
        public void Fades_out_and_destroys_itself_once_every_puff_is_spent()
        {
            var smoke = MuzzleSmoke.Puff(Vector3.zero, "Default", 0);
            GameObject go = smoke.gameObject; // captured before destruction: a destroyed component
                                               // throws on member access, not just on ==/!= null.
            try
            {
                int lifeSteps = smoke.State.LifeSteps;
                Assert.IsFalse(smoke.State.IsDone);

                smoke.Advance(lifeSteps - 1);
                Assert.IsFalse(smoke.State.IsDone, "not yet at the end of its life");
                Assert.IsTrue(go != null);

                smoke.Advance(1);
                Assert.IsTrue(go == null, "destroys itself once every puff has faded");
            }
            finally
            {
                if (go != null) Object.DestroyImmediate(go);
            }
        }
    }
}

using System.Collections;
using System.Linq;
using Incognito.Player;
using Incognito.Weapon;
using Incognito.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Incognito.Tests
{
    /// <summary>
    /// U.INC-T41, played in a real room: firing actually puffs a <see cref="MuzzleSmoke"/> (the bug
    /// was that <see cref="PlayerWeapon"/>'s old <c>muzzleEffect</c> hook was never assigned by any
    /// builder, so nothing ever showed), at the same point the bullet spawns and drawn below every
    /// other actor sprite in its sorting layer, the way <c>effect_create_below</c> drew it in GM.
    /// </summary>
    public class MuzzleSmokePlayTests
    {
        [UnityTest]
        public IEnumerator Firing_spawns_one_puff_at_the_muzzle_below_every_other_actor_sprite()
        {
            SceneManager.LoadScene("Street00");
            yield return null;
            yield return null;

            var locomotion = Object.FindAnyObjectByType<PlayerLocomotion>();
            Assert.IsNotNull(locomotion, "street00 has the player");
            var weapon = Object.FindAnyObjectByType<PlayerWeapon>();
            Assert.IsNotNull(weapon);
            GameSession session = locomotion.Session;
            Assert.IsNotNull(session);

            Assert.AreEqual(0, Object.FindObjectsByType<MuzzleSmoke>(FindObjectsSortMode.None).Length,
                "no smoke before the gun is even drawn");

            session.Submit(new ToggleGun(locomotion.Id));
            session.Submit(new PressAim(locomotion.Id));
            Assert.IsTrue(weapon.State.IsAiming);

            session.Submit(new PressFire(locomotion.Id));

            var smokes = Object.FindObjectsByType<MuzzleSmoke>(FindObjectsSortMode.None);
            Assert.AreEqual(1, smokes.Length, "exactly one puff per shot");
            MuzzleSmoke smoke = smokes[0];

            // The bullet spawns at the exact same muzzle point (PlayerWeapon.Fire); cross-checking
            // against it proves the puff is at the muzzle without duplicating its aim-angle math.
            var bullets = Object.FindObjectsByType<Bullet>(FindObjectsSortMode.None);
            Assert.AreEqual(1, bullets.Length, "exactly one bullet per shot");
            Assert.AreEqual(bullets[0].transform.position, smoke.transform.position,
                "the puff spawns at the same muzzle point as the bullet");

            var puffRenderer = smoke.GetComponentInChildren<SpriteRenderer>();
            Assert.IsNotNull(puffRenderer, "the puff draws through generated SpriteRenderers");

            // effect_create_below drew ef_smoke below every instance, not just the player: every
            // other sprite sharing its sorting layer must draw in front of it. Excludes the smoke's
            // own sibling puff particles, which share its sorting order by design.
            var othersInLayer = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None)
                .Where(r => r.transform.parent != smoke.transform && r.sortingLayerName == puffRenderer.sortingLayerName);
            foreach (SpriteRenderer other in othersInLayer)
            {
                Assert.Less(puffRenderer.sortingOrder, other.sortingOrder,
                    $"{other.name} ({other.sortingLayerName}) must draw in front of the muzzle smoke");
            }

            // Holster so a later test loading the same scene starts from Off.
            session.Submit(new ToggleGun(locomotion.Id));
        }
    }
}

using System;
using Incognito.Player;
using Incognito.World;
using UnityEngine;

namespace Incognito.Weapon
{
    /// <summary>
    /// Something a <see cref="Bullet"/> can hit (needs a Collider2D on this object or a child).
    /// Reproduces obj_npc's obj_bullet collision event: the NPC is shoved 100px away from the
    /// side the bullet came from and the bullet is destroyed. The original wired nothing else to
    /// a hit (no health, no suspicion change). Anything that owns the target's position (an
    /// NpcController's patrol) must send the world a command from <see cref="Struck"/>.
    /// </summary>
    public sealed class BulletTarget : MonoBehaviour
    {
        [Tooltip("Original pixels the target is shoved away from the bullet (obj_npc: 100).")]
        [SerializeField] private float knockback = 100f;

        /// <summary>Raised after the shove, with the signed shove in original pixels.</summary>
        public event Action<float> Hit;

        /// <summary>Like <see cref="Hit"/>, with the player whose bullet it was (for the command that tells the world).</summary>
        public event Action<float, PlayerId> Struck;

        public void TakeHit(float bulletWorldX, PlayerId shooter = default)
        {
            float dx = BulletState.KnockbackX(bulletWorldX, transform.position.x, knockback);
            transform.position += new Vector3(dx * PlayerLocomotion.WorldUnitsPerOriginalPixel, 0f, 0f);
            Hit?.Invoke(dx);
            Struck?.Invoke(dx, shooter);
        }
    }
}

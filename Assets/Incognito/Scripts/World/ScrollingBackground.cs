using Incognito.Player;
using UnityEngine;

namespace Incognito.World
{
    /// <summary>
    /// A room background drawn tiled and scrolled at a fixed speed, as GameMaker's room
    /// backgrounds with htiled + hspeed: the title screen's bg_city_black skyline slides left
    /// 2px per step behind the title. Expects a SpriteRenderer in Tiled draw mode at least one
    /// tile wider than the view; this wraps its offset by one tile width.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class ScrollingBackground : MonoBehaviour
    {
        [Tooltip("hspeed, original px per step (negative scrolls left).")]
        [SerializeField] private float hspeed = -2f;

        private readonly StepClock clock = new StepClock();
        private float tileWidth;
        private float baseX;
        private float offset;

        public float Offset => offset;

        /// <summary>The wrapped offset after moving <paramref name="hspeed"/> for
        /// <paramref name="steps"/> steps, kept in (-tileWidth, 0].</summary>
        public static float Wrap(float offset, float hspeed, int steps, float tileWidth)
        {
            if (tileWidth <= 0f) return 0f;
            offset += hspeed * steps;
            offset %= tileWidth;
            if (offset > 0f) offset -= tileWidth;
            return offset;
        }

        public void Configure(float hspeed) => this.hspeed = hspeed;

        private void Awake()
        {
            var sr = GetComponent<SpriteRenderer>();
            tileWidth = sr.sprite != null ? sr.sprite.rect.width : 0f;
            baseX = transform.localPosition.x;
        }

        private void Update()
        {
            offset = Wrap(offset, hspeed, clock.Advance(Time.deltaTime), tileWidth);
            var p = transform.localPosition;
            transform.localPosition = new Vector3(baseX + offset * PlayerLocomotion.WorldUnitsPerOriginalPixel, p.y, p.z);
        }
    }
}

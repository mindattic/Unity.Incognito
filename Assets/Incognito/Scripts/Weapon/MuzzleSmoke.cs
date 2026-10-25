using System.Collections.Generic;
using Incognito.Player;
using Incognito.Settings;
using Incognito.World;
using UnityEngine;

namespace Incognito.Weapon
{
    /// <summary>
    /// Draws <see cref="MuzzleSmokeState"/>: a code-built puff of small black particles (no
    /// original asset exists for GameMaker's built-in ef_smoke) spawned at the muzzle on every shot
    /// by <see cref="PlayerWeapon.Fire"/>, drawn at whatever sorting layer/order it is given — the
    /// caller fixes that below every actor sprite, the same "below every instance" depth
    /// <c>effect_create_below</c> drew it at — that drifts and fades on its own and then destroys
    /// itself (U.INC-T41).
    /// </summary>
    public sealed class MuzzleSmoke : MonoBehaviour
    {
        private static Sprite puffSprite;

        public MuzzleSmokeState State { get; private set; }

        private readonly StepClock clock = new StepClock();
        private readonly List<SpriteRenderer> pool = new List<SpriteRenderer>();
        private string sortingLayer = "Default";
        private int sortingOrder;

        /// <summary>Spawns one puff at <paramref name="worldPosition"/>, every particle drawn at
        /// <paramref name="sortingLayer"/>/<paramref name="sortingOrder"/>.</summary>
        public static MuzzleSmoke Puff(Vector3 worldPosition, string sortingLayer, int sortingOrder)
        {
            var go = new GameObject("Muzzle Smoke");
            go.transform.position = worldPosition;
            var smoke = go.AddComponent<MuzzleSmoke>();
            smoke.sortingLayer = sortingLayer;
            smoke.sortingOrder = sortingOrder;
            smoke.Init();
            return smoke;
        }

        private void Init()
        {
            State = new MuzzleSmokeState(tuning: GameSettings.Tuning.Weapon);
            for (int i = 0; i < State.Puffs.Count; i++) pool.Add(CreateParticle());
            Sync();
        }

        private void Update() => Advance(clock.Advance(Time.deltaTime));

        /// <summary>One or more 60Hz steps. Public so EditMode tests can drive it without Play mode.</summary>
        public void Advance(int steps)
        {
            if (State == null) return;
            if (steps > 0) State.Step(steps);
            Sync();
            if (!State.IsDone) return;

            // Same as Bullet.Spend(): Destroy is deferred and warns outside Play mode (EditMode tests).
            if (Application.isPlaying) Destroy(gameObject);
            else DestroyImmediate(gameObject);
        }

        private void Sync()
        {
            const float px = PlayerLocomotion.WorldUnitsPerOriginalPixel;
            IReadOnlyList<MuzzleSmokeState.Puff> puffs = State.Puffs;
            for (int i = 0; i < pool.Count; i++)
            {
                bool used = i < puffs.Count;
                if (pool[i].enabled != used) pool[i].enabled = used;
                if (!used) continue;

                MuzzleSmokeState.Puff p = puffs[i];
                pool[i].transform.localPosition = new Vector3(p.X * px, p.Y * px, 0f);
                pool[i].color = new Color(0f, 0f, 0f, State.Alpha(p));
            }
        }

        private SpriteRenderer CreateParticle()
        {
            var go = new GameObject("Puff", typeof(SpriteRenderer));
            go.transform.SetParent(transform, false);
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = PuffSprite();
            sr.sortingLayerName = sortingLayer;
            sr.sortingOrder = sortingOrder;

            const float px = PlayerLocomotion.WorldUnitsPerOriginalPixel;
            float size = GameSettings.Tuning.Weapon.MuzzleSmokeSize * px;
            go.transform.localScale = new Vector3(size, size, 1f);
            return sr;
        }

        /// <summary>A soft round dot, generated once and reused by every puff: ef_smoke is a
        /// GameMaker built-in effect with no original sprite to import.</summary>
        private static Sprite PuffSprite()
        {
            if (puffSprite != null) return puffSprite;

            const int res = 16;
            var tex = new Texture2D(res, res, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[res * res];
            float radius = res * 0.5f;
            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    float dx = x + 0.5f - radius;
                    float dy = y + 0.5f - radius;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) / radius;
                    pixels[y * res + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - dist));
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();

            puffSprite = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res);
            return puffSprite;
        }
    }
}

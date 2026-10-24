using System.Collections.Generic;
using Incognito.Player;
using Incognito.Settings;
using UnityEngine;

namespace Incognito.World
{
    /// <summary>
    /// Draws <see cref="RainState"/> with pooled spr_rain sprites at its tuned alpha, in front of
    /// the player (obj_raindrop depth -1) and behind front props. Drops spawn across the camera's
    /// current view, as obj_raindrop read view_xview. Drop count/size/drift/fall/alpha come from
    /// the validated settings registry (U.INC-T22), not serialized fields here.
    /// </summary>
    public sealed class RainEmitter : MonoBehaviour
    {
        [SerializeField] private Sprite raindrop;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private string sortingLayer = "Default";
        [SerializeField] private int sortingOrder = 2;

        public RainState State { get; private set; }

        private readonly StepClock clock = new StepClock();
        private readonly List<SpriteRenderer> pool = new List<SpriteRenderer>();

        public void Configure(Sprite raindrop, Camera viewCamera, string sortingLayer, int sortingOrder)
        {
            this.raindrop = raindrop;
            this.viewCamera = viewCamera;
            this.sortingLayer = sortingLayer;
            this.sortingOrder = sortingOrder;
        }

        private void Awake()
        {
            State = new RainState(tuning: GameSettings.Tuning.Rain);
        }

        private void Update()
        {
            var cam = viewCamera != null ? viewCamera : Camera.main;
            if (cam == null) return;

            const float px = PlayerLocomotion.WorldUnitsPerOriginalPixel;
            float viewWidth = cam.orthographicSize * 2f * cam.aspect;
            float viewLeft = cam.transform.position.x - viewWidth * 0.5f;
            State.Step(clock.Advance(Time.deltaTime), viewLeft / px, viewWidth / px);

            var drops = State.Drops;
            while (pool.Count < drops.Count) pool.Add(CreateDrop());
            for (int i = 0; i < pool.Count; i++)
            {
                bool used = i < drops.Count;
                if (pool[i].enabled != used) pool[i].enabled = used;
                if (!used) continue;
                var d = drops[i];
                pool[i].transform.position = new Vector3(d.X * px, (State.RoomHeight - d.Y) * px, 0f);
            }
        }

        private SpriteRenderer CreateDrop()
        {
            var go = new GameObject("Raindrop", typeof(SpriteRenderer));
            go.transform.SetParent(transform, false);
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = raindrop;
            sr.sortingLayerName = sortingLayer;
            sr.sortingOrder = sortingOrder;
            sr.color = new Color(1f, 1f, 1f, State.Alpha);
            return sr;
        }
    }
}

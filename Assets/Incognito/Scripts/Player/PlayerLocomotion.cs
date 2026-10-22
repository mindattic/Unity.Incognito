using Incognito.Terminal;
using UnityEngine;

namespace Incognito.Player
{
    /// <summary>
    /// Drives <see cref="PlayerLocomotionState"/> from input and applies it to the
    /// transform/sprite. See docs/RECONSTRUCTION.md §1. Arm swing, aiming, camera and eye
    /// tracking are other tickets (U.INC-T4/T5/T9) and are not handled here.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PlayerLocomotion : MonoBehaviour
    {
        [SerializeField] private float bobAmplitude = 5f;
        [SerializeField] private float bobRampSpeed = 0.3f;
        [SerializeField] private float walkSpeed = 4f;
        [SerializeField] private float sprintMultiplier = 2f;

        public PlayerLocomotionState State { get; private set; }

        private SpriteRenderer spriteRenderer;
        private float baseY;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            baseY = transform.position.y;

            State = new PlayerLocomotionState
            {
                BobAmplitude = bobAmplitude,
                BobRampSpeed = bobRampSpeed,
                WalkSpeed = walkSpeed,
                SprintMultiplier = sprintMultiplier,
            };
            State.SetInitialPositionX(transform.position.x / WorldUnitsPerOriginalPixel);
        }

        private void Update()
        {
            // While the typed console is open the keyboard is typing, not walking (U.INC-T10).
            bool keys = !CommandPrompt.CapturesKeyboard;
            bool moveLeft = keys && (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A));
            bool moveRight = keys && (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D));
            bool sprint = keys && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

            State.Step(Time.deltaTime, moveLeft, moveRight, sprint);

            transform.position = new Vector3(
                State.PositionX * WorldUnitsPerOriginalPixel,
                baseY + State.BobOffset * WorldUnitsPerOriginalPixel,
                transform.position.z);
            spriteRenderer.flipX = State.Facing == PlayerLocomotionState.FacingDirection.Right;
        }

        // The original ran in GameMaker pixel units; a real art pass (U.INC-T11) will set the
        // final scale. 1 original pixel ~= 1/32 Unity unit is a placeholder matching a typical
        // 32px-per-unit pixel-art import.
        public const float WorldUnitsPerOriginalPixel = 1f / 32f;
    }
}

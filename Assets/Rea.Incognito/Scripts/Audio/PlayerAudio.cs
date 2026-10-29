using Incognito.Player;
using Incognito.World;
using UnityEngine;

namespace Incognito.Audio
{
    /// <summary>
    /// The player's own sounds from obj_walker (RECONSTRUCTION.md §8): play_footstep("concrete")
    /// — one of the 16 sfx_step_concrete## at random — each time the walk bob peaks, and
    /// sfx_whoosh when a walk key turns the player around. Weapon sounds stay with
    /// Incognito.Weapon.PlayerWeapon.
    /// </summary>
    [RequireComponent(typeof(PlayerLocomotion))]
    public sealed class PlayerAudio : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        [Tooltip("sfx_step_concrete01..16; one is chosen at random per step (choose()).")]
        [SerializeField] private AudioClip[] footsteps = new AudioClip[0];
        [SerializeField] private AudioClip whoosh;

        private PlayerLocomotion locomotion;
        private GameSession session;
        private PlayerLocomotionState.FacingDirection lastFacing;

        /// <summary>Sets the room's audio source and clips (U.INC-T26's RoomComposer): the original
        /// sfx_step_concrete01..16 and sfx_whoosh, imported per room by the editor builder.</summary>
        public void Configure(AudioSource audioSource, AudioClip[] footsteps, AudioClip whoosh)
        {
            this.audioSource = audioSource;
            this.footsteps = footsteps ?? new AudioClip[0];
            this.whoosh = whoosh;
        }

        private void Start()
        {
            locomotion = GetComponent<PlayerLocomotion>();
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            lastFacing = locomotion.State.Facing;
            session = locomotion.Session;
            if (session != null) session.Played += OnPlayed;
        }

        private void OnDestroy()
        {
            if (session != null) session.Played -= OnPlayed;
        }

        // The walk bob peaked: a footstep.
        private void OnPlayed(GameEvent e)
        {
            if (e is PlayerFootstep step && step.Player == locomotion.Id) PlayFootstep();
        }

        private void LateUpdate()
        {
            var state = locomotion.State;
            // obj_walker's walk-key press: if (facing == left) audio_play_sound(sfx_whoosh) before
            // facing = right (and the mirror). Turning in place to face a talking NPC (idle) and
            // walking off under autowalk don't whoosh.
            if (state.Facing != lastFacing && state.CurrentMode == PlayerLocomotionState.WalkMode.Walking && !locomotion.AutoWalk)
            {
                Play(whoosh);
            }
            lastFacing = state.Facing;
        }

        private void PlayFootstep()
        {
            if (footsteps == null || footsteps.Length == 0) return;
            Play(footsteps[Random.Range(0, footsteps.Length)]);
        }

        private void Play(AudioClip clip)
        {
            if (clip != null && audioSource != null) audioSource.PlayOneShot(clip);
        }
    }
}

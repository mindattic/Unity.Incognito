using System;
using Unity.Shared.Core;
using UnityEngine;

namespace Incognito.Audio
{
    // Honours the player's shared Unity.Shared profile audio settings for the whole game, which is how the
    // CLI mute reaches it: launched with -muted or MINDATTIC_CLI_MUTED=1, Unity.Shared's bootstrap turns on
    // its session mute before the first scene loads, and the profile's CommonSettings then report music and
    // sound effects muted at volume 0. Unlike QuickDuel (sfx only, no music), Incognito has both a
    // MusicPlayer and sound effects (PlayerAudio, the HUD flick, the weapon), so the listener is only
    // silenced when BOTH channels report muted -- the CLI mute forces both, but a player who only muted one
    // channel of their own should not lose the other. That silences the listener for this run only:
    // AudioListener.volume is never saved, and settings.json is never written.
    public static class SessionAudio
    {
        // True once Apply found the profile's music and sound effects both muted (the CLI mute, or the
        // player's own settings muting both channels).
        public static bool Muted { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnLoad() => Apply();

        // Reads the current profile's audio settings and mutes the listener if both channels say so. Never
        // unmutes what something else muted. Returns Muted.
        public static bool Apply()
        {
            Muted = ProfileMutesSound();
            if (Muted)
            {
                AudioListener.volume = 0f;
                Debug.Log("Incognito: audio muted for this session (Unity.Shared profile mute; -muted / MINDATTIC_CLI_MUTED=1).");
            }
            return Muted;
        }

        private static bool ProfileMutesSound()
        {
            if (!Profiles.IsConfigured)
                return false;
            try
            {
                var common = Profiles.Current.Settings.Common;
                bool musicMuted = common.MuteMusic || common.MusicVolume <= 0f;
                bool sfxMuted = common.MuteSfx || common.SfxVolume <= 0f;
                return musicMuted && sfxMuted;
            }
            catch (Exception e)
            {
                // A profile folder the disk refuses must not stop the game; it just plays with sound.
                Debug.LogWarning($"Incognito: could not read the profile's audio settings ({e.Message}); sound stays on.");
                return false;
            }
        }
    }
}

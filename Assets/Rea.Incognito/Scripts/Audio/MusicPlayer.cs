using UnityEngine;

namespace Incognito.Audio
{
    /// <summary>
    /// obj_music: each room names its track (current_track) and the music keeps playing across
    /// room changes, switching only when a room asks for a different track — so the title, Bleecher
    /// Street and 2nd Avenue share one unbroken music_oppressivegloom and the shop cuts to
    /// music_dubferal. Looped, as audio_play_music(track, 1) was. One persistent player for the
    /// whole session.
    /// </summary>
    public sealed class MusicPlayer : MonoBehaviour
    {
        private static MusicPlayer instance;
        private AudioSource source;

        public static AudioClip Current => instance != null ? instance.source.clip : null;

        /// <summary>True if switching from <paramref name="current"/> to
        /// <paramref name="requested"/> restarts the music (a different track, or none playing).</summary>
        public static bool ShouldSwitch(AudioClip current, bool currentPlaying, AudioClip requested)
        {
            if (requested == null) return false;
            return current != requested || !currentPlaying;
        }

        public static void Play(AudioClip track, float volume = 1f)
        {
            if (track == null) return;
            if (instance == null)
            {
                var go = new GameObject("Music");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<MusicPlayer>();
                instance.source = go.AddComponent<AudioSource>();
                instance.source.loop = true;
                instance.source.playOnAwake = false;
            }

            instance.source.volume = volume;
            if (!ShouldSwitch(instance.source.clip, instance.source.isPlaying, track)) return;
            instance.source.clip = track;
            instance.source.Play();
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }
    }
}

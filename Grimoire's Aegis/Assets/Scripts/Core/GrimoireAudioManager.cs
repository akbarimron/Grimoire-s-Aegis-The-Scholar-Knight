using System.Collections.Generic;
using UnityEngine;

namespace GrimoireAegis.Core
{
    public class GrimoireAudioManager : MonoBehaviour
    {
        public static GrimoireAudioManager Instance { get; private set; }

        private AudioSource _sfxSource;
        private AudioSource _musicSource;
        private readonly Dictionary<string, AudioClip> _clipCache = new Dictionary<string, AudioClip>();
        private string _currentMusicKey = "";

        public float SfxVolume { get; private set; } = 0.85f;
        public float MusicVolume { get; private set; } = 0.45f;
        public bool IsMuted { get; private set; } = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;

            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.loop = true;
            _musicSource.volume = MusicVolume;
        }

        private AudioClip GetClip(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (_clipCache.TryGetValue(key, out AudioClip cached)) return cached;

            AudioClip clip = Resources.Load<AudioClip>("Gen/audio/" + key);
            if (clip != null)
            {
                _clipCache[key] = clip;
            }
            return clip;
        }

        public void PlaySfx(string key, float volumeScale = 1.0f)
        {
            if (IsMuted || _sfxSource == null) return;
            AudioClip clip = GetClip(key);
            if (clip != null)
            {
                _sfxSource.PlayOneShot(clip, Mathf.Clamp01(SfxVolume * volumeScale));
            }
        }

        public void PlayMusic(string key)
        {
            if (_musicSource == null) return;
            if (_currentMusicKey == key && _musicSource.isPlaying) return;

            AudioClip clip = GetClip(key);
            if (clip == null) return;

            _currentMusicKey = key;
            _musicSource.clip = clip;
            _musicSource.volume = IsMuted ? 0f : MusicVolume;
            _musicSource.Play();
        }

        public void SetVolumes(float sfx, float music, bool muted)
        {
            SfxVolume = Mathf.Clamp01(sfx);
            MusicVolume = Mathf.Clamp01(music);
            IsMuted = muted;
            if (_musicSource != null)
            {
                _musicSource.volume = IsMuted ? 0f : MusicVolume;
            }
        }
    }
}

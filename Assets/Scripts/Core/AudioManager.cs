using UnityEngine;
using System.Collections.Generic;

namespace PastGuardians.Core
{
    /// <summary>
    /// Sound effect types
    /// </summary>
    public enum SoundEffect
    {
        Tap,
        TapSuccess,
        IntruderSpawn,
        IntruderReturn,
        PortalOpen,
        PortalClose,
        LaserStart,
        LaserEnd,
        Celebration,
        RankUp,
        CoinEarned,
        ButtonClick,
        Error,
        Whoosh
    }

    /// <summary>
    /// Manages all game audio - generates placeholder sounds procedurally
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private bool soundEnabled = true;
        [SerializeField] [Range(0f, 1f)] private float masterVolume = 0.7f;
        [SerializeField] [Range(0f, 1f)] private float sfxVolume = 1f;
        [SerializeField] [Range(0f, 1f)] private float musicVolume = 0.5f;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource musicSource;

        // Generated audio clips
        private Dictionary<SoundEffect, AudioClip> soundClips = new Dictionary<SoundEffect, AudioClip>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            SetupAudioSources();
            GenerateAllSounds();
        }

        /// <summary>
        /// Set up audio source components
        /// </summary>
        private void SetupAudioSources()
        {
            // SFX Source
            if (sfxSource == null)
            {
                GameObject sfxObj = new GameObject("SFX Source");
                sfxObj.transform.SetParent(transform);
                sfxSource = sfxObj.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }

            // Music Source
            if (musicSource == null)
            {
                GameObject musicObj = new GameObject("Music Source");
                musicObj.transform.SetParent(transform);
                musicSource = musicObj.AddComponent<AudioSource>();
                musicSource.playOnAwake = false;
                musicSource.loop = true;
            }
        }

        /// <summary>
        /// Generate all placeholder sound effects procedurally
        /// </summary>
        private void GenerateAllSounds()
        {
            soundClips[SoundEffect.Tap] = GenerateTapSound();
            soundClips[SoundEffect.TapSuccess] = GenerateSuccessSound();
            soundClips[SoundEffect.IntruderSpawn] = GenerateWhooshSound();
            soundClips[SoundEffect.IntruderReturn] = GenerateReturnSound();
            soundClips[SoundEffect.PortalOpen] = GeneratePortalOpenSound();
            soundClips[SoundEffect.PortalClose] = GeneratePortalCloseSound();
            soundClips[SoundEffect.LaserStart] = GenerateLaserSound();
            soundClips[SoundEffect.LaserEnd] = GenerateLaserEndSound();
            soundClips[SoundEffect.Celebration] = GenerateCelebrationSound();
            soundClips[SoundEffect.RankUp] = GenerateRankUpSound();
            soundClips[SoundEffect.CoinEarned] = GenerateCoinSound();
            soundClips[SoundEffect.ButtonClick] = GenerateClickSound();
            soundClips[SoundEffect.Error] = GenerateErrorSound();
            soundClips[SoundEffect.Whoosh] = GenerateWhooshSound();

            Debug.Log($"[AudioManager] Generated {soundClips.Count} sound effects");
        }

        /// <summary>
        /// Play a sound effect
        /// </summary>
        public void PlaySound(SoundEffect effect, float volumeMultiplier = 1f)
        {
            if (!soundEnabled) return;
            if (!soundClips.ContainsKey(effect)) return;

            float volume = masterVolume * sfxVolume * volumeMultiplier;
            sfxSource.PlayOneShot(soundClips[effect], volume);
        }

        /// <summary>
        /// Play tap sound (called frequently, uses pooling)
        /// </summary>
        public void PlayTap()
        {
            PlaySound(SoundEffect.Tap, 0.5f);
        }

        /// <summary>
        /// Play coin earned sound
        /// </summary>
        public void PlayCoinEarned()
        {
            PlaySound(SoundEffect.CoinEarned, 0.6f);
        }

        /// <summary>
        /// Play intruder return celebration
        /// </summary>
        public void PlayIntruderReturn()
        {
            PlaySound(SoundEffect.IntruderReturn);
            PlaySound(SoundEffect.Celebration, 0.7f);
        }

        /// <summary>
        /// Toggle sound on/off
        /// </summary>
        public void SetSoundEnabled(bool enabled)
        {
            soundEnabled = enabled;
        }

        /// <summary>
        /// Set master volume
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
        }

        // ===== PROCEDURAL SOUND GENERATION =====
        // These generate simple placeholder sounds using sine waves
        // Replace with real audio files later

        private AudioClip GenerateTapSound()
        {
            return GenerateTone(0.05f, 800f, 1200f, ToneType.Blip);
        }

        private AudioClip GenerateSuccessSound()
        {
            return GenerateTone(0.15f, 600f, 900f, ToneType.Rising);
        }

        private AudioClip GenerateWhooshSound()
        {
            return GenerateNoise(0.3f, NoiseType.Whoosh);
        }

        private AudioClip GenerateReturnSound()
        {
            return GenerateTone(0.5f, 400f, 800f, ToneType.Sweep);
        }

        private AudioClip GeneratePortalOpenSound()
        {
            return GenerateTone(0.4f, 200f, 600f, ToneType.Rising);
        }

        private AudioClip GeneratePortalCloseSound()
        {
            return GenerateTone(0.3f, 600f, 200f, ToneType.Falling);
        }

        private AudioClip GenerateLaserSound()
        {
            return GenerateTone(0.1f, 1000f, 1500f, ToneType.Buzz);
        }

        private AudioClip GenerateLaserEndSound()
        {
            return GenerateTone(0.1f, 1500f, 800f, ToneType.Falling);
        }

        private AudioClip GenerateCelebrationSound()
        {
            return GenerateTone(0.4f, 500f, 1000f, ToneType.Fanfare);
        }

        private AudioClip GenerateRankUpSound()
        {
            return GenerateTone(0.6f, 400f, 1200f, ToneType.Fanfare);
        }

        private AudioClip GenerateCoinSound()
        {
            return GenerateTone(0.08f, 1200f, 1800f, ToneType.Blip);
        }

        private AudioClip GenerateClickSound()
        {
            return GenerateTone(0.03f, 600f, 800f, ToneType.Blip);
        }

        private AudioClip GenerateErrorSound()
        {
            return GenerateTone(0.2f, 300f, 200f, ToneType.Buzz);
        }

        private enum ToneType { Blip, Rising, Falling, Sweep, Buzz, Fanfare }
        private enum NoiseType { White, Whoosh }

        private AudioClip GenerateTone(float duration, float startFreq, float endFreq, ToneType type)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(duration * sampleRate);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float envelope = GetEnvelope(t, type);
                float freq = Mathf.Lerp(startFreq, endFreq, t);

                float sample = 0f;
                switch (type)
                {
                    case ToneType.Blip:
                    case ToneType.Rising:
                    case ToneType.Falling:
                        sample = Mathf.Sin(2 * Mathf.PI * freq * i / sampleRate);
                        break;
                    case ToneType.Sweep:
                        sample = Mathf.Sin(2 * Mathf.PI * freq * i / sampleRate) * 0.7f +
                                 Mathf.Sin(4 * Mathf.PI * freq * i / sampleRate) * 0.3f;
                        break;
                    case ToneType.Buzz:
                        sample = Mathf.Sign(Mathf.Sin(2 * Mathf.PI * freq * i / sampleRate)) * 0.5f;
                        break;
                    case ToneType.Fanfare:
                        float f1 = Mathf.Lerp(startFreq, endFreq, t);
                        float f2 = f1 * 1.5f; // Fifth
                        float f3 = f1 * 2f;   // Octave
                        sample = (Mathf.Sin(2 * Mathf.PI * f1 * i / sampleRate) +
                                  Mathf.Sin(2 * Mathf.PI * f2 * i / sampleRate) * 0.5f +
                                  Mathf.Sin(2 * Mathf.PI * f3 * i / sampleRate) * 0.3f) / 1.8f;
                        break;
                }

                samples[i] = sample * envelope;
            }

            AudioClip clip = AudioClip.Create("Generated", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private float GetEnvelope(float t, ToneType type)
        {
            switch (type)
            {
                case ToneType.Blip:
                    return 1f - t; // Quick decay
                case ToneType.Rising:
                    return Mathf.Sin(t * Mathf.PI); // Smooth rise and fall
                case ToneType.Falling:
                    return 1f - t * t; // Faster decay
                case ToneType.Sweep:
                    return Mathf.Sin(t * Mathf.PI * 0.5f); // Slow rise, sustain
                case ToneType.Buzz:
                    return t < 0.1f ? t * 10f : (1f - t); // Quick attack, decay
                case ToneType.Fanfare:
                    if (t < 0.1f) return t * 10f; // Attack
                    if (t > 0.7f) return (1f - t) / 0.3f; // Release
                    return 1f; // Sustain
                default:
                    return 1f;
            }
        }

        private AudioClip GenerateNoise(float duration, NoiseType type)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(duration * sampleRate);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float envelope = Mathf.Sin(t * Mathf.PI); // Fade in and out

                float sample = Random.Range(-1f, 1f);

                if (type == NoiseType.Whoosh)
                {
                    // Filter the noise for whoosh effect
                    if (i > 0)
                    {
                        sample = samples[i - 1] * 0.9f + sample * 0.1f;
                    }
                }

                samples[i] = sample * envelope * 0.3f;
            }

            AudioClip clip = AudioClip.Create("Noise", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}

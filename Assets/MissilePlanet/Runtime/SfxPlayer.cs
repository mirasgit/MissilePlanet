using System;
using UnityEngine;

namespace MissilePlanet
{
    /// <summary>
    /// Centralized sound-effects player. Uses a small pool of AudioSources so pooled gameplay
    /// objects can despawn immediately without cutting their sounds. If no external clips are
    /// assigned, the four default effects are generated procedurally at runtime.
    /// </summary>
    public sealed class SfxPlayer : MonoBehaviour
    {
        [Header("Optional overrides")]
        [SerializeField] private AudioClip _shotClip;
        [SerializeField] private AudioClip _missileExplosionClip;
        [SerializeField] private AudioClip _asteroidDestroyedClip;
        [SerializeField] private AudioClip _planetImpactClip;
        [SerializeField] private AudioClip _planetPulseClip;

        [Header("Mix")]
        [SerializeField, Range(0f, 1f)] private float _masterVolume = 0.78f;
        [SerializeField, Range(4, 24)] private int _voiceCount = 12;
        [SerializeField] private Camera _camera;

        private AudioSource[] _voices;
        private int _nextVoice;

        public void Configure(Camera gameCamera)
        {
            _camera = gameCamera;
        }

        private void Awake()
        {
            if (_camera == null)
                _camera = Camera.main;

            BuildVoices();
            BuildFallbackClips();
        }

        public void PlayShot(Vector2 worldPosition)
        {
            Play(_shotClip, worldPosition, 0.52f, 0.96f, 1.06f);
        }

        public void PlayMissileExplosion(Vector2 worldPosition)
        {
            Play(_missileExplosionClip, worldPosition, 0.78f, 0.94f, 1.04f);
        }

        public void PlayAsteroidDestroyed(Vector2 worldPosition)
        {
            Play(_asteroidDestroyedClip, worldPosition, 0.62f, 0.92f, 1.10f);
        }

        public void PlayPlanetImpact(Vector2 worldPosition)
        {
            Play(_planetImpactClip, worldPosition, 0.90f, 0.94f, 1.02f);
        }

        public void PlayPlanetPulse(Vector2 worldPosition)
        {
            Play(_planetPulseClip, worldPosition, 0.88f, 0.98f, 1.02f);
        }

        private void BuildVoices()
        {
            int count = Mathf.Clamp(_voiceCount, 4, 24);
            _voices = new AudioSource[count];

            for (int i = 0; i < count; i++)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0f;
                source.dopplerLevel = 0f;
#if UNITY_EDITOR
                // AudioSource gizmos are not useful for this central 2D mixer and can appear
                // as a speaker icon in the Game view when Gizmos are enabled.
                source.hideFlags = HideFlags.HideInInspector;
#endif
                _voices[i] = source;
            }
        }

        private void Play(AudioClip clip, Vector2 worldPosition, float volume, float pitchMin, float pitchMax)
        {
            if (!GameSettings.SfxEnabled || clip == null || _voices == null || _voices.Length == 0)
                return;

            AudioSource source = GetVoice();
            source.Stop();
            source.clip = clip;
            source.volume = Mathf.Clamp01(volume * _masterVolume);
            source.pitch = UnityEngine.Random.Range(pitchMin, pitchMax);
            source.panStereo = GetStereoPan(worldPosition);
            source.Play();
        }

        private AudioSource GetVoice()
        {
            for (int i = 0; i < _voices.Length; i++)
            {
                int index = (_nextVoice + i) % _voices.Length;
                if (!_voices[index].isPlaying)
                {
                    _nextVoice = (index + 1) % _voices.Length;
                    return _voices[index];
                }
            }

            AudioSource fallback = _voices[_nextVoice];
            _nextVoice = (_nextVoice + 1) % _voices.Length;
            return fallback;
        }

        private float GetStereoPan(Vector2 worldPosition)
        {
            if (_camera == null)
                return 0f;

            Vector3 viewport = _camera.WorldToViewportPoint(worldPosition);
            return Mathf.Clamp((viewport.x - 0.5f) * 1.6f, -0.8f, 0.8f);
        }

        private void BuildFallbackClips()
        {
            if (_shotClip == null)
                _shotClip = CreateShotClip();
            if (_missileExplosionClip == null)
                _missileExplosionClip = CreateExplosionClip("MissileExplosion_Procedural", 0.44f, 0.78f, 63f);
            if (_asteroidDestroyedClip == null)
                _asteroidDestroyedClip = CreateAsteroidDestroyedClip();
            if (_planetImpactClip == null)
                _planetImpactClip = CreateExplosionClip("PlanetImpact_Procedural", 0.62f, 0.92f, 48f);
            if (_planetPulseClip == null)
                _planetPulseClip = CreatePulseClip();
        }


        private static AudioClip CreatePulseClip()
        {
            const int sampleRate = 44100;
            const float duration = 0.72f;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[sampleCount];
            float lowPhase = 0f;
            float highPhase = 0f;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;
                float normalized = t / duration;
                float attack = Mathf.Clamp01(t / 0.018f);
                float envelope = attack * Mathf.Pow(1f - normalized, 1.35f);

                float lowFrequency = Mathf.Lerp(78f, 42f, normalized);
                float highFrequency = Mathf.Lerp(360f, 110f, normalized);
                lowPhase += 2f * Mathf.PI * lowFrequency / sampleRate;
                highPhase += 2f * Mathf.PI * highFrequency / sampleRate;

                float sweep = Mathf.Sin(lowPhase) * 0.62f + Mathf.Sin(highPhase) * 0.24f;
                data[i] = Mathf.Clamp(sweep * envelope, -1f, 1f);
            }

            return CreateClip("PlanetPulse_Procedural", data, sampleRate);
        }

        private static AudioClip CreateShotClip()
        {
            const int sampleRate = 44100;
            const float duration = 0.18f;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[sampleCount];
            uint noiseState = 0xA341316Cu;
            float phase = 0f;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;
                float normalized = t / duration;
                float frequency = Mathf.Lerp(780f, 210f, normalized);
                phase += 2f * Mathf.PI * frequency / sampleRate;

                float attack = Mathf.Clamp01(t / 0.008f);
                float decay = Mathf.Pow(1f - normalized, 2.2f);
                float envelope = attack * decay;
                float noise = NextNoise(ref noiseState);

                data[i] = Mathf.Clamp(
                    (Mathf.Sin(phase) * 0.78f + noise * 0.12f) * envelope,
                    -1f,
                    1f);
            }

            return CreateClip("Shot_Procedural", data, sampleRate);
        }

        private static AudioClip CreateAsteroidDestroyedClip()
        {
            const int sampleRate = 44100;
            const float duration = 0.24f;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[sampleCount];
            uint noiseState = 0xC8013EA4u;
            float phase = 0f;
            float smoothedNoise = 0f;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;
                float normalized = t / duration;
                float envelope = Mathf.Pow(1f - normalized, 3.2f);
                float frequency = Mathf.Lerp(390f, 115f, normalized);
                phase += 2f * Mathf.PI * frequency / sampleRate;

                float rawNoise = NextNoise(ref noiseState);
                smoothedNoise = Mathf.Lerp(smoothedNoise, rawNoise, 0.42f);
                float crack = rawNoise * 0.48f + smoothedNoise * 0.28f;
                float tone = Mathf.Sin(phase) * 0.32f;

                data[i] = Mathf.Clamp((crack + tone) * envelope, -1f, 1f);
            }

            return CreateClip("AsteroidDestroyed_Procedural", data, sampleRate);
        }

        private static AudioClip CreateExplosionClip(string name, float duration, float strength, float baseFrequency)
        {
            const int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[sampleCount];
            uint noiseState = name == "PlanetImpact_Procedural" ? 0x9E3779B9u : 0x7F4A7C15u;
            float phase = 0f;
            float smoothedNoise = 0f;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;
                float normalized = t / duration;
                float attack = Mathf.Clamp01(t / 0.006f);
                float envelope = attack * Mathf.Pow(1f - normalized, 2.1f);
                float frequency = Mathf.Lerp(baseFrequency, baseFrequency * 0.52f, normalized);
                phase += 2f * Mathf.PI * frequency / sampleRate;

                float rawNoise = NextNoise(ref noiseState);
                smoothedNoise += (rawNoise - smoothedNoise) * 0.055f;
                float boom = Mathf.Sin(phase) * 0.72f;
                float body = smoothedNoise * 0.82f + rawNoise * 0.14f;

                data[i] = Mathf.Clamp((boom + body) * envelope * strength, -1f, 1f);
            }

            return CreateClip(name, data, sampleRate);
        }

        private static AudioClip CreateClip(string name, float[] samples, int sampleRate)
        {
            AudioClip clip = AudioClip.Create(name, samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static float NextNoise(ref uint state)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            return (state / (float)uint.MaxValue) * 2f - 1f;
        }
    }
}

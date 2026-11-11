using UnityEngine;

namespace MissilePlanet
{
    public enum MusicMode
    {
        Menu,
        Gameplay,
        Boss
    }

    /// <summary>
    /// Lightweight placeholder music system. External tracks can simply be assigned in Inspector.
    /// If clips are missing, low-volume procedural ambience is generated at runtime.
    /// </summary>
    public sealed class MusicPlayer : MonoBehaviour
    {
        [SerializeField] private AudioClip _menuClip;
        [SerializeField] private AudioClip _gameplayClip;
        [SerializeField] private AudioClip _bossClip;
        [SerializeField, Range(0f, 1f)] private float _volume = 0.18f;

        private AudioSource _source;
        private MusicMode _mode = MusicMode.Menu;

        private void Awake()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = true;
            _source.spatialBlend = 0f;
            _source.dopplerLevel = 0f;
#if UNITY_EDITOR
            _source.hideFlags = HideFlags.HideInInspector;
#endif
            BuildFallbackClips();
            SetMode(MusicMode.Menu, true);
        }

        private void Update()
        {
            if (_source == null) return;
            float desired = GameSettings.MusicEnabled ? _volume : 0f;
            _source.volume = Mathf.MoveTowards(_source.volume, desired, Time.unscaledDeltaTime * 0.28f);
            if (GameSettings.MusicEnabled && !_source.isPlaying && _source.clip != null)
                _source.Play();
        }

        public void SetMode(MusicMode mode, bool force = false)
        {
            if (!force && _mode == mode)
                return;

            _mode = mode;
            AudioClip next = mode == MusicMode.Boss ? _bossClip : mode == MusicMode.Gameplay ? _gameplayClip : _menuClip;
            if (_source == null || next == null)
                return;

            if (_source.clip == next && _source.isPlaying)
                return;

            _source.clip = next;
            _source.time = 0f;
            if (GameSettings.MusicEnabled)
                _source.Play();
        }

        private void BuildFallbackClips()
        {
            if (_menuClip == null) _menuClip = CreateAmbient("MenuAmbient_Procedural", 48f, 72f, 0.08f, 11f);
            if (_gameplayClip == null) _gameplayClip = CreateAmbient("GameplayAmbient_Procedural", 58f, 116f, 0.10f, 10f);
            if (_bossClip == null) _bossClip = CreateAmbient("BossAmbient_Procedural", 74f, 148f, 0.14f, 8f);
        }

        private static AudioClip CreateAmbient(string name, float lowHz, float highHz, float strength, float duration)
        {
            const int sampleRate = 22050;
            int count = Mathf.CeilToInt(duration * sampleRate);
            float[] samples = new float[count];
            float phaseA = 0f;
            float phaseB = 0f;
            float phaseC = 0f;

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)sampleRate;
                float wobble = 0.5f + 0.5f * Mathf.Sin(t * 0.37f);
                phaseA += 2f * Mathf.PI * lowHz / sampleRate;
                phaseB += 2f * Mathf.PI * Mathf.Lerp(lowHz * 1.5f, highHz, wobble) / sampleRate;
                phaseC += 2f * Mathf.PI * (lowHz * 0.5f) / sampleRate;

                float fade = Mathf.Clamp01(t / 0.8f) * Mathf.Clamp01((duration - t) / 0.8f);
                float value = Mathf.Sin(phaseA) * 0.50f + Mathf.Sin(phaseB) * 0.22f + Mathf.Sin(phaseC) * 0.28f;
                samples[i] = value * strength * fade;
            }

            AudioClip clip = AudioClip.Create(name, count, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}

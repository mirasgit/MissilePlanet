using UnityEngine;

namespace MissilePlanet
{
    public enum BossEnginePattern
    {
        RapidNormal,
        HeavyRed,
        ArmoredGreen,
        ScatterNormal
    }

    public sealed class BossEngine : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private SpriteRenderer _reloadGlow;

        private AlienBoss _boss;
        private int _engineIndex;
        private int _shotsPerCycle;
        private float _shotInterval;
        private float _reloadDuration;
        private float _hitCooldownDuration;
        private BossEnginePattern _pattern;
        private int _shotsFired;
        private int _hitPoints;
        private int _maxHitPoints;
        private float _timer;
        private float _hitCooldown;
        private float _damageFlash;
        private bool _reloading;
        private bool _alive;
        private float _phase;

        public bool IsAlive => _alive;
        public bool IsReloading => _alive && _reloading;
        public float CollisionRadius { get; private set; } = 0.42f;
        public int EngineIndex => _engineIndex;
        public int HitPoints => _hitPoints;
        public int MaxHitPoints => _maxHitPoints;

        private void Awake()
        {
            if (_renderer == null)
                _renderer = GetComponent<SpriteRenderer>();
            if (_reloadGlow == null)
            {
                Transform glow = transform.Find("ReloadGlow");
                if (glow != null)
                    _reloadGlow = glow.GetComponent<SpriteRenderer>();
            }
        }

        public void Initialize(
            AlienBoss boss,
            int engineIndex,
            BossEnginePattern pattern,
            int shotsPerCycle,
            float shotInterval,
            float reloadDuration,
            int hitPoints,
            float hitCooldown)
        {
            _boss = boss;
            _engineIndex = engineIndex;
            _pattern = pattern;
            _shotsPerCycle = Mathf.Max(1, shotsPerCycle);
            _shotInterval = Mathf.Max(0.1f, shotInterval);
            _reloadDuration = Mathf.Max(0.5f, reloadDuration);
            _hitCooldownDuration = Mathf.Max(0.05f, hitCooldown);
            _maxHitPoints = Mathf.Max(1, hitPoints);
            _hitPoints = _maxHitPoints;
            _shotsFired = 0;
            _timer = 0.75f + engineIndex * 0.35f;
            _hitCooldown = 0f;
            _damageFlash = 0f;
            _reloading = false;
            _alive = true;
            _phase = Random.Range(0f, Mathf.PI * 2f);

            if (_renderer != null)
            {
                _renderer.enabled = true;
                _renderer.color = GetActiveColor();
            }

            if (_reloadGlow != null)
                _reloadGlow.enabled = false;
        }

        private void Update()
        {
            if (!_alive || _boss == null || !_boss.IsActive)
                return;

            if (_hitCooldown > 0f)
                _hitCooldown -= Time.deltaTime;
            if (_damageFlash > 0f)
                _damageFlash -= Time.deltaTime;

            _timer -= Time.deltaTime;

            if (_reloading)
            {
                UpdateReloadVisual();
                if (_timer <= 0f)
                {
                    _reloading = false;
                    _shotsFired = 0;
                    _timer = _shotInterval / _boss.FireRateMultiplier;
                    if (_renderer != null)
                        _renderer.color = GetActiveColor();
                    if (_reloadGlow != null)
                        _reloadGlow.enabled = false;
                }
                return;
            }

            if (_timer > 0f)
                return;

            _boss.FireEngine(this, _pattern);
            _shotsFired++;

            if (_shotsFired >= _shotsPerCycle)
            {
                _reloading = true;
                _timer = _reloadDuration / _boss.ReloadRateMultiplier;
                if (_reloadGlow != null)
                    _reloadGlow.enabled = true;
            }
            else
            {
                _timer = _shotInterval / _boss.FireRateMultiplier;
            }
        }

        /// <summary>
        /// Returns true when this explosion actually applied one point of damage.
        /// Engines only accept damage while visibly reloading/overheated.
        /// </summary>
        public bool TryHit()
        {
            if (!_alive || !_reloading || _hitCooldown > 0f || _boss == null || !_boss.CanEngineTakeDamage(this))
                return false;

            _hitCooldown = _hitCooldownDuration;
            _damageFlash = 0.16f;
            _hitPoints = Mathf.Max(0, _hitPoints - 1);

            if (_hitPoints > 0)
                return true;

            _alive = false;
            if (_renderer != null)
                _renderer.enabled = false;
            if (_reloadGlow != null)
                _reloadGlow.enabled = false;

            _boss?.OnEngineDestroyed(this);
            return true;
        }

        private void UpdateReloadVisual()
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 9f + _phase);
            if (_renderer != null)
            {
                Color vulnerable = _boss != null && _boss.EngineDamageLocked
                    ? Color.Lerp(new Color(0.12f, 0.22f, 0.55f, 1f), new Color(0.30f, 0.55f, 1f, 1f), pulse)
                    : Color.Lerp(new Color(0.22f, 0.7f, 0.9f, 1f), Color.white, pulse);
                if (_damageFlash > 0f)
                    vulnerable = Color.Lerp(vulnerable, new Color(1f, 0.35f, 0.20f, 1f), 0.75f);
                _renderer.color = vulnerable;
            }
            if (_reloadGlow != null)
            {
                Color c = _reloadGlow.color;
                c.a = Mathf.Lerp(0.16f, 0.52f, pulse);
                _reloadGlow.color = c;
            }
        }

        private Color GetActiveColor()
        {
            switch (_pattern)
            {
                case BossEnginePattern.HeavyRed:
                    return new Color(1f, 0.22f, 0.12f, 1f);
                case BossEnginePattern.ArmoredGreen:
                    return new Color(0.26f, 1f, 0.32f, 1f);
                case BossEnginePattern.ScatterNormal:
                    return new Color(1f, 0.68f, 0.16f, 1f);
                default:
                    return new Color(0.95f, 0.46f, 0.12f, 1f);
            }
        }
    }
}

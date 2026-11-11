using UnityEngine;

namespace MissilePlanet
{
    public sealed class AlienBoss : MonoBehaviour
    {
        [SerializeField] private BossEngine[] _engines;
        [SerializeField] private SpriteRenderer _bodyRenderer;

        private MissilePlanetGame _game;
        private MissilePlanetConfig _config;
        private bool _active;
        private int _aliveEngines;
        private int _bossTier = 1;
        private Vector2 _anchorPosition;
        private float _movePhase;
        private float _coreAttackTimer;
        private bool _telegraphing;
        private Color _normalBodyColor;
        private float _engineGlobalLockoutTimer;

        public bool IsActive => _active;
        public bool IsDefeated => !_active && _aliveEngines <= 0;
        public int AliveEngines => _aliveEngines;
        public BossEngine[] Engines => _engines;
        public bool EngineDamageLocked => _engineGlobalLockoutTimer > 0f;
        public float EngineDamageLockRemaining => Mathf.Max(0f, _engineGlobalLockoutTimer);
        public float FireRateMultiplier => 1f
            + (_bossTier - 1) * _config.BossTierFireRateIncrease
            + (4 - _aliveEngines) * _config.BossFireRateIncreasePerDestroyedEngine;
        public float ReloadRateMultiplier => 1f
            + (_bossTier - 1) * _config.BossTierReloadRateIncrease
            + (4 - _aliveEngines) * _config.BossReloadRateIncreasePerDestroyedEngine;

        private void Awake()
        {
            if (_engines == null || _engines.Length == 0)
                _engines = GetComponentsInChildren<BossEngine>(true);
            if (_bodyRenderer == null)
            {
                Transform body = transform.Find("Body");
                _bodyRenderer = body != null ? body.GetComponent<SpriteRenderer>() : GetComponent<SpriteRenderer>();
            }
        }

        public void Initialize(MissilePlanetGame game, MissilePlanetConfig config, int bossTier = 1)
        {
            _game = game;
            _config = config;
            _active = true;
            _bossTier = Mathf.Max(1, bossTier);
            _aliveEngines = _engines != null ? _engines.Length : 0;
            _anchorPosition = transform.position;
            _movePhase = Random.Range(0f, Mathf.PI * 2f);
            _coreAttackTimer = Mathf.Max(1f, _config.BossCoreBarrageInterval * 0.72f);
            _telegraphing = false;
            _engineGlobalLockoutTimer = 0f;
            _normalBodyColor = new Color(0.34f, 0.16f, 0.52f, 1f);

            if (_bodyRenderer != null)
                _bodyRenderer.color = _normalBodyColor;

            int engineHp = Mathf.Max(1,
                _config.BossEngineBaseHitPoints
                + ((_bossTier - 1) / 2) * _config.BossEngineHitPointsPerTwoTiers);

            for (int i = 0; i < _engines.Length; i++)
            {
                switch (i)
                {
                    case 0:
                        _engines[i].Initialize(this, i, BossEnginePattern.RapidNormal, 6, 0.48f, 3.0f, engineHp, _config.BossEngineHitCooldown);
                        break;
                    case 1:
                        _engines[i].Initialize(this, i, BossEnginePattern.HeavyRed, 3, 0.92f, 3.8f, engineHp, _config.BossEngineHitCooldown);
                        break;
                    case 2:
                        _engines[i].Initialize(this, i, BossEnginePattern.ArmoredGreen, 3, 1.08f, 4.15f, engineHp, _config.BossEngineHitCooldown);
                        break;
                    default:
                        _engines[i].Initialize(this, i, BossEnginePattern.ScatterNormal, 3, 1.18f, 3.45f, engineHp, _config.BossEngineHitCooldown);
                        break;
                }
            }
        }

        private void Update()
        {
            if (!_active || _game == null || _config == null || !_game.IsPlaying)
                return;

            if (_engineGlobalLockoutTimer > 0f)
                _engineGlobalLockoutTimer = Mathf.Max(0f, _engineGlobalLockoutTimer - Time.deltaTime);

            UpdateMovement();
            UpdateCoreAttack();
        }

        private void UpdateMovement()
        {
            float phase = Time.time * _config.BossMoveAngularSpeed + _movePhase;
            float x = _anchorPosition.x + Mathf.Sin(phase) * _config.BossMoveAmplitude;
            transform.position = new Vector3(x, _anchorPosition.y, transform.position.z);
        }

        private void UpdateCoreAttack()
        {
            _coreAttackTimer -= Time.deltaTime;
            float telegraphDuration = Mathf.Max(0.1f, _config.BossCoreTelegraphDuration);
            bool telegraph = _coreAttackTimer <= telegraphDuration;

            if (_bodyRenderer != null)
            {
                if (telegraph)
                {
                    float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 15f);
                    _bodyRenderer.color = Color.Lerp(_normalBodyColor, new Color(1f, 0.18f, 0.72f, 1f), pulse);
                }
                else if (_telegraphing)
                {
                    _bodyRenderer.color = _normalBodyColor;
                }
            }
            _telegraphing = telegraph;

            if (_coreAttackTimer > 0f)
                return;

            FireCoreBarrage(false);
            float aggression = Mathf.Max(1f, FireRateMultiplier * 0.78f);
            _coreAttackTimer = Mathf.Max(1.35f, _config.BossCoreBarrageInterval / aggression);
            _telegraphing = false;
            if (_bodyRenderer != null)
                _bodyRenderer.color = _normalBodyColor;
        }

        private void FireCoreBarrage(bool retaliation)
        {
            if (!_active || _game == null)
                return;

            int destroyed = Mathf.Max(0, 4 - _aliveEngines);
            int count = retaliation
                ? Mathf.Max(1, _config.BossRetaliationProjectiles + destroyed)
                : Mathf.Max(1, _config.BossCoreBaseProjectiles + destroyed);
            count = Mathf.Min(count, 7);

            float speedMultiplier = retaliation
                ? _config.BossRetaliationSpeedMultiplier
                : _config.BossCoreProjectileSpeedMultiplier;

            Vector2 center = transform.position + Vector3.down * 0.45f;
            for (int i = 0; i < count; i++)
            {
                float normalized = count <= 1 ? 0f : (i / (float)(count - 1) - 0.5f);
                Vector2 origin = center + Vector2.right * normalized * 2.5f;
                AsteroidVariant variant = AsteroidVariant.Normal;

                if (_bossTier >= 2 && i == count / 2)
                    variant = AsteroidVariant.RedSplitter;
                if (_bossTier >= 3 && i == count - 1)
                    variant = AsteroidVariant.GreenArmored;

                int hp = variant == AsteroidVariant.GreenArmored ? _game.GetGreenAsteroidHitPoints() : 1;
                _game.SpawnBossAsteroid(origin, variant, hp, speedMultiplier);
            }
        }

        public bool CanEngineTakeDamage(BossEngine engine)
        {
            return _active && engine != null && engine.IsAlive && _engineGlobalLockoutTimer <= 0f;
        }

        public void FireEngine(BossEngine engine, BossEnginePattern pattern)
        {
            if (!_active || _game == null || engine == null || !engine.IsAlive)
                return;

            Vector2 origin = engine.transform.position;
            switch (pattern)
            {
                case BossEnginePattern.HeavyRed:
                    _game.SpawnBossAsteroid(origin, AsteroidVariant.RedSplitter, 1, 1.05f);
                    break;

                case BossEnginePattern.ArmoredGreen:
                    _game.SpawnBossAsteroid(origin, AsteroidVariant.GreenArmored, _game.GetGreenAsteroidHitPoints(), 1.0f);
                    break;

                case BossEnginePattern.ScatterNormal:
                    _game.SpawnBossAsteroid(origin + Vector2.left * 0.16f, AsteroidVariant.Normal, 1, 1.12f);
                    _game.SpawnBossAsteroid(origin + Vector2.right * 0.16f, AsteroidVariant.Normal, 1, 1.12f);
                    break;

                default:
                    _game.SpawnBossAsteroid(origin, AsteroidVariant.Normal, 1, 1.08f);
                    break;
            }
        }

        public void OnEngineDestroyed(BossEngine engine)
        {
            if (!_active)
                return;

            _aliveEngines = Mathf.Max(0, _aliveEngines - 1);
            _engineGlobalLockoutTimer = _aliveEngines > 0 ? Mathf.Max(0f, _config.BossEngineGlobalLockoutDuration) : 0f;
            _game?.HandleBossEngineDestroyed(engine, _aliveEngines);

            if (_aliveEngines <= 0)
            {
                _active = false;
                if (_bodyRenderer != null)
                    _bodyRenderer.color = new Color(0.75f, 0.28f, 0.72f, 0.45f);
                _game?.HandleBossDefeated(this);
                return;
            }

            // Destroying an engine makes the fight escalate immediately rather than granting a free pause.
            FireCoreBarrage(true);
            _coreAttackTimer = Mathf.Min(_coreAttackTimer, Mathf.Max(1.2f, _config.BossCoreBarrageInterval * 0.55f));
        }
    }
}

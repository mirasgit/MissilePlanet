using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MissilePlanet
{
    public sealed partial class MissilePlanetGame : MonoBehaviour
    {
        // Prevent pathological click bursts from spawning an unbounded amount
        // of missiles/explosions in a handful of frames. UI input is not throttled.
        private const float PlayerShotCooldownSeconds = 0.055f;
        private float _nextPlayerShotRealtime;

        [Header("References")]
        [SerializeField] private MissilePlanetConfig _config;
        [SerializeField] private Camera _camera;
        [SerializeField] private Transform _planet;
        [SerializeField] private PlanetController _planetController;
        [SerializeField] private SimplePool _pool;
        [SerializeField] private CameraShake _cameraShake;
        [SerializeField] private SfxPlayer _sfxPlayer;
        [SerializeField] private GameObject _asteroidPrefab;
        [SerializeField] private GameObject _missilePrefab;
        [SerializeField] private GameObject _explosionPrefab;
        [SerializeField] private GameObject _planetPulsePrefab;
        [SerializeField] private GameObject _alienBossPrefab;
        [SerializeField] private GameObject _orbitalHelperPrefab;
        [SerializeField] private SpriteRenderer _planetPulseShield;

        [Header("DEBUG (leave defaults for normal game)")]
        [SerializeField, Min(1)] private int _debugStartWave = 1;
        [SerializeField] private bool _debugGodMode;
        [SerializeField] private bool _debugInfiniteMissiles;
        [SerializeField, Min(0), InspectorName("Debug Starting Coins")] private int _debugStartingShopPoints;

        private readonly List<Asteroid> _activeAsteroids = new List<Asteroid>();
        private PlanetTarget[] _targets;
        private Coroutine _gameLoop;
        private bool _playing;
        private bool _hasStarted;
        private bool _paused;
        private bool _shopOpen;
        private bool _bossDefeatInProgress;
        private int _planetPulseCharges;
        private int _shopPoints;
        private int _missileUpgradeLevel;
        private int _missilePowerLevel;
        private int _guardianCount;
        private int _cannonCount;
        private int _guardianPurchaseCount;
        private int _cannonPurchaseCount;
        private int _cannonsDestroyed;
        private readonly List<OrbitalHelper> _guardianHelpers = new List<OrbitalHelper>();
        private readonly List<OrbitalHelper> _cannonHelpers = new List<OrbitalHelper>();
        private MissilePlanetHUD _hud;
        private AlienBoss _activeBoss;
        private AnalyticsBridge _analytics;
        private MusicPlayer _musicPlayer;
        private HapticsBridge _haptics;
        private int _shotsFired;
        private WaitForSeconds _waitSpawnInterval;
        private WaitForSeconds _waitInterWaveDelay;
        private WaitForSeconds _waitBossDefeatBeat;

        public IReadOnlyList<Asteroid> ActiveAsteroids => _activeAsteroids;
        public bool IsPlaying => _playing && !_paused && !_shopOpen;
        public bool IsStarted => _hasStarted;
        public bool IsPaused => _paused;
        public bool IsShopOpen => _shopOpen;
        public bool IsGameOver => _hasStarted && !_playing;
        public int Score { get; private set; }
        public int BestScore { get; private set; }
        public int Wave { get; private set; }
        public int Ammo { get; private set; }
        public int PlanetPulseCharges => _planetPulseCharges;
        public int ShopPoints => _shopPoints; // compatibility alias; v10 treats this as run Coins
        public int Coins => _shopPoints;
        public int MissileUpgradeLevel => _missileUpgradeLevel;
        public int MissileUpgradeBonus => _config != null ? _missileUpgradeLevel * _config.ShopMissilesPerUpgrade : 0;
        public int MissilePowerLevel => _missilePowerLevel;
        public int GuardianCount => _guardianCount;
        public int CannonCount => _cannonCount;
        public int GuardianMaxCount => _config != null ? _config.ShopGuardianMaxCount : 0;
        public int CannonMaxCount => _config != null ? _config.ShopCannonMaxCount : 0;
        public int GuardianCurrentSlotCap => GetGuardianSlotCap();
        public int CannonCurrentSlotCap => GetCannonSlotCap();
        public bool GuardianPurchased => _guardianCount > 0;
        public bool CannonPurchased => _cannonCount > 0;
        public bool CannonDestroyed => _cannonsDestroyed > 0;
        public int MissileUpgradeCost => _config == null ? 0 : _config.ShopMissileUpgradeBaseCost + _missileUpgradeLevel * _config.ShopMissileUpgradeCostStep;
        public int MissilePowerUpgradeCost => GetMissilePowerUpgradeCost();
        public int ShieldChargeCost => _config != null ? _config.ShopShieldChargeCost : 0;
        public int GuardianCost => _config == null ? 0 : _config.ShopGuardianCost + _guardianPurchaseCount * _config.ShopGuardianCostStep;
        public int CannonCost => _config == null ? 0 : _config.ShopCannonCost + _cannonPurchaseCount * _config.ShopCannonCostStep;
        public bool CanUpgradeMissiles => _config != null && _missileUpgradeLevel < _config.ShopMissileUpgradeMaxLevel;
        public bool CanUpgradeMissilePower => _config != null && _missilePowerLevel < _config.ShopPowerUpgradeMaxLevel;
        public bool CanBuyGuardian => _config != null && _guardianCount < GetGuardianSlotCap() && _guardianCount < _config.ShopGuardianMaxCount;
        public bool CanBuyCannon => _config != null && _cannonCount < GetCannonSlotCap() && _cannonCount < _config.ShopCannonMaxCount;
        public int MissileUpgradeMaxLevel => _config != null ? _config.ShopMissileUpgradeMaxLevel : 0;
        public int MissilePowerMaxLevel => _config != null ? _config.ShopPowerUpgradeMaxLevel : 0;
        public int MaxPulseCharges => _config != null ? _config.MaxPulseCharges : 0;
        public bool PlanetPulseAvailable => _planetPulseCharges > 0;
        public Vector2 PlanetPosition => _planet != null ? _planet.position : Vector2.zero;
        public Camera GameCamera => _camera;
        public float PlanetRadius => _config != null ? _config.PlanetRadius : 0f;
        public bool IsBossWave => _activeBoss != null && _activeBoss.IsActive;
        public int BossEnginesAlive => _activeBoss != null ? _activeBoss.AliveEngines : 0;
        public bool DebugGodMode => _debugGodMode;
        public bool DebugInfiniteMissiles => _debugInfiniteMissiles;
        public int DebugStartWave => Mathf.Max(1, _debugStartWave);
        public int DebugStartingShopPoints => Mathf.Max(0, _debugStartingShopPoints);
        public int ShotsFired => _shotsFired;

        public void Configure(
            MissilePlanetConfig config,
            Camera gameCamera,
            Transform planet,
            PlanetController planetController,
            SimplePool pool,
            CameraShake cameraShake,
            SfxPlayer sfxPlayer,
            GameObject asteroidPrefab,
            GameObject missilePrefab,
            GameObject explosionPrefab,
            GameObject planetPulsePrefab,
            GameObject alienBossPrefab,
            GameObject orbitalHelperPrefab,
            SpriteRenderer planetPulseShield)
        {
            _config = config;
            _camera = gameCamera;
            _planet = planet;
            _planetController = planetController;
            _pool = pool;
            _cameraShake = cameraShake;
            _sfxPlayer = sfxPlayer;
            _asteroidPrefab = asteroidPrefab;
            _missilePrefab = missilePrefab;
            _explosionPrefab = explosionPrefab;
            _planetPulsePrefab = planetPulsePrefab;
            _alienBossPrefab = alienBossPrefab;
            _orbitalHelperPrefab = orbitalHelperPrefab;
            _planetPulseShield = planetPulseShield;
        }

        private void Awake()
        {
            GameSave.Initialize();
            GameSettings.ApplyRuntime();
            Screen.orientation = ScreenOrientation.Portrait;

            if (_camera == null)
                _camera = Camera.main;

            _hud = GetComponent<MissilePlanetHUD>();

            if (_planet != null)
                _targets = _planet.GetComponentsInChildren<PlanetTarget>(true);
            else
                _targets = System.Array.Empty<PlanetTarget>();
        }

        private void Start()
        {
            if (_config == null || _camera == null || _planet == null || _pool == null)
            {
                Debug.LogError("Missile Planet is missing required references. Rebuild the demo scene from Tools > Missile Planet > Build Demo Scene.");
                enabled = false;
                return;
            }

            if (_planetController != null)
                _planetController.RotationSpeed = _config.PlanetRotationSpeed;

            _waitSpawnInterval = new WaitForSeconds(_config.SpawnInterval);
            _waitInterWaveDelay = new WaitForSeconds(_config.InterWaveDelay);
            _waitBossDefeatBeat = new WaitForSeconds(0.8f);

            // Prewarm the hot-path pooled objects while the main menu is visible.
            // This moves Instantiate spikes away from the first combat waves.
            _pool.Prewarm(_asteroidPrefab, 48);
            _pool.Prewarm(_missilePrefab, 28);
            _pool.Prewarm(_explosionPrefab, 24);
            _pool.Prewarm(_planetPulsePrefab, 2);

            foreach (PlanetTarget target in _targets)
                target.ResetTarget();

            BestScore = GameSave.Data.bestScore;
            Score = 0;
            Wave = 0;
            Ammo = 0;
            _planetPulseCharges = Mathf.Clamp(_config.StartingPulseCharges, 0, _config.MaxPulseCharges);
            _hasStarted = false;
            _playing = false;
            _paused = false;
            _shopOpen = false;
            _bossDefeatInProgress = false;
            _shopPoints = 0;
            _missileUpgradeLevel = 0;
            _missilePowerLevel = 0;
            _guardianCount = 0;
            _cannonCount = 0;
            _guardianPurchaseCount = 0;
            _cannonPurchaseCount = 0;
            _cannonsDestroyed = 0;
            _shotsFired = 0;
            _guardianHelpers.Clear();
            _cannonHelpers.Clear();
            Time.timeScale = 1f;
            AudioListener.pause = false;
            InitializeMetaSystems();
            ApplyCosmeticsVisuals();
            RefreshPulseShield();
        }


        public void StartGame()
        {
            if (_hasStarted || _config == null)
                return;

            foreach (PlanetTarget target in _targets)
                target.ResetTarget();

            Score = 0;
            // GameLoop increments Wave before starting each round, so preload one less
            // than the requested debug wave. Default value 1 preserves normal behavior.
            Wave = Mathf.Max(1, _debugStartWave) - 1;
            Ammo = 0;
            _planetPulseCharges = Mathf.Clamp(_config.StartingPulseCharges, 0, _config.MaxPulseCharges);
            _shopPoints = Mathf.Max(0, _debugStartingShopPoints);
            _missileUpgradeLevel = 0;
            _missilePowerLevel = 0;
            _guardianCount = 0;
            _cannonCount = 0;
            _guardianPurchaseCount = 0;
            _cannonPurchaseCount = 0;
            _cannonsDestroyed = 0;
            _shotsFired = 0;
            _guardianHelpers.Clear();
            _cannonHelpers.Clear();
            ResetRunMetaState();
            _shopOpen = false;
            _bossDefeatInProgress = false;
            _hasStarted = true;
            _playing = true;
            _paused = false;
            _nextPlayerShotRealtime = 0f;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            RefreshPulseShield();
            _musicPlayer?.SetMode(MusicMode.Gameplay);
            GameSave.Data.gamesStarted++;
            GameSave.MarkDirty();
            _analytics?.Track("game_start",
                new AnalyticsParam("start_wave", Mathf.Max(1, _debugStartWave)),
                new AnalyticsParam("cores", Cores));
            _gameLoop = StartCoroutine(GameLoop());
        }

        /// <summary>
        /// Gameplay pointer input is routed here by MissilePlanetHUD after UI hit-testing.
        /// Keeping a single input reader avoids New Input System update-order races.
        /// </summary>
        public void HandleGameplayPointer(Vector2 screenPosition)
        {
            if (!_playing || _paused || _shopOpen || _camera == null || _config == null)
                return;

            Vector3 world3 = _camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -_camera.transform.position.z));
            Vector2 world = new Vector2(world3.x, world3.y);

            float planetTapRadius = _config.PlanetRadius * 1.02f;
            if ((world - PlanetPosition).sqrMagnitude <= planetTapRadius * planetTapRadius)
            {
                TryActivatePlanetPulse();
                return;
            }

            // UI taps are handled before this method. Throttle only actual combat
            // shots; rapid tapping should never be able to overload the frame and
            // make input appear frozen.
            float now = Time.realtimeSinceStartup;
            if (now < _nextPlayerShotRealtime)
                return;
            _nextPlayerShotRealtime = now + PlayerShotCooldownSeconds;

            if (!_debugInfiniteMissiles && Ammo <= 0)
                return;

            PlanetTarget launcher = GetClosestAliveLauncher(world);
            if (launcher == null)
            {
                EndGame();
                return;
            }

            Missile missile = _pool.Spawn<Missile>(_missilePrefab, launcher.transform.position, Quaternion.identity);
            if (missile == null)
                return;

            if (!_debugInfiniteMissiles)
                Ammo--;

            _shotsFired++;
            _sfxPlayer?.PlayShot(launcher.transform.position);
            missile.Initialize(this, launcher.transform.position, world, _config.MissileSpeed);
        }

        public bool TryActivatePlanetPulse()
        {
            if (!_playing || _planetPulseCharges <= 0 || _planetPulsePrefab == null || !HasVisibleAsteroid())
                return false;

            PlanetPulseWave pulse = _pool.Spawn<PlanetPulseWave>(_planetPulsePrefab, PlanetPosition, Quaternion.identity);
            if (pulse == null)
                return false;

            _planetPulseCharges--;
            RefreshPulseShield();
            float maxRadius = GetPlanetPulseMaxRadius();
            pulse.Initialize(
                this,
                PlanetPosition,
                _config.PlanetRadius * 0.94f,
                maxRadius,
                _config.PlanetPulseDuration,
                new Color(0.25f, 0.92f, 1f, 0.86f),
                _activeAsteroids,
                _camera);

            _sfxPlayer?.PlayPlanetPulse(PlanetPosition);
            _cameraShake?.Shake(_config.PlanetPulseShakeDuration, _config.PlanetPulseShakeMagnitude);
            _analytics?.Track("pulse_used", new AnalyticsParam("wave", Wave), new AnalyticsParam("charges_left", _planetPulseCharges));
            return true;
        }

        private void SetPulseShieldVisible(bool visible)
        {
            if (_planetPulseShield != null)
                _planetPulseShield.enabled = visible;
        }

        private void RefreshPulseShield()
        {
            if (_planetPulseShield == null)
                return;

            _planetPulseShield.enabled = _planetPulseCharges > 0;
            if (_planetPulseCharges <= 0)
                return;

            Color color = _planetPulseShield.color;
            float normalized = _config.MaxPulseCharges <= 1
                ? 1f
                : Mathf.InverseLerp(1f, _config.MaxPulseCharges, _planetPulseCharges);
            color.a = Mathf.Lerp(0.52f, 0.88f, normalized);
            _planetPulseShield.color = color;
        }

        private void AwardPulseCharge()
        {
            int previous = _planetPulseCharges;
            _planetPulseCharges = Mathf.Min(_config.MaxPulseCharges, _planetPulseCharges + 1);
            if (_planetPulseCharges != previous)
                RefreshPulseShield();
        }

        private bool HasVisibleAsteroid()
        {
            for (int i = 0; i < _activeAsteroids.Count; i++)
            {
                Asteroid asteroid = _activeAsteroids[i];
                if (asteroid == null || !asteroid.gameObject.activeInHierarchy)
                    continue;

                Vector3 viewport = _camera.WorldToViewportPoint(asteroid.transform.position);
                if (viewport.z >= 0f && viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f)
                    return true;
            }

            return false;
        }

        private float GetPlanetPulseMaxRadius()
        {
            float halfHeight = _camera.orthographicSize;
            float halfWidth = halfHeight * _camera.aspect;
            Vector2 cameraCenter = _camera.transform.position;

            Vector2[] corners =
            {
                cameraCenter + new Vector2(-halfWidth, -halfHeight),
                cameraCenter + new Vector2(-halfWidth, halfHeight),
                cameraCenter + new Vector2(halfWidth, -halfHeight),
                cameraCenter + new Vector2(halfWidth, halfHeight)
            };

            float maxDistance = 0f;
            for (int i = 0; i < corners.Length; i++)
                maxDistance = Mathf.Max(maxDistance, Vector2.Distance(PlanetPosition, corners[i]));

            return maxDistance + _config.PlanetPulseScreenPadding;
        }

        private IEnumerator GameLoop()
        {
            while (_playing)
            {
                Wave++;
                Ammo = GetWaveAmmo();
                GameSave.SetHighestWave(Wave);
                _analytics?.Track("wave_start", new AnalyticsParam("wave", Wave), new AnalyticsParam("ammo", Ammo));

                if (IsBossWaveNumber(Wave))
                {
                    yield return RunBossWave();
                    if (!_playing)
                        yield break;

                    _analytics?.Track("wave_complete", new AnalyticsParam("wave", Wave), new AnalyticsParam("boss", true), new AnalyticsParam("coins", Coins));
                    GameSave.FlushIfDirty();
                    AwardEndOfWaveCityCoins();
                    OpenShop();
                    while (_playing && _shopOpen)
                        yield return null;

                    if (!_playing)
                        yield break;

                    yield return _waitInterWaveDelay;
                    continue;
                }

                int asteroidCount = _config.StartingAsteroids + (Wave - 1) * _config.AsteroidsPerWave;

                bool spawnCarrierThisWave = _config.PulseCarrierWaveInterval > 0
                    && Wave % _config.PulseCarrierWaveInterval == 0;
                int carrierSpawnIndex = spawnCarrierThisWave
                    ? Random.Range(0, asteroidCount)
                    : -1;

                // Wave 21 is the showcase/tutorial wave for the Yellow Dodger.
                // Force exactly one slot to be yellow so the player is guaranteed to meet it,
                // while every other spawn keeps using the normal rarity table.
                int guaranteedYellowSpawnIndex = Wave == _config.YellowGuaranteedWave && asteroidCount > 0
                    ? Random.Range(0, asteroidCount)
                    : -1;

                for (int i = 0; i < asteroidCount && _playing; i++)
                {
                    if (i == guaranteedYellowSpawnIndex)
                        SpawnAsteroid(AsteroidVariant.YellowDodger);
                    else
                        SpawnAsteroid();

                    if (i == carrierSpawnIndex)
                        SpawnPulseCarrier();

                    yield return _waitSpawnInterval;
                }

                while (_playing && _activeAsteroids.Count > 0)
                    yield return null;

                if (!_playing)
                    yield break;

                _analytics?.Track("wave_complete", new AnalyticsParam("wave", Wave), new AnalyticsParam("boss", false), new AnalyticsParam("coins", Coins));
                GameSave.FlushIfDirty();
                AwardEndOfWaveCityCoins();
                yield return _waitInterWaveDelay;
            }
        }

        private bool IsBossWaveNumber(int wave)
        {
            return _config != null
                && _config.BossWaveInterval > 0
                && wave > 0
                && wave % _config.BossWaveInterval == 0;
        }

        private int GetWaveAmmo()
        {
            return _config.MissilesPerWave
                + (Wave - 1) * _config.MissileBonusPerWave
                + MissileUpgradeBonus;
        }

        private IEnumerator RunBossWave()
        {
            if (_alienBossPrefab == null)
            {
                Debug.LogError("Boss prefab is missing. Rebuild the demo scene.");
                yield break;
            }

            Vector2 bossPosition = new Vector2(0f, _config.BossSpawnY);
            GameObject bossGo = Instantiate(_alienBossPrefab, bossPosition, Quaternion.identity);
            _activeBoss = bossGo.GetComponent<AlienBoss>();
            if (_activeBoss == null)
            {
                Destroy(bossGo);
                yield break;
            }

            int bossTier = Mathf.Max(1, Wave / Mathf.Max(1, _config.BossWaveInterval));
            _activeBoss.Initialize(this, _config, bossTier);
            _musicPlayer?.SetMode(MusicMode.Boss);
            _analytics?.Track("boss_start", new AnalyticsParam("wave", Wave), new AnalyticsParam("tier", bossTier));

            bool shouldSpawnCarrier = _config.PulseCarrierWaveInterval > 0
                && Wave % _config.PulseCarrierWaveInterval == 0;
            float carrierDelay = Random.Range(4.5f, 8.0f);
            float elapsed = 0f;
            bool carrierSpawned = false;

            while (_playing && _activeBoss != null && _activeBoss.IsActive)
            {
                elapsed += Time.deltaTime;
                if (shouldSpawnCarrier && !carrierSpawned && elapsed >= carrierDelay)
                {
                    carrierSpawned = true;
                    SpawnPulseCarrier();
                }

                yield return null;
            }

            if (!_playing)
                yield break;

            while (_playing && (_bossDefeatInProgress || _activeBoss != null))
                yield return null;

            while (_playing && _activeAsteroids.Count > 0)
                yield return null;
        }

        private void SpawnAsteroid(AsteroidVariant? forcedVariant = null)
        {
            AsteroidVariant variant = forcedVariant ?? RollWaveAsteroidVariant();
            int hitPoints = variant == AsteroidVariant.GreenArmored ? GetGreenAsteroidHitPoints() : 1;
            SpawnTargetedAsteroid(GetAsteroidSpawnPoint(), variant, hitPoints, false);
        }

        private AsteroidVariant RollWaveAsteroidVariant()
        {
            float roll = Random.value;

            if (Wave > _config.RedAsteroidUnlockAfterWave && roll < _config.RedAsteroidChance)
                return AsteroidVariant.RedSplitter;

            if (Wave > _config.GreenAsteroidUnlockAfterWave
                && roll < _config.RedAsteroidChance + _config.GreenAsteroidChance)
            {
                // Yellow Dodgers do not add another flat spawn chance. They replace 30% of
                // already-rare green rolls after Wave 20: roughly 3% of the normal stream.
                if (Wave >= _config.YellowAsteroidUnlockAfterWave
                    && Random.value < _config.YellowReplaceGreenChance)
                    return AsteroidVariant.YellowDodger;

                return AsteroidVariant.GreenArmored;
            }

            return AsteroidVariant.Normal;
        }

        public int GetGreenAsteroidHitPoints()
        {
            return Wave > _config.GreenUpgradeAfterWave
                ? _config.GreenHitsAfterUpgrade
                : _config.GreenHitsBeforeUpgrade;
        }

        private int GetRedSplitCount()
        {
            return Wave > _config.RedUpgradeAfterWave
                ? _config.RedSplitCountAfterUpgrade
                : _config.RedSplitCountBeforeUpgrade;
        }

        private void SpawnTargetedAsteroid(
            Vector2 spawn,
            AsteroidVariant variant,
            int hitPoints,
            bool bossSpawned,
            float speedMultiplier = 1f)
        {
            PlanetTarget target = GetRandomAsteroidTarget();
            if (target == null)
            {
                EndGame();
                return;
            }

            Asteroid asteroid = _pool.Spawn<Asteroid>(_asteroidPrefab, spawn, Quaternion.identity);
            if (asteroid == null)
                return;

            float speed = (_config.AsteroidBaseSpeed + (Wave - 1) * _config.AsteroidSpeedPerWave) * speedMultiplier;
            float scaleMultiplier = 1f;
            float collisionRadius = _config.AsteroidCollisionRadius;

            if (variant == AsteroidVariant.RedSplitter)
            {
                scaleMultiplier = _config.RedAsteroidScaleMultiplier;
            }
            else if (variant == AsteroidVariant.GreenArmored)
            {
                speed *= _config.GreenAsteroidSpeedMultiplier;
            }

            if (variant == AsteroidVariant.YellowDodger)
            {
                asteroid.InitializeYellowDodger(
                    this,
                    target,
                    Mathf.Max(_config.YellowAsteroidDashSpeed, _config.MissileSpeed * 1.08f),
                    collisionRadius,
                    _config.YellowMinManeuvers,
                    _config.YellowMaxManeuvers,
                    _config.YellowWaypointReachDistance);
                _activeAsteroids.Add(asteroid);
                return;
            }

            // Boss-fired objects start slightly slower to keep the boss wave readable.
            if (bossSpawned)
                speed *= 0.92f;

            asteroid.Initialize(
                this,
                target,
                speed,
                _config.AsteroidTurnSpeedDegrees,
                _config.AsteroidImpactDistance,
                collisionRadius,
                variant,
                hitPoints,
                scaleMultiplier,
                _config.RedSplitTravelFraction,
                _config.RedBlinkSlowInterval,
                _config.RedBlinkFastInterval);

            _activeAsteroids.Add(asteroid);
        }

        public void SpawnBossAsteroid(Vector2 spawn, AsteroidVariant variant, int hitPoints, float speedMultiplier = 1f)
        {
            if (!_playing || _activeBoss == null || !_activeBoss.IsActive)
                return;

            if (variant == AsteroidVariant.GreenArmored
                && Wave >= _config.YellowAsteroidUnlockAfterWave
                && Random.value < _config.YellowReplaceGreenChance)
            {
                variant = AsteroidVariant.YellowDodger;
                hitPoints = 1;
            }

            SpawnTargetedAsteroid(spawn, variant, hitPoints, true, Mathf.Max(0.1f, speedMultiplier));
        }

        private void SpawnPulseCarrier()
        {
            float halfHeight = _camera.orthographicSize;
            float halfWidth = halfHeight * _camera.aspect;
            Vector2 cameraCenter = _camera.transform.position;
            float offscreenMargin = Mathf.Max(0.8f, _config.AsteroidSpawnTopMargin);

            Vector2 spawn;
            Vector2 exit;

            // Three equally likely fly-by routes:
            // 0 = top -> bottom, 1 = left -> right, 2 = right -> left.
            // All lanes are chosen so the Carrier passes the planet rather than targeting it.
            int route = Random.Range(0, 3);

            if (route == 0)
            {
                float sign = Random.value < 0.5f ? -1f : 1f;
                float minimumLane = Mathf.Max(_config.PlanetRadius + 1.15f, halfWidth * 0.62f);
                float maximumLane = Mathf.Max(minimumLane + 0.1f, halfWidth * 0.88f);
                float spawnX = cameraCenter.x + sign * Random.Range(minimumLane, maximumLane);
                float exitX = cameraCenter.x + sign * Random.Range(minimumLane, maximumLane);

                spawn = new Vector2(spawnX, cameraCenter.y + halfHeight + offscreenMargin);
                exit = new Vector2(exitX, cameraCenter.y - halfHeight - offscreenMargin);
            }
            else
            {
                float minimumY = Mathf.Max(
                    PlanetPosition.y + _config.PlanetRadius + 1.0f,
                    cameraCenter.y - halfHeight * 0.18f);
                float maximumY = cameraCenter.y + halfHeight * 0.72f;

                if (minimumY > maximumY)
                    minimumY = maximumY = cameraCenter.y + halfHeight * 0.35f;

                float spawnY = Random.Range(minimumY, maximumY);
                float exitY = Mathf.Clamp(
                    spawnY + Random.Range(-halfHeight * 0.12f, halfHeight * 0.12f),
                    minimumY,
                    maximumY);

                float leftX = cameraCenter.x - halfWidth - offscreenMargin;
                float rightX = cameraCenter.x + halfWidth + offscreenMargin;

                if (route == 1)
                {
                    spawn = new Vector2(leftX, spawnY);
                    exit = new Vector2(rightX, exitY);
                }
                else
                {
                    spawn = new Vector2(rightX, spawnY);
                    exit = new Vector2(leftX, exitY);
                }
            }

            Vector2 direction = (exit - spawn).normalized;
            float baseSpeed = _config.AsteroidBaseSpeed + (Wave - 1) * _config.AsteroidSpeedPerWave;
            float speed = baseSpeed * _config.PulseCarrierSpeedMultiplier;

            Asteroid asteroid = _pool.Spawn<Asteroid>(_asteroidPrefab, spawn, Quaternion.identity);
            if (asteroid == null)
                return;

            asteroid.InitializePulseCarrier(this, direction * speed, _config.AsteroidCollisionRadius * 1.08f);
            _activeAsteroids.Add(asteroid);
        }

        private Vector2 GetAsteroidSpawnPoint()
        {
            // Asteroids now enter only from above. They never spawn at the side or below
            // the planet, and therefore never originate inside the planet's square region.
            float halfHeight = _camera.orthographicSize;
            float halfWidth = halfHeight * _camera.aspect;
            Vector2 cameraCenter = _camera.transform.position;

            float horizontalPadding = halfWidth * Mathf.Clamp01(_config.AsteroidSpawnHorizontalPadding);
            float minX = cameraCenter.x - halfWidth + horizontalPadding;
            float maxX = cameraCenter.x + halfWidth - horizontalPadding;
            float spawnY = cameraCenter.y + halfHeight + _config.AsteroidSpawnTopMargin;

            return new Vector2(Random.Range(minX, maxX), spawnY);
        }

        public PlanetTarget GetRandomAsteroidTarget()
        {
            if (_targets == null || _targets.Length == 0)
                return null;

            // Incoming asteroids come from the top, so only attack targets on the exposed
            // upper hemisphere. As the planet rotates, every target eventually becomes exposed.
            float minimumTargetY = PlanetPosition.y + _config.PlanetRadius * 0.1f;
            int candidateCount = 0;

            for (int i = 0; i < _targets.Length; i++)
            {
                PlanetTarget target = _targets[i];
                if (target != null && target.IsAlive && target.transform.position.y >= minimumTargetY)
                    candidateCount++;
            }

            if (candidateCount > 0)
            {
                int pick = Random.Range(0, candidateCount);
                for (int i = 0; i < _targets.Length; i++)
                {
                    PlanetTarget target = _targets[i];
                    if (target == null || !target.IsAlive || target.transform.position.y < minimumTargetY)
                        continue;

                    if (pick-- == 0)
                        return target;
                }
            }

            // Defensive fallback: choose the highest alive target rather than aiming through
            // the planet at a target on the lower hemisphere.
            PlanetTarget highest = null;
            float highestY = float.NegativeInfinity;
            for (int i = 0; i < _targets.Length; i++)
            {
                PlanetTarget target = _targets[i];
                if (target == null || !target.IsAlive)
                    continue;

                if (target.transform.position.y > highestY)
                {
                    highestY = target.transform.position.y;
                    highest = target;
                }
            }

            return highest;
        }

        public bool TryGetPlanetSurfaceIntersection(
            Vector2 start,
            Vector2 end,
            float movingRadius,
            out Vector2 intersection)
        {
            Vector2 center = PlanetPosition;
            float radius = _config.PlanetRadius + Mathf.Max(0f, movingRadius);
            Vector2 startFromCenter = start - center;
            Vector2 delta = end - start;

            // If an asteroid is somehow already inside the collision circle, resolve it
            // immediately at the nearest point on the surface.
            if (startFromCenter.sqrMagnitude <= radius * radius)
            {
                Vector2 normal = startFromCenter.sqrMagnitude > 0.000001f
                    ? startFromCenter.normalized
                    : Vector2.up;
                intersection = center + normal * radius;
                return true;
            }

            float a = Vector2.Dot(delta, delta);
            if (a <= 0.0000001f)
            {
                intersection = default;
                return false;
            }

            float b = 2f * Vector2.Dot(startFromCenter, delta);
            float c = Vector2.Dot(startFromCenter, startFromCenter) - radius * radius;
            float discriminant = b * b - 4f * a * c;

            if (discriminant < 0f)
            {
                intersection = default;
                return false;
            }

            float sqrt = Mathf.Sqrt(discriminant);
            float denominator = 2f * a;
            float t1 = (-b - sqrt) / denominator;
            float t2 = (-b + sqrt) / denominator;

            float t = float.PositiveInfinity;
            if (t1 >= 0f && t1 <= 1f)
                t = t1;
            if (t2 >= 0f && t2 <= 1f)
                t = Mathf.Min(t, t2);

            if (float.IsInfinity(t))
            {
                intersection = default;
                return false;
            }

            intersection = start + delta * t;
            return true;
        }

        public PlanetTarget GetRandomAliveTarget()
        {
            if (_targets == null || _targets.Length == 0)
                return null;

            int aliveCount = 0;
            for (int i = 0; i < _targets.Length; i++)
            {
                if (_targets[i] != null && _targets[i].IsAlive)
                    aliveCount++;
            }

            if (aliveCount == 0)
                return null;

            int pick = Random.Range(0, aliveCount);
            for (int i = 0; i < _targets.Length; i++)
            {
                PlanetTarget target = _targets[i];
                if (target == null || !target.IsAlive)
                    continue;

                if (pick-- == 0)
                    return target;
            }

            return null;
        }

        private PlanetTarget GetClosestAliveLauncher(Vector2 worldPoint)
        {
            PlanetTarget best = null;
            float bestDistance = float.PositiveInfinity;

            foreach (PlanetTarget target in _targets)
            {
                if (target == null || !target.IsAlive || target.TargetType != PlanetTargetType.Launcher)
                    continue;

                float sqr = ((Vector2)target.transform.position - worldPoint).sqrMagnitude;
                if (sqr < bestDistance)
                {
                    bestDistance = sqr;
                    best = target;
                }
            }

            return best;
        }

        public void HandleAsteroidDestroyed(Asteroid asteroid, bool createChainExplosion, bool playSfx = true, bool grantCarrierReward = true)
        {
            if (asteroid == null)
                return;

            Vector2 position = asteroid.transform.position;
            AsteroidVariant variant = asteroid.Variant;

            if (playSfx)
                _sfxPlayer?.PlayAsteroidDestroyed(position);

            _activeAsteroids.Remove(asteroid);

            int score = _config.ScorePerAsteroid;
            if (variant == AsteroidVariant.PulseCarrier)
                score = _config.PulseCarrierScore;
            else if (variant == AsteroidVariant.RedSplitter)
                score = _config.RedAsteroidScore;
            else if (variant == AsteroidVariant.GreenArmored)
                score = _config.GreenAsteroidScore;
            else if (variant == AsteroidVariant.YellowDodger)
                score = _config.YellowAsteroidScore;

            AddScore(score);
            AddCoins(GetCoinRewardForAsteroid(variant));
            GameSave.Data.lifetimeAsteroidsDestroyed++;
            GameSave.MarkDirty();
            if (variant != AsteroidVariant.Normal)
                _analytics?.Track("special_asteroid_destroyed", new AnalyticsParam("variant", variant), new AnalyticsParam("wave", Wave));

            if (variant == AsteroidVariant.PulseCarrier && grantCarrierReward)
                AwardPulseCharge();

            _pool.Despawn(asteroid.gameObject);

            if (createChainExplosion && _playing)
            {
                CreateExplosion(
                    position,
                    _config.ChainExplosionRadius,
                    _config.ChainExplosionDuration,
                    GetPlayerExplosionColor(0.46f),
                    true);
            }
        }

        public void HandleRedAsteroidTimedSplit(Asteroid asteroid)
        {
            if (asteroid == null || !_playing)
                return;

            Vector2 position = asteroid.transform.position;
            _activeAsteroids.Remove(asteroid);
            _sfxPlayer?.PlayAsteroidDestroyed(position);
            _pool.Despawn(asteroid.gameObject);

            // Natural split is a threat escalation, not a player kill: no score is awarded.
            // The small visual burst is non-damaging so it cannot erase its own fragments.
            CreateExplosion(
                position,
                0.42f,
                0.24f,
                new Color(1f, 0.18f, 0.06f, 0.52f),
                false);

            StartCoroutine(SpawnRedFragmentsNextFrame(position, GetRedSplitCount()));
        }

        private IEnumerator SpawnRedFragmentsNextFrame(Vector2 origin, int count)
        {
            // Delay one frame so the explosion that destroyed the red asteroid cannot
            // instantly erase the fragments in the same damage pass.
            yield return null;

            if (!_playing || (_activeBoss != null && !_activeBoss.IsActive))
                yield break;

            int fragmentCount = Mathf.Max(1, count);
            float spreadRadius = 0.28f;

            for (int i = 0; i < fragmentCount; i++)
            {
                float angle = (360f / fragmentCount) * i + Random.Range(-18f, 18f);
                Vector2 offset = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * spreadRadius;
                SpawnTargetedAsteroid(
                    origin + offset,
                    AsteroidVariant.Normal,
                    1,
                    false,
                    _config.RedFragmentSpeedMultiplier);
            }
        }

        public void PlayArmoredAsteroidHit(Vector2 position)
        {
            _sfxPlayer?.PlayAsteroidDestroyed(position);
        }

        public void HandleAsteroidImpact(Asteroid asteroid, PlanetTarget target, Vector2 impactPosition)
        {
            if (asteroid == null)
                return;

            _activeAsteroids.Remove(asteroid);
            _sfxPlayer?.PlayPlanetImpact(impactPosition);
            _pool.Despawn(asteroid.gameObject);

            // God Mode keeps the real impact/collision/effects, but protects all
            // surface targets so the player can safely test late waves and the boss.
            if (!_debugGodMode && target != null && target.IsAlive)
                target.TakeDamage(1);

            CreateExplosion(
                impactPosition,
                _config.ImpactExplosionRadius,
                _config.ImpactExplosionDuration,
                new Color(1f, 0.25f, 0.08f, 0.42f),
                false);

            _cameraShake?.Shake(_config.ImpactShakeDuration, _config.ImpactShakeMagnitude);

            if (!_debugGodMode
                && (CountAlive(PlanetTargetType.City) == 0 || CountAlive(PlanetTargetType.Launcher) == 0))
                EndGame();
        }

        public void HandleAsteroidSurfaceImpact(
            Asteroid asteroid,
            PlanetTarget intendedTarget,
            Vector2 impactPosition)
        {
            PlanetTarget hitTarget = null;
            float hitRadiusSqr = _config.AsteroidTargetHitRadius * _config.AsteroidTargetHitRadius;

            if (intendedTarget != null && intendedTarget.IsAlive)
            {
                float intendedDistanceSqr = ((Vector2)intendedTarget.transform.position - impactPosition).sqrMagnitude;
                if (intendedDistanceSqr <= hitRadiusSqr)
                    hitTarget = intendedTarget;
            }

            if (hitTarget == null)
            {
                float bestDistanceSqr = hitRadiusSqr;
                for (int i = 0; i < _targets.Length; i++)
                {
                    PlanetTarget candidate = _targets[i];
                    if (candidate == null || !candidate.IsAlive)
                        continue;

                    float distanceSqr = ((Vector2)candidate.transform.position - impactPosition).sqrMagnitude;
                    if (distanceSqr <= bestDistanceSqr)
                    {
                        bestDistanceSqr = distanceSqr;
                        hitTarget = candidate;
                    }
                }
            }

            HandleAsteroidImpact(asteroid, hitTarget, impactPosition);
        }

        private void AddScore(int points)
        {
            int awarded = Mathf.Max(0, points);
            Score += awarded;
            if (Score <= BestScore)
                return;

            BestScore = Score;
            GameSave.SetBestScore(BestScore);
        }

        public bool TryDamageBossEngines(Vector2 explosionCenter, float explosionRadius, HashSet<int> hitEngines)
        {
            if (_activeBoss == null || !_activeBoss.IsActive)
                return false;

            BossEngine[] engines = _activeBoss.Engines;
            if (engines == null)
                return false;

            for (int i = 0; i < engines.Length; i++)
            {
                BossEngine engine = engines[i];
                if (engine == null || !engine.IsAlive || !engine.IsReloading)
                    continue;

                int id = engine.GetInstanceID();
                if (hitEngines.Contains(id))
                    continue;

                float reach = explosionRadius + engine.CollisionRadius;
                if (((Vector2)engine.transform.position - explosionCenter).sqrMagnitude > reach * reach)
                    continue;

                if (engine.TryHit())
                {
                    hitEngines.Add(id);
                    return true;
                }
            }

            return false;
        }

        public void HandleBossEngineDestroyed(BossEngine engine, int enginesRemaining)
        {
            if (!_playing || engine == null)
                return;

            Vector2 position = engine.transform.position;
            AddScore(_config.BossEngineScore);
            AddCoins(_config.CoinPerBossEngine);

            // Each destroyed engine refills the current wave's missile supply.
            Ammo = GetWaveAmmo();

            _sfxPlayer?.PlayMissileExplosion(position);
            _cameraShake?.Shake(_config.BossEngineShakeDuration, _config.BossEngineShakeMagnitude);
            _haptics?.Pulse(HapticKind.Medium);
            _analytics?.Track("boss_engine_destroyed", new AnalyticsParam("wave", Wave), new AnalyticsParam("remaining", enginesRemaining));
            CreateExplosion(
                position,
                0.9f,
                0.58f,
                new Color(0.35f, 0.95f, 1f, 0.52f),
                false);
        }

        public void HandleBossDefeated(AlienBoss boss)
        {
            if (!_playing || boss == null || boss != _activeBoss)
                return;

            AddScore(_config.BossDefeatScore);
            AddCoins(_config.CoinPerBossDefeat);
            MetaProgression.AddCores(_config.BossCoreReward);
            GameSave.Data.bossesDefeated++;
            GameSave.MarkDirty();
            _analytics?.Track("boss_defeated", new AnalyticsParam("wave", Wave), new AnalyticsParam("score", Score), new AnalyticsParam("coins", Coins));
            _haptics?.Pulse(HapticKind.Heavy);
            _bossDefeatInProgress = true;
            StartCoroutine(BossDefeatSequence(boss));
        }

        private IEnumerator BossDefeatSequence(AlienBoss boss)
        {
            Vector2 center = boss.transform.position;

            // The boss death clears its remaining projectiles so victory is determined
            // strictly by destroying all four engines.
            for (int i = _activeAsteroids.Count - 1; i >= 0; i--)
            {
                Asteroid asteroid = _activeAsteroids[i];
                if (asteroid != null)
                    _pool.Despawn(asteroid.gameObject);
            }
            _activeAsteroids.Clear();

            _sfxPlayer?.PlayPlanetPulse(center);
            _cameraShake?.Shake(0.5f, 0.18f);

            CreateExplosion(center + new Vector2(-1.35f, -0.2f), 1.25f, 0.7f, new Color(0.9f, 0.25f, 1f, 0.45f), false);
            CreateExplosion(center + new Vector2(1.25f, 0.1f), 1.35f, 0.75f, new Color(0.35f, 0.9f, 1f, 0.45f), false);
            CreateExplosion(center, 2.25f, 1.05f, new Color(1f, 0.45f, 0.2f, 0.42f), false);

            yield return _waitBossDefeatBeat;

            if (boss != null)
                Destroy(boss.gameObject);

            if (_activeBoss == boss)
                _activeBoss = null;

            _bossDefeatInProgress = false;
            _musicPlayer?.SetMode(MusicMode.Gameplay);
        }

        private void OpenShop()
        {
            if (!_playing)
                return;

            _shopOpen = true;
        }

        public void CloseShop()
        {
            if (!_shopOpen)
                return;

            _shopOpen = false;
        }

        private bool TrySpendShopPoints(int cost)
        {
            cost = Mathf.Max(0, cost);
            if (!_shopOpen || _shopPoints < cost)
                return false;

            _shopPoints -= cost;
            return true;
        }

        public bool TryBuyMissileUpgrade()
        {
            if (_config == null || !CanUpgradeMissiles)
                return false;

            int cost = MissileUpgradeCost;
            if (!TrySpendShopPoints(cost))
                return false;

            _missileUpgradeLevel++;
            TrackShopPurchase("missile_supply", cost, _missileUpgradeLevel);
            return true;
        }

        public bool TryBuyMissilePowerUpgrade()
        {
            if (_config == null || !CanUpgradeMissilePower)
                return false;

            int cost = MissilePowerUpgradeCost;
            if (!TrySpendShopPoints(cost))
                return false;

            _missilePowerLevel++;
            TrackShopPurchase("blast_power", cost, _missilePowerLevel);
            return true;
        }

        private int GetMissilePowerUpgradeCost()
        {
            if (_config == null)
                return 0;

            long cost = Mathf.Max(0, _config.ShopPowerUpgradeBaseCost);
            int multiplier = Mathf.Max(2, _config.ShopPowerUpgradeCostMultiplier);
            for (int i = 0; i < _missilePowerLevel; i++)
                cost = System.Math.Min(999999L, cost * multiplier);
            return (int)cost;
        }

        private int GetCompletedBossTier()
        {
            if (_config == null || _config.BossWaveInterval <= 0)
                return 0;
            return Mathf.Max(0, Wave / _config.BossWaveInterval);
        }

        private int GetGuardianSlotCap()
        {
            if (_config == null)
                return 0;
            int bossTier = GetCompletedBossTier();
            // 10 -> 2, 20 -> 4, 30+ -> 5. This prevents filling the entire orbit before mid-game.
            return Mathf.Min(_config.ShopGuardianMaxCount, bossTier * 2);
        }

        private int GetCannonSlotCap()
        {
            if (_config == null)
                return 0;
            int bossTier = GetCompletedBossTier();
            // 10 -> 1, 20 -> 2, 30+ -> 3.
            return Mathf.Min(_config.ShopCannonMaxCount, bossTier);
        }

        public int NextGuardianSlotUnlockWave
        {
            get
            {
                if (_config == null || GetGuardianSlotCap() >= _config.ShopGuardianMaxCount) return 0;
                int nextTier = GetCompletedBossTier() + 1;
                return nextTier * _config.BossWaveInterval;
            }
        }

        public int NextCannonSlotUnlockWave
        {
            get
            {
                if (_config == null || GetCannonSlotCap() >= _config.ShopCannonMaxCount) return 0;
                int nextTier = GetCompletedBossTier() + 1;
                return nextTier * _config.BossWaveInterval;
            }
        }

        public bool TryBuyShieldCharge()
        {
            if (_config == null || _planetPulseCharges >= _config.MaxPulseCharges)
                return false;

            if (!TrySpendShopPoints(_config.ShopShieldChargeCost))
                return false;

            _planetPulseCharges++;
            RefreshPulseShield();
            TrackShopPurchase("pulse_charge", _config.ShopShieldChargeCost, _planetPulseCharges);
            return true;
        }

        public bool TryBuyGuardianHelper()
        {
            if (_config == null || !CanBuyGuardian || _orbitalHelperPrefab == null)
                return false;

            int cost = GuardianCost;
            if (!TrySpendShopPoints(cost))
                return false;

            _guardianCount++;
            _guardianPurchaseCount++;
            SpawnGuardianHelper(_guardianCount - 1);
            TrackShopPurchase("orbital_guardian", cost, _guardianCount);
            return true;
        }

        public bool TryBuyCannonHelper()
        {
            if (_config == null || !CanBuyCannon || _orbitalHelperPrefab == null)
                return false;

            int cost = CannonCost;
            if (!TrySpendShopPoints(cost))
                return false;

            _cannonCount++;
            _cannonPurchaseCount++;
            SpawnCannonHelper((_cannonPurchaseCount - 1) % Mathf.Max(1, _config.ShopCannonMaxCount));
            TrackShopPurchase("orbital_cannon", cost, _cannonCount);
            return true;
        }

        private void TrackShopPurchase(string item, int cost, int levelOrCount)
        {
            _analytics?.Track("shop_purchase",
                new AnalyticsParam("item", item),
                new AnalyticsParam("cost", cost),
                new AnalyticsParam("wave", Wave),
                new AnalyticsParam("level", levelOrCount),
                new AnalyticsParam("coins_left", Coins));
        }

        private void SpawnGuardianHelper(int index)
        {
            if (_orbitalHelperPrefab == null)
                return;

            GameObject go = Instantiate(_orbitalHelperPrefab, PlanetPosition, Quaternion.identity);
            go.name = $"OrbitalGuardian_{index + 1}";
            OrbitalHelper helper = go.GetComponent<OrbitalHelper>();
            if (helper == null)
            {
                Destroy(go);
                return;
            }

            _guardianHelpers.Add(helper);
            float spacing = 360f / Mathf.Max(1, _config.ShopGuardianMaxCount);
            helper.Initialize(
                this,
                OrbitalHelperType.Guardian,
                _config.GuardianOrbitRadius,
                _config.GuardianAngularSpeed,
                _config.GuardianCollisionRadius,
                999f,
                35f + spacing * index);
        }

        private void SpawnCannonHelper(int index)
        {
            if (_orbitalHelperPrefab == null)
                return;

            GameObject go = Instantiate(_orbitalHelperPrefab, PlanetPosition, Quaternion.identity);
            go.name = $"OrbitalCannon_{index + 1}";
            OrbitalHelper helper = go.GetComponent<OrbitalHelper>();
            if (helper == null)
            {
                Destroy(go);
                return;
            }

            _cannonHelpers.Add(helper);
            float spacing = 360f / Mathf.Max(1, _config.ShopCannonMaxCount);
            helper.Initialize(
                this,
                OrbitalHelperType.Cannon,
                _config.CannonOrbitRadius,
                _config.CannonAngularSpeed,
                _config.CannonCollisionRadius,
                _config.CannonFireInterval,
                205f + spacing * index);
        }

        private void RespawnOrbitalHelpers()
        {
            DestroyOrbitalHelperVisuals();
            for (int i = 0; i < _guardianCount; i++)
                SpawnGuardianHelper(i);
            for (int i = 0; i < _cannonCount; i++)
                SpawnCannonHelper(i);
        }

        private void DestroyOrbitalHelperVisuals()
        {
            for (int i = 0; i < _guardianHelpers.Count; i++)
                if (_guardianHelpers[i] != null) Destroy(_guardianHelpers[i].gameObject);
            for (int i = 0; i < _cannonHelpers.Count; i++)
                if (_cannonHelpers[i] != null) Destroy(_cannonHelpers[i].gameObject);
            _guardianHelpers.Clear();
            _cannonHelpers.Clear();
        }

        public bool IsAsteroidReservedByAnotherCannon(Asteroid asteroid, OrbitalHelper requester)
        {
            if (asteroid == null)
                return false;

            for (int i = 0; i < _cannonHelpers.Count; i++)
            {
                OrbitalHelper helper = _cannonHelpers[i];
                if (helper == null || helper == requester || !helper.IsActive)
                    continue;
                if (helper.ReservedTarget == asteroid)
                    return true;
            }

            return false;
        }

        public void FireOrbitalCannon(Vector2 origin, Asteroid target)
        {
            if (!IsPlaying || target == null || _missilePrefab == null)
                return;

            Missile missile = _pool.Spawn<Missile>(_missilePrefab, origin, Quaternion.identity);
            if (missile == null)
                return;

            _sfxPlayer?.PlayShot(origin);
            missile.InitializeHoming(this, origin, target, _config.CannonMissileSpeed, true);
        }

        public void HandleGuardianBlock(Asteroid asteroid, Vector2 helperPosition)
        {
            if (asteroid == null)
                return;

            Vector2 hit = asteroid.transform.position;
            HandleAsteroidDestroyed(asteroid, false, true, true);
            CreateExplosion(hit, 0.48f, 0.28f, new Color(0.25f, 0.85f, 1f, 0.48f), false);
        }

        public void HandleCannonHelperDestroyed(OrbitalHelper helper, Asteroid asteroid, Vector2 impactPosition)
        {
            if (helper == null)
                return;

            _cannonHelpers.Remove(helper);
            _cannonCount = Mathf.Max(0, _cannonCount - 1);
            _cannonsDestroyed++;

            if (asteroid != null)
            {
                _activeAsteroids.Remove(asteroid);
                _pool.Despawn(asteroid.gameObject);
            }

            _sfxPlayer?.PlayPlanetImpact(impactPosition);
            _cameraShake?.Shake(0.18f, 0.08f);
            CreateExplosion(impactPosition, 0.78f, 0.48f, new Color(1f, 0.48f, 0.12f, 0.52f), false);
            Destroy(helper.gameObject);
        }


        public void CreateOrbitalCannonExplosion(Vector2 position)
        {
            if (!_playing)
                return;

            _sfxPlayer?.PlayMissileExplosion(position);
            CreateExplosion(
                position,
                _config.CannonExplosionRadius,
                _config.CannonExplosionDuration,
                new Color(1f, 0.72f, 0.22f, 0.34f),
                true);
        }

        public void CreatePlayerExplosion(Vector2 position)
        {
            if (!_playing)
                return;

            _sfxPlayer?.PlayMissileExplosion(position);

            float radius = _config.ExplosionRadius + _missilePowerLevel * _config.ShopPowerRadiusBonusPerLevel;
            float duration = _config.ExplosionDuration + _missilePowerLevel * _config.ShopPowerDurationBonusPerLevel;
            CreateExplosion(
                position,
                radius,
                duration,
                GetPlayerExplosionColor(0.36f),
                true);
        }

        private void CreateExplosion(Vector2 position, float radius, float duration, Color color, bool damagesAsteroids)
        {
            Explosion explosion = _pool.Spawn<Explosion>(_explosionPrefab, position, Quaternion.identity);
            if (explosion != null)
                explosion.Initialize(this, position, radius, duration, color, damagesAsteroids);
        }

        public void DespawnAsteroid(Asteroid asteroid)
        {
            if (asteroid == null)
                return;

            _activeAsteroids.Remove(asteroid);
            _pool.Despawn(asteroid.gameObject);
        }

        public void DespawnMissile(Missile missile)
        {
            if (missile != null)
                _pool.Despawn(missile.gameObject);
        }

        public void DespawnExplosion(Explosion explosion)
        {
            if (explosion != null)
                _pool.Despawn(explosion.gameObject);
        }

        public void DespawnPlanetPulse(PlanetPulseWave pulse)
        {
            if (pulse != null)
                _pool.Despawn(pulse.gameObject);
        }

        private int CountAlive(PlanetTargetType type)
        {
            int count = 0;
            foreach (PlanetTarget target in _targets)
            {
                if (target != null && target.IsAlive && target.TargetType == type)
                    count++;
            }

            return count;
        }

        /// <summary>
        /// Editor/debug helper. Immediately jumps into the configured boss wave.
        /// Works both from the start screen and during a running game.
        /// </summary>
        public void DebugSpawnBossNow()
        {
            if (_config == null || !Application.isPlaying)
                return;

            if (!_hasStarted)
            {
                _debugStartWave = Mathf.Max(1, _config.BossWaveInterval);
                StartGame();
                return;
            }

            if (!_playing)
                return;

            if (_paused)
                ResumeGame();
            _shopOpen = false;
            _bossDefeatInProgress = false;

            if (_gameLoop != null)
            {
                StopCoroutine(_gameLoop);
                _gameLoop = null;
            }

            ClearDebugCombatants();
            Wave = GetNextBossWaveForDebug();
            Ammo = GetWaveAmmo();
            _gameLoop = StartCoroutine(DebugBossThenContinue());
        }

        private int GetNextBossWaveForDebug()
        {
            int interval = Mathf.Max(1, _config.BossWaveInterval);
            int current = Mathf.Max(1, Wave);
            int remainder = current % interval;
            return remainder == 0 ? current : current + (interval - remainder);
        }

        private IEnumerator DebugBossThenContinue()
        {
            yield return RunBossWave();

            if (!_playing)
                yield break;

            OpenShop();
            while (_playing && _shopOpen)
                yield return null;

            if (!_playing)
                yield break;

            yield return _waitInterWaveDelay;

            // Wave is the selected boss wave, so the regular loop continues with the next wave.
            _gameLoop = StartCoroutine(GameLoop());
        }

        private void ClearDebugCombatants()
        {
            for (int i = _activeAsteroids.Count - 1; i >= 0; i--)
            {
                Asteroid asteroid = _activeAsteroids[i];
                if (asteroid != null)
                    _pool.Despawn(asteroid.gameObject);
            }
            _activeAsteroids.Clear();

            if (_activeBoss != null)
            {
                Destroy(_activeBoss.gameObject);
                _activeBoss = null;
            }

            _bossDefeatInProgress = false;
        }

        public void PauseGame()
        {
            if (!_playing || _paused || _shopOpen)
                return;

            _paused = true;
            Time.timeScale = 0f;
            AudioListener.pause = true;
        }

        public void ResumeGame()
        {
            if (!_playing || !_paused)
                return;

            _paused = false;
            _nextPlayerShotRealtime = 0f;
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        public void TogglePause()
        {
            if (_paused)
                ResumeGame();
            else
                PauseGame();
        }

        private void EndGame()
        {
            if (!_playing || _debugGodMode)
                return;

            _playing = false;
            _paused = false;
            _shopOpen = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            SetPulseShieldVisible(false);
            if (_gameLoop != null)
                StopCoroutine(_gameLoop);

            if (_activeBoss != null)
            {
                Destroy(_activeBoss.gameObject);
                _activeBoss = null;
            }

            DestroyOrbitalHelperVisuals();

            _analytics?.Track("game_over",
                new AnalyticsParam("wave", Wave),
                new AnalyticsParam("score", Score),
                new AnalyticsParam("coins", Coins),
                new AnalyticsParam("shots", _shotsFired));
            _haptics?.Pulse(HapticKind.Heavy);
            _musicPlayer?.SetMode(MusicMode.Menu);
            SaveBestScore();
        }

        private void SaveBestScore()
        {
            GameSave.SetBestScore(BestScore);
            GameSave.SetHighestWave(Wave);
            GameSave.SaveNow();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (!pauseStatus)
                return;

            SaveBestScore();

            if (_playing && !_paused)
                PauseGame();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && _playing && !_paused && !_shopOpen)
                PauseGame();
        }

        private void OnApplicationQuit()
        {
            SaveBestScore();
        }

        private void OnDestroy()
        {
            GameSave.FlushIfDirty();
        }

        public void RestartGame()
        {
            ReloadSceneToMenu();
        }

        public void ReturnToMainMenu()
        {
            _analytics?.Track("return_to_menu", new AnalyticsParam("wave", Wave), new AnalyticsParam("score", Score));
            SaveBestScore();
            ReloadSceneToMenu();
        }

        private void ReloadSceneToMenu()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;

            Scene scene = SceneManager.GetActiveScene();
            if (scene.buildIndex >= 0)
                SceneManager.LoadScene(scene.buildIndex);
            else
                SceneManager.LoadScene(scene.name);
        }


    }
}

using UnityEngine;

namespace MissilePlanet
{
    public enum AsteroidVariant
    {
        Normal,
        RedSplitter,
        GreenArmored,
        YellowDodger,
        PulseCarrier
    }

    public sealed class Asteroid : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private TrailRenderer _trail;
        [SerializeField] private Sprite _classicSprite;
        [SerializeField] private Sprite _kamikazeSprite;

        private SpriteRenderer _carrierGlow;
        private MissilePlanetGame _game;
        private PlanetTarget _target;
        private Vector2 _velocity;
        private float _speed;
        private float _turnSpeedDegrees;
        private float _impactDistance;
        private bool _active;
        private bool _carrierEnteredScreen;
        private float _glowPhase;
        private float _redSplitElapsed;
        private float _redSplitDelay;
        private float _redBlinkTimer;
        private float _redBlinkSlowInterval;
        private float _redBlinkFastInterval;
        private bool _redBlinkBright;
        private Color _redBaseColor;
        private int _hitPoints;
        private int _maxHitPoints;
        private AsteroidVariant _variant;
        private int _yellowManeuversRemaining;
        private Vector2 _yellowWaypoint;
        private float _yellowWaypointReachDistance;
        private bool _yellowFinalDive;

        public float CollisionRadius { get; private set; } = 0.14f;
        public int SpawnSequence { get; private set; }
        public bool IsPulseCarrier => _variant == AsteroidVariant.PulseCarrier;
        public AsteroidVariant Variant => _variant;
        public int HitPoints => _hitPoints;

        public void ConfigureSkins(Sprite classicSprite, Sprite kamikazeSprite)
        {
            _classicSprite = classicSprite;
            _kamikazeSprite = kamikazeSprite;
        }

        private void Awake()
        {
            if (_renderer == null)
                _renderer = GetComponent<SpriteRenderer>();
            if (_trail == null)
                _trail = GetComponent<TrailRenderer>();

            Transform glowTransform = transform.Find("CarrierGlow");
            if (glowTransform != null)
                _carrierGlow = glowTransform.GetComponent<SpriteRenderer>();
        }

        public void Initialize(
            MissilePlanetGame game,
            PlanetTarget target,
            float speed,
            float turnSpeedDegrees,
            float impactDistance,
            float collisionRadius,
            AsteroidVariant variant = AsteroidVariant.Normal,
            int hitPoints = 1,
            float visualScaleMultiplier = 1f,
            float redSplitTravelFraction = 0.5f,
            float redBlinkSlowInterval = 0.55f,
            float redBlinkFastInterval = 0.08f)
        {
            _game = game;
            _target = target;
            unchecked { SpawnSequence++; }
            _speed = speed;
            _turnSpeedDegrees = turnSpeedDegrees;
            _impactDistance = impactDistance;
            CollisionRadius = collisionRadius * Mathf.Max(0.5f, visualScaleMultiplier);
            _active = true;
            _carrierEnteredScreen = false;
            _variant = variant;
            _maxHitPoints = Mathf.Max(1, hitPoints);
            _hitPoints = _maxHitPoints;

            _redSplitElapsed = 0f;
            _redBlinkTimer = 0f;
            _redBlinkBright = true;
            _redBlinkSlowInterval = Mathf.Max(0.05f, redBlinkSlowInterval);
            _redBlinkFastInterval = Mathf.Clamp(redBlinkFastInterval, 0.03f, _redBlinkSlowInterval);

            ResetCarrierGlow();
            ConfigureVisuals(visualScaleMultiplier);

            Vector2 desired = GetTargetPosition() - (Vector2)transform.position;
            _velocity = desired.sqrMagnitude > 0.0001f
                ? desired.normalized * _speed
                : Vector2.down * _speed;

            if (_variant == AsteroidVariant.RedSplitter)
            {
                float initialDistance = Mathf.Max(0.25f, desired.magnitude);
                float estimatedTravelTime = initialDistance / Mathf.Max(0.1f, _speed);
                _redSplitDelay = Mathf.Max(0.25f, estimatedTravelTime * Mathf.Clamp(redSplitTravelFraction, 0.2f, 0.8f));
            }
            else
            {
                _redSplitDelay = 0f;
            }
        }

        public void InitializeYellowDodger(
            MissilePlanetGame game,
            PlanetTarget target,
            float dashSpeed,
            float collisionRadius,
            int minManeuvers,
            int maxManeuvers,
            float waypointReachDistance)
        {
            _game = game;
            _target = target;
            unchecked { SpawnSequence++; }
            _speed = Mathf.Max(1f, dashSpeed);
            _turnSpeedDegrees = 0f;
            _impactDistance = 0.30f;
            CollisionRadius = collisionRadius;
            _active = true;
            _carrierEnteredScreen = false;
            _variant = AsteroidVariant.YellowDodger;
            _hitPoints = _maxHitPoints = 1;
            _yellowWaypointReachDistance = Mathf.Max(0.05f, waypointReachDistance);
            _yellowManeuversRemaining = Random.Range(Mathf.Max(1, minManeuvers), Mathf.Max(minManeuvers, maxManeuvers) + 1);
            _yellowFinalDive = false;
            _glowPhase = Random.Range(0f, Mathf.PI * 2f);

            ResetCarrierGlow();
            ConfigureVisuals(1f);
            ChooseNextYellowWaypoint(true);
        }

        public void InitializePulseCarrier(
            MissilePlanetGame game,
            Vector2 velocity,
            float collisionRadius)
        {
            _game = game;
            _target = null;
            unchecked { SpawnSequence++; }
            _velocity = velocity;
            _speed = velocity.magnitude;
            CollisionRadius = collisionRadius;
            _active = true;
            _variant = AsteroidVariant.PulseCarrier;
            _hitPoints = _maxHitPoints = 1;
            _carrierEnteredScreen = false;
            _glowPhase = Random.Range(0f, Mathf.PI * 2f);

            transform.localScale = Vector3.one * Random.Range(0.34f, 0.46f);

            if (_renderer != null)
            {
                if (_classicSprite != null)
                    _renderer.sprite = _classicSprite;
                _renderer.color = new Color(0.22f, 1f, 0.92f, 1f);
            }

            if (_carrierGlow != null)
            {
                _carrierGlow.enabled = true;
                _carrierGlow.color = new Color(0.18f, 1f, 0.92f, 0.3f);
            }

            if (_trail != null)
            {
                _trail.Clear();
                _trail.time = 0.72f;
                _trail.startWidth = 0.19f;
                _trail.endWidth = 0f;
                _trail.startColor = new Color(0.25f, 1f, 0.95f, 0.95f);
                _trail.endColor = new Color(0.10f, 0.75f, 1f, 0f);
            }
        }

        private void ConfigureVisuals(float visualScaleMultiplier)
        {
            float baseScale = Random.Range(0.26f, 0.42f) * visualScaleMultiplier;
            transform.localScale = Vector3.one * baseScale;

            if (_trail != null)
            {
                _trail.Clear();
                _trail.time = 0.55f;
                _trail.startWidth = 0.13f * Mathf.Lerp(1f, visualScaleMultiplier, 0.55f);
                _trail.endWidth = 0f;
            }

            if (_renderer == null)
                return;

            if (_game != null && _game.UseKamikazeAsteroids && _kamikazeSprite != null)
                _renderer.sprite = _kamikazeSprite;
            else if (_classicSprite != null)
                _renderer.sprite = _classicSprite;

            switch (_variant)
            {
                case AsteroidVariant.RedSplitter:
                    _redBaseColor = new Color(1f, 0.16f, 0.10f, 1f);
                    _renderer.color = _redBaseColor;
                    if (_trail != null)
                    {
                        _trail.startColor = new Color(1f, 0.18f, 0.08f, 0.95f);
                        _trail.endColor = new Color(1f, 0.04f, 0.01f, 0f);
                    }
                    break;

                case AsteroidVariant.GreenArmored:
                    _renderer.color = new Color(0.28f, 1f, 0.28f, 1f);
                    if (_trail != null)
                    {
                        _trail.startColor = new Color(0.35f, 1f, 0.35f, 0.86f);
                        _trail.endColor = new Color(0.06f, 0.65f, 0.14f, 0f);
                    }
                    break;

                case AsteroidVariant.YellowDodger:
                    _renderer.color = new Color(1f, 0.92f, 0.10f, 1f);
                    if (_trail != null)
                    {
                        _trail.time = 0.34f;
                        _trail.startWidth = 0.17f;
                        _trail.startColor = new Color(1f, 0.95f, 0.18f, 0.96f);
                        _trail.endColor = new Color(1f, 0.46f, 0.04f, 0f);
                    }
                    if (_carrierGlow != null)
                    {
                        _carrierGlow.enabled = true;
                        _carrierGlow.color = new Color(1f, 0.88f, 0.12f, 0.22f);
                        _carrierGlow.transform.localScale = Vector3.one * 2.1f;
                    }
                    break;

                default:
                    float brightness = Random.Range(0.78f, 1f);
                    _renderer.color = new Color(0.72f * brightness, 0.44f * brightness, 0.24f * brightness, 1f);
                    if (_trail != null)
                    {
                        _trail.startColor = new Color(1f, 0.42f, 0.12f, 0.85f);
                        _trail.endColor = new Color(1f, 0.1f, 0.02f, 0f);
                    }
                    break;
            }
        }

        private void ResetCarrierGlow()
        {
            if (_carrierGlow != null)
            {
                _carrierGlow.enabled = false;
                _carrierGlow.transform.localScale = Vector3.one * 2.4f;
            }
        }

        private void Update()
        {
            if (!_active || _game == null || !_game.IsPlaying)
                return;

            if (IsPulseCarrier)
            {
                UpdatePulseCarrier();
                return;
            }

            if (_variant == AsteroidVariant.YellowDodger)
            {
                UpdateYellowDodger();
                return;
            }

            if (_variant == AsteroidVariant.RedSplitter && UpdateRedSplitterWarning())
                return;

            if (_target == null || !_target.IsAlive)
                _target = _game.GetRandomAsteroidTarget();

            if (_target == null)
            {
                _game.DespawnAsteroid(this);
                return;
            }

            Vector2 position = transform.position;
            Vector2 targetPosition = GetTargetPosition();
            Vector2 toTarget = targetPosition - position;

            if (toTarget.magnitude <= _impactDistance)
            {
                ImpactTarget(position);
                return;
            }

            Vector2 desiredDirection = toTarget.normalized;
            Vector2 currentDirection = _velocity.sqrMagnitude > 0.0001f
                ? _velocity.normalized
                : desiredDirection;

            float signedAngle = Vector2.SignedAngle(currentDirection, desiredDirection);
            float maxTurn = _turnSpeedDegrees * Time.deltaTime;
            float turn = Mathf.Clamp(signedAngle, -maxTurn, maxTurn);
            Vector2 turnedDirection = Quaternion.Euler(0f, 0f, turn) * currentDirection;

            _velocity = turnedDirection.normalized * _speed;
            Vector2 nextPosition = position + _velocity * Time.deltaTime;

            if (_game.TryGetPlanetSurfaceIntersection(position, nextPosition, CollisionRadius, out Vector2 impactPoint))
            {
                transform.position = impactPoint;
                ImpactPlanet(impactPoint);
                return;
            }

            transform.position = nextPosition;
            if (_game.UseKamikazeAsteroids)
            {
                float shipAngle = Mathf.Atan2(_velocity.y, _velocity.x) * Mathf.Rad2Deg - 90f;
                transform.rotation = Quaternion.Euler(0f, 0f, shipAngle);
            }
            else
            {
                transform.Rotate(0f, 0f, 110f * Time.deltaTime, Space.Self);
            }
        }

        private bool UpdateRedSplitterWarning()
        {
            _redSplitElapsed += Time.deltaTime;

            if (_redSplitDelay > 0f && _redSplitElapsed >= _redSplitDelay)
            {
                _active = false;
                _game?.HandleRedAsteroidTimedSplit(this);
                return true;
            }

            if (_renderer == null || _redSplitDelay <= 0f)
                return false;

            float progress = Mathf.Clamp01(_redSplitElapsed / _redSplitDelay);
            // Start calm and become increasingly urgent as the automatic split approaches.
            float accelerated = progress * progress;
            float interval = Mathf.Lerp(_redBlinkSlowInterval, _redBlinkFastInterval, accelerated);
            _redBlinkTimer += Time.deltaTime;

            if (_redBlinkTimer >= interval)
            {
                _redBlinkTimer = 0f;
                _redBlinkBright = !_redBlinkBright;
            }

            float brightness = _redBlinkBright ? 1f : Mathf.Lerp(0.42f, 0.68f, progress);
            _renderer.color = new Color(
                _redBaseColor.r * brightness,
                _redBaseColor.g * brightness,
                _redBaseColor.b * brightness,
                1f);

            if (_trail != null)
            {
                Color start = _trail.startColor;
                start.a = _redBlinkBright ? 0.95f : 0.34f;
                _trail.startColor = start;
            }

            return false;
        }

        private void UpdateYellowDodger()
        {
            if (_target == null || !_target.IsAlive)
                _target = _game.GetRandomAsteroidTarget();

            Vector2 position = transform.position;
            Vector2 destination = _yellowFinalDive
                ? (_target != null ? (Vector2)_target.transform.position : _game.PlanetPosition)
                : _yellowWaypoint;

            Vector2 toDestination = destination - position;
            if (!_yellowFinalDive && toDestination.magnitude <= _yellowWaypointReachDistance)
            {
                _yellowManeuversRemaining--;
                if (_yellowManeuversRemaining <= 0)
                {
                    _yellowFinalDive = true;
                    destination = _target != null ? (Vector2)_target.transform.position : _game.PlanetPosition;
                }
                else
                {
                    ChooseNextYellowWaypoint(false);
                    destination = _yellowWaypoint;
                }
                toDestination = destination - position;
            }

            if (_yellowFinalDive && _target != null && toDestination.magnitude <= _impactDistance)
            {
                ImpactTarget(position);
                return;
            }

            Vector2 direction = toDestination.sqrMagnitude > 0.0001f ? toDestination.normalized : Vector2.down;
            _velocity = direction * _speed;
            Vector2 nextPosition = Vector2.MoveTowards(position, destination, _speed * Time.deltaTime);

            if (_game.TryGetPlanetSurfaceIntersection(position, nextPosition, CollisionRadius, out Vector2 impactPoint))
            {
                transform.position = impactPoint;
                ImpactPlanet(impactPoint);
                return;
            }

            transform.position = nextPosition;
            float angle = Mathf.Atan2(_velocity.y, _velocity.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            if (_carrierGlow != null)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 18f + _glowPhase);
                Color c = _carrierGlow.color;
                c.a = Mathf.Lerp(0.12f, 0.38f, pulse);
                _carrierGlow.color = c;
            }
        }

        private void ChooseNextYellowWaypoint(bool first)
        {
            Camera camera = _game != null ? _game.GameCamera : null;
            if (camera == null)
            {
                _yellowWaypoint = (Vector2)transform.position + Vector2.down * 2f;
                return;
            }

            Vector3 viewport = camera.WorldToViewportPoint(transform.position);
            bool goRight = viewport.x < 0.5f;
            float x = goRight ? Random.Range(0.66f, 0.92f) : Random.Range(0.08f, 0.34f);
            if (first && Random.value < 0.5f)
                x = 1f - x;
            float y = Random.Range(0.42f, 0.90f);
            Vector3 world = camera.ViewportToWorldPoint(new Vector3(x, y, Mathf.Abs(camera.transform.position.z)));
            _yellowWaypoint = new Vector2(world.x, world.y);
        }

        private void UpdatePulseCarrier()
        {
            transform.position = (Vector2)transform.position + _velocity * Time.deltaTime;
            transform.Rotate(0f, 0f, 190f * Time.deltaTime, Space.Self);

            if (_carrierGlow != null)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 6.5f + _glowPhase);
                Color color = _carrierGlow.color;
                color.a = Mathf.Lerp(0.18f, 0.52f, pulse);
                _carrierGlow.color = color;
                _carrierGlow.transform.localScale = Vector3.one * Mathf.Lerp(2.15f, 2.75f, pulse);
            }

            Camera camera = _game.GameCamera;
            if (camera == null)
                return;

            Vector3 viewport = camera.WorldToViewportPoint(transform.position);
            bool inside = viewport.z >= 0f && viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f;
            if (inside)
            {
                _carrierEnteredScreen = true;
                return;
            }

            if (_carrierEnteredScreen && (viewport.x < -0.12f || viewport.x > 1.12f || viewport.y < -0.12f || viewport.y > 1.12f))
                _game.DespawnAsteroid(this);
        }

        public void HitByExplosion()
        {
            if (!_active)
                return;

            if (_variant == AsteroidVariant.GreenArmored && _hitPoints > 1)
            {
                _hitPoints--;
                FlashArmoredHit();
                return;
            }

            _active = false;
            _game?.HandleAsteroidDestroyed(this, true, true, true);
        }

        public void HitByPlanetPulse()
        {
            if (!_active)
                return;

            _active = false;
            _game?.HandleAsteroidDestroyed(this, false, false, false);
        }

        private void FlashArmoredHit()
        {
            if (_renderer == null)
                return;

            float healthFraction = _maxHitPoints <= 1 ? 0f : (_hitPoints - 1f) / (_maxHitPoints - 1f);
            _renderer.color = Color.Lerp(new Color(0.75f, 1f, 0.45f, 1f), new Color(0.16f, 0.82f, 0.20f, 1f), healthFraction);
            transform.localScale *= 0.94f;
            _game?.PlayArmoredAsteroidHit(transform.position);
        }

        private void ImpactTarget(Vector2 impactPosition)
        {
            if (!_active)
                return;

            _active = false;
            _game.HandleAsteroidImpact(this, _target, impactPosition);
        }

        private void ImpactPlanet(Vector2 impactPosition)
        {
            if (!_active)
                return;

            _active = false;
            _game.HandleAsteroidSurfaceImpact(this, _target, impactPosition);
        }

        private Vector2 GetTargetPosition()
        {
            if (_target != null)
                return _target.transform.position;

            return _game != null ? _game.PlanetPosition : Vector2.zero;
        }
    }
}

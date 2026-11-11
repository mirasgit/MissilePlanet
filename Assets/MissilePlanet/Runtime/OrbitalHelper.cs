using UnityEngine;

namespace MissilePlanet
{
    public enum OrbitalHelperType
    {
        Guardian,
        Cannon
    }

    public sealed class OrbitalHelper : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _bodyRenderer;
        [SerializeField] private SpriteRenderer _coreRenderer;

        private MissilePlanetGame _game;
        private OrbitalHelperType _type;
        private float _orbitRadius;
        private float _angularSpeed;
        private float _collisionRadius;
        private float _fireInterval;
        private float _fireTimer;
        private float _angle;
        private bool _active;
        private Asteroid _reservedTarget;
        private int _reservedSpawnSequence;

        public OrbitalHelperType HelperType => _type;
        public bool IsActive => _active;
        public Asteroid ReservedTarget
        {
            get
            {
                if (_reservedTarget == null || !_reservedTarget.gameObject.activeInHierarchy || _reservedTarget.SpawnSequence != _reservedSpawnSequence)
                    return null;
                return _reservedTarget;
            }
        }

        private void Awake()
        {
            if (_bodyRenderer == null)
                _bodyRenderer = GetComponent<SpriteRenderer>();

            if (_coreRenderer == null)
            {
                Transform core = transform.Find("Core");
                if (core != null)
                    _coreRenderer = core.GetComponent<SpriteRenderer>();
            }
        }

        public void Initialize(
            MissilePlanetGame game,
            OrbitalHelperType type,
            float orbitRadius,
            float angularSpeed,
            float collisionRadius,
            float fireInterval,
            float startAngle)
        {
            _game = game;
            _type = type;
            _orbitRadius = Mathf.Max(0.1f, orbitRadius);
            _angularSpeed = angularSpeed;
            _collisionRadius = Mathf.Max(0.05f, collisionRadius);
            _fireInterval = Mathf.Max(0.2f, fireInterval);
            _fireTimer = Random.Range(0.35f, _fireInterval);
            _angle = startAngle;
            _active = true;
            _reservedTarget = null;
            _reservedSpawnSequence = 0;

            ApplyVisuals();
            UpdateOrbitPosition();
        }

        private void Update()
        {
            if (!_active || _game == null || !_game.IsPlaying)
                return;

            _angle += _angularSpeed * Time.deltaTime;
            UpdateOrbitPosition();

            if (_type == OrbitalHelperType.Guardian)
            {
                CheckGuardianCollision();
                return;
            }

            if (CheckCannonCollision())
                return;

            if (ReservedTarget == null)
                _reservedTarget = null;

            _fireTimer -= Time.deltaTime;
            if (_fireTimer <= 0f)
            {
                _fireTimer = _fireInterval;
                // Release our previous claim before selecting a new target. Other cannons keep theirs.
                _reservedTarget = null;
                Asteroid target = FindPriorityTarget();
                if (target != null)
                {
                    _reservedTarget = target;
                    _reservedSpawnSequence = target.SpawnSequence;
                    _game.FireOrbitalCannon(transform.position, target);
                }
            }
        }

        private void UpdateOrbitPosition()
        {
            if (_game == null)
                return;

            float radians = _angle * Mathf.Deg2Rad;
            Vector2 offset = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * _orbitRadius;
            transform.position = _game.PlanetPosition + offset;
            transform.rotation = Quaternion.Euler(0f, 0f, _angle - 90f);
        }

        private void CheckGuardianCollision()
        {
            var asteroids = _game.ActiveAsteroids;
            for (int i = asteroids.Count - 1; i >= 0; i--)
            {
                Asteroid asteroid = asteroids[i];
                if (!IsValidAsteroid(asteroid))
                    continue;

                float reach = _collisionRadius + asteroid.CollisionRadius;
                if (((Vector2)asteroid.transform.position - (Vector2)transform.position).sqrMagnitude > reach * reach)
                    continue;

                _game.HandleGuardianBlock(asteroid, transform.position);
                return;
            }
        }

        private bool CheckCannonCollision()
        {
            var asteroids = _game.ActiveAsteroids;
            for (int i = asteroids.Count - 1; i >= 0; i--)
            {
                Asteroid asteroid = asteroids[i];
                if (!IsValidAsteroid(asteroid))
                    continue;

                float reach = _collisionRadius + asteroid.CollisionRadius;
                if (((Vector2)asteroid.transform.position - (Vector2)transform.position).sqrMagnitude > reach * reach)
                    continue;

                _active = false;
                _reservedTarget = null;
                _game.HandleCannonHelperDestroyed(this, asteroid, transform.position);
                return true;
            }

            return false;
        }

        private Asteroid FindPriorityTarget()
        {
            var asteroids = _game.ActiveAsteroids;
            Asteroid best = null;
            float bestDistance = float.PositiveInfinity;

            for (int i = 0; i < asteroids.Count; i++)
            {
                Asteroid asteroid = asteroids[i];
                if (!IsValidAsteroid(asteroid))
                    continue;
                if (_game.IsAsteroidReservedByAnotherCannon(asteroid, this))
                    continue;

                // Protect the planet first: prefer the unclaimed asteroid closest to Earth.
                float distance = ((Vector2)asteroid.transform.position - _game.PlanetPosition).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = asteroid;
                }
            }

            return best;
        }

        private static bool IsValidAsteroid(Asteroid asteroid)
        {
            return asteroid != null && asteroid.gameObject.activeInHierarchy;
        }

        private void ApplyVisuals()
        {
            if (_type == OrbitalHelperType.Guardian)
            {
                transform.localScale = Vector3.one * 0.95f;
                if (_bodyRenderer != null)
                    _bodyRenderer.color = new Color(0.22f, 0.78f, 1f, 1f);
                if (_coreRenderer != null)
                    _coreRenderer.color = new Color(0.72f, 0.95f, 1f, 1f);
            }
            else
            {
                transform.localScale = Vector3.one * 0.66f;
                if (_bodyRenderer != null)
                    _bodyRenderer.color = new Color(1f, 0.62f, 0.16f, 1f);
                if (_coreRenderer != null)
                    _coreRenderer.color = new Color(1f, 0.94f, 0.52f, 1f);
            }
        }
    }
}

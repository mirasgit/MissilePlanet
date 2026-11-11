using System.Collections.Generic;
using UnityEngine;

namespace MissilePlanet
{
    public sealed class Explosion : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;

        private readonly HashSet<long> _hitAsteroids = new HashSet<long>();
        private readonly HashSet<int> _hitBossEngines = new HashSet<int>();
        private MissilePlanetGame _game;
        private float _maxRadius;
        private float _duration;
        private float _elapsed;
        private Color _baseColor;
        private bool _active;
        private bool _damagesAsteroids;
        private bool _bossEngineHitConsumed;

        private void Awake()
        {
            if (_renderer == null)
                _renderer = GetComponent<SpriteRenderer>();

            if (_renderer != null)
                _baseColor = _renderer.color;
        }

        public void Initialize(
            MissilePlanetGame game,
            Vector2 position,
            float maxRadius,
            float duration,
            Color color,
            bool damagesAsteroids)
        {
            _game = game;
            _maxRadius = maxRadius;
            _duration = Mathf.Max(0.05f, duration);
            _elapsed = 0f;
            _active = true;
            _damagesAsteroids = damagesAsteroids;
            transform.position = position;
            transform.localScale = Vector3.zero;
            _baseColor = color;
            _hitAsteroids.Clear();
            _hitBossEngines.Clear();
            _bossEngineHitConsumed = false;

            if (_renderer != null)
                _renderer.color = _baseColor;
        }

        private void Update()
        {
            if (!_active)
                return;

            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);
            float radius = _maxRadius * EaseOutCubic(t);
            transform.localScale = Vector3.one * radius * 2f;

            if (_renderer != null)
            {
                Color c = _baseColor;
                c.a = _baseColor.a * (1f - t);
                _renderer.color = c;
            }

            if (_damagesAsteroids && _game != null)
            {
                var asteroids = _game.ActiveAsteroids;
                for (int i = asteroids.Count - 1; i >= 0; i--)
                {
                    Asteroid asteroid = asteroids[i];
                    if (asteroid == null || !asteroid.gameObject.activeInHierarchy)
                        continue;

                    float maxDistance = radius + asteroid.CollisionRadius;
                    if (((Vector2)asteroid.transform.position - (Vector2)transform.position).sqrMagnitude > maxDistance * maxDistance)
                        continue;

                    long hitKey = ((long)asteroid.GetInstanceID() << 32) ^ (uint)asteroid.SpawnSequence;
                    if (_hitAsteroids.Add(hitKey))
                        asteroid.HitByExplosion();
                }

                if (!_bossEngineHitConsumed && _game.TryDamageBossEngines(transform.position, radius, _hitBossEngines))
                    _bossEngineHitConsumed = true;
            }

            if (t >= 1f)
            {
                _active = false;
                _game?.DespawnExplosion(this);
            }
        }

        private static float EaseOutCubic(float t)
        {
            float u = 1f - t;
            return 1f - u * u * u;
        }
    }
}

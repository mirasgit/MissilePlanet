using System.Collections.Generic;
using UnityEngine;

namespace MissilePlanet
{
    public sealed class PlanetPulseWave : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;

        private struct CapturedAsteroid
        {
            public Asteroid Asteroid;
            public int SpawnSequence;
        }

        private readonly List<CapturedAsteroid> _targets = new List<CapturedAsteroid>();
        private MissilePlanetGame _game;
        private float _startRadius;
        private float _maxRadius;
        private float _duration;
        private float _elapsed;
        private Color _baseColor;
        private bool _active;

        private void Awake()
        {
            if (_renderer == null)
                _renderer = GetComponent<SpriteRenderer>();
        }

        public void Initialize(
            MissilePlanetGame game,
            Vector2 origin,
            float startRadius,
            float maxRadius,
            float duration,
            Color color,
            IReadOnlyList<Asteroid> asteroids,
            Camera camera)
        {
            _game = game;
            _startRadius = Mathf.Max(0.01f, startRadius);
            _maxRadius = Mathf.Max(_startRadius, maxRadius);
            _duration = Mathf.Max(0.05f, duration);
            _elapsed = 0f;
            _baseColor = color;
            _active = true;

            transform.position = origin;
            transform.localScale = Vector3.one * _startRadius * 2f;

            if (_renderer != null)
                _renderer.color = _baseColor;

            _targets.Clear();
            if (asteroids == null || camera == null)
                return;

            // Snapshot only the asteroids that are on screen at activation. SpawnSequence
            // prevents a pooled asteroid that gets reused during the wave from being mistaken
            // for the old asteroid captured here.
            for (int i = 0; i < asteroids.Count; i++)
            {
                Asteroid asteroid = asteroids[i];
                if (asteroid == null || !asteroid.gameObject.activeInHierarchy)
                    continue;

                Vector3 viewport = camera.WorldToViewportPoint(asteroid.transform.position);
                if (viewport.z >= 0f && viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f)
                {
                    _targets.Add(new CapturedAsteroid
                    {
                        Asteroid = asteroid,
                        SpawnSequence = asteroid.SpawnSequence
                    });
                }
            }
        }

        private void Update()
        {
            if (!_active)
                return;

            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);
            float radius = Mathf.Lerp(_startRadius, _maxRadius, EaseOutCubic(t));
            transform.localScale = Vector3.one * radius * 2f;

            if (_renderer != null)
            {
                Color color = _baseColor;
                color.a = _baseColor.a * Mathf.Lerp(1f, 0.12f, t);
                _renderer.color = color;
            }

            Vector2 origin = transform.position;
            for (int i = _targets.Count - 1; i >= 0; i--)
            {
                CapturedAsteroid captured = _targets[i];
                Asteroid asteroid = captured.Asteroid;
                if (!IsSameActiveAsteroid(captured))
                {
                    _targets.RemoveAt(i);
                    continue;
                }

                float hitDistance = radius + asteroid.CollisionRadius;
                if (((Vector2)asteroid.transform.position - origin).sqrMagnitude <= hitDistance * hitDistance)
                {
                    asteroid.HitByPlanetPulse();
                    _targets.RemoveAt(i);
                }
            }

            if (t < 1f)
                return;

            // Numerical safety: every still-existing asteroid captured at activation is removed.
            for (int i = _targets.Count - 1; i >= 0; i--)
            {
                CapturedAsteroid captured = _targets[i];
                if (IsSameActiveAsteroid(captured))
                    captured.Asteroid.HitByPlanetPulse();
            }
            _targets.Clear();

            _active = false;
            _game?.DespawnPlanetPulse(this);
        }

        private static bool IsSameActiveAsteroid(CapturedAsteroid captured)
        {
            return captured.Asteroid != null
                && captured.Asteroid.gameObject.activeInHierarchy
                && captured.Asteroid.SpawnSequence == captured.SpawnSequence;
        }

        private static float EaseOutCubic(float t)
        {
            float u = 1f - t;
            return 1f - u * u * u;
        }
    }
}

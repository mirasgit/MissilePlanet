using UnityEngine;

namespace MissilePlanet
{
    public enum PlanetTargetType
    {
        City,
        Launcher
    }

    public sealed class PlanetTarget : MonoBehaviour
    {
        [SerializeField] private PlanetTargetType _targetType = PlanetTargetType.City;
        [SerializeField, Min(1)] private int _maxHealth = 1;
        [SerializeField] private Color _destroyedColor = new Color(0.12f, 0.12f, 0.14f, 0.9f);

        private SpriteRenderer[] _renderers;
        private Color[] _aliveColors;
        private int _health;

        public PlanetTargetType TargetType => _targetType;
        public bool IsAlive => _health > 0;
        public int Health => _health;

        private void Awake()
        {
            _renderers = GetComponentsInChildren<SpriteRenderer>(true);
            _aliveColors = new Color[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
                _aliveColors[i] = _renderers[i].color;

            ResetTarget();
        }

        public void Configure(PlanetTargetType targetType, int maxHealth = 1)
        {
            _targetType = targetType;
            _maxHealth = Mathf.Max(1, maxHealth);
        }

        public void ResetTarget()
        {
            _health = Mathf.Max(1, _maxHealth);
            if (_renderers == null || _aliveColors == null)
                return;

            for (int i = 0; i < _renderers.Length; i++)
                _renderers[i].color = _aliveColors[i];
        }

        public void Revive()
        {
            ResetTarget();
        }

        public void TakeDamage(int amount)
        {
            if (!IsAlive)
                return;

            _health = Mathf.Max(0, _health - Mathf.Max(1, amount));
            if (IsAlive || _renderers == null)
                return;

            for (int i = 0; i < _renderers.Length; i++)
            {
                Color c = _destroyedColor;
                c.a = _aliveColors != null && i < _aliveColors.Length ? _aliveColors[i].a : 1f;
                _renderers[i].color = c;
            }
        }
    }
}

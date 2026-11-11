using UnityEngine;

namespace MissilePlanet
{
    public sealed class Missile : MonoBehaviour
    {
        [SerializeField] private TrailRenderer _trail;
        [SerializeField] private SpriteRenderer _renderer;

        private MissilePlanetGame _game;
        private Vector2 _target;
        private float _speed;
        private bool _active;
        private Asteroid _homingTarget;
        private int _homingSpawnSequence;
        private bool _useOrbitalExplosion;

        private void Awake()
        {
            if (_trail == null)
                _trail = GetComponent<TrailRenderer>();
            if (_renderer == null)
                _renderer = GetComponent<SpriteRenderer>();
        }

        public void Initialize(MissilePlanetGame game, Vector2 start, Vector2 target, float speed)
        {
            _game = game;
            _target = target;
            _speed = speed;
            _active = true;
            _homingTarget = null;
            _homingSpawnSequence = 0;
            _useOrbitalExplosion = false;

            if (_trail != null)
                _trail.Clear();

            ApplyCosmeticColor();
            transform.position = start;
            transform.localScale = new Vector3(0.16f, 0.24f, 1f);
            FaceTarget();
        }

        public void InitializeHoming(MissilePlanetGame game, Vector2 start, Asteroid target, float speed, bool useOrbitalExplosion = false)
        {
            if (target == null)
            {
                Initialize(game, start, start, speed);
                return;
            }

            _game = game;
            _target = target.transform.position;
            _speed = speed;
            _active = true;
            _homingTarget = target;
            _homingSpawnSequence = target.SpawnSequence;
            _useOrbitalExplosion = useOrbitalExplosion;

            if (_trail != null)
                _trail.Clear();

            ApplyCosmeticColor();
            transform.position = start;
            transform.localScale = new Vector3(0.13f, 0.20f, 1f);
            FaceTarget();
        }


        private void ApplyCosmeticColor()
        {
            if (_game == null)
                return;

            Color color = _game.GetNextMissileCosmeticColor();
            if (_renderer != null)
                _renderer.color = color;

            if (_trail != null)
            {
                Color start = color;
                start.a = 0.95f;
                Color end = color;
                end.a = 0f;
                _trail.startColor = start;
                _trail.endColor = end;
            }
        }

        private void Update()
        {
            if (!_active || _game == null || !_game.IsPlaying)
                return;

            if (_homingTarget != null)
            {
                if (_homingTarget.gameObject.activeInHierarchy && _homingTarget.SpawnSequence == _homingSpawnSequence)
                    _target = _homingTarget.transform.position;
                else
                    _homingTarget = null;
            }

            Vector2 position = transform.position;
            float step = _speed * Time.deltaTime;
            Vector2 next = Vector2.MoveTowards(position, _target, step);
            transform.position = next;
            FaceTarget();

            if ((next - _target).sqrMagnitude <= 0.0025f)
            {
                _active = false;
                if (_useOrbitalExplosion)
                    _game.CreateOrbitalCannonExplosion(_target);
                else
                    _game.CreatePlayerExplosion(_target);
                _game.DespawnMissile(this);
            }
        }

        private void FaceTarget()
        {
            Vector2 direction = _target - (Vector2)transform.position;
            if (direction.sqrMagnitude < 0.0001f)
                return;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}

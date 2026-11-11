using UnityEngine;

namespace MissilePlanet
{
    public sealed class PlanetController : MonoBehaviour
    {
        [SerializeField] private float _rotationSpeed = 7f;

        public float RotationSpeed
        {
            get => _rotationSpeed;
            set => _rotationSpeed = value;
        }

        private void Update()
        {
            transform.Rotate(0f, 0f, _rotationSpeed * Time.deltaTime, Space.Self);
        }
    }
}

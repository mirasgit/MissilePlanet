using System.Collections;
using UnityEngine;

namespace MissilePlanet
{
    public sealed class CameraShake : MonoBehaviour
    {
        private Coroutine _routine;
        private Vector3 _baseLocalPosition;

        private void Awake()
        {
            _baseLocalPosition = transform.localPosition;
        }

        public void Shake(float duration, float magnitude)
        {
            if (!GameSettings.ScreenShakeEnabled || duration <= 0f || magnitude <= 0f)
                return;

            if (_routine != null)
                StopCoroutine(_routine);

            _routine = StartCoroutine(ShakeRoutine(duration, magnitude));
        }

        private IEnumerator ShakeRoutine(float duration, float magnitude)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                Vector2 offset = Random.insideUnitCircle * magnitude;
                transform.localPosition = _baseLocalPosition + new Vector3(offset.x, offset.y, 0f);
                yield return null;
            }

            transform.localPosition = _baseLocalPosition;
            _routine = null;
        }
    }
}

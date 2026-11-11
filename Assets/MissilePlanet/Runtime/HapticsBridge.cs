using UnityEngine;

namespace MissilePlanet
{
    public enum HapticKind
    {
        Light,
        Medium,
        Heavy
    }

    /// <summary>
    /// SDK-neutral haptic seam. The built-in fallback only vibrates for major events on device.
    /// Replace/extend this component with a native haptics plugin for short impact patterns.
    /// </summary>
    public class HapticsBridge : MonoBehaviour
    {
        private float _nextAllowedRealtime;

        public virtual void Pulse(HapticKind kind)
        {
            if (!GameSettings.HapticsEnabled || Time.realtimeSinceStartup < _nextAllowedRealtime)
                return;

            _nextAllowedRealtime = Time.realtimeSinceStartup + (kind == HapticKind.Heavy ? 0.45f : 0.18f);

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            if (kind == HapticKind.Heavy)
                Handheld.Vibrate();
#endif
        }
    }
}

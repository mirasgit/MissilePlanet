using System;
using UnityEngine;

namespace MissilePlanet
{
    [Serializable]
    public struct AnalyticsParam
    {
        public string Key;
        public string Value;

        public AnalyticsParam(string key, object value)
        {
            Key = key ?? string.Empty;
            Value = value != null ? value.ToString() : string.Empty;
        }
    }

    /// <summary>
    /// Implement this class in a separate integration script (Firebase, GameAnalytics, etc.)
    /// and assign it to AnalyticsBridge. Gameplay never references an SDK directly.
    /// </summary>
    public abstract class AnalyticsProviderBehaviour : MonoBehaviour
    {
        public abstract bool IsReady { get; }
        public abstract void LogEvent(string eventName, AnalyticsParam[] parameters);
    }

    public sealed class AnalyticsBridge : MonoBehaviour
    {
        [SerializeField] private AnalyticsProviderBehaviour _provider;
        [SerializeField] private bool _logToConsoleInEditor;

        public bool IsConnected => _provider != null && _provider.IsReady;

        private void Awake()
        {
            if (_provider == null)
                _provider = GetComponent<AnalyticsProviderBehaviour>();
        }

        public void Track(string eventName, params AnalyticsParam[] parameters)
        {
            if (string.IsNullOrWhiteSpace(eventName))
                return;

#if UNITY_EDITOR
            if (_logToConsoleInEditor)
            {
                string payload = string.Empty;
                if (parameters != null)
                {
                    for (int i = 0; i < parameters.Length; i++)
                        payload += $" {parameters[i].Key}={parameters[i].Value}";
                }
                Debug.Log($"[Analytics] {eventName}{payload}");
            }
#endif

            if (_provider != null && _provider.IsReady)
                _provider.LogEvent(eventName, parameters ?? Array.Empty<AnalyticsParam>());
        }
    }
}

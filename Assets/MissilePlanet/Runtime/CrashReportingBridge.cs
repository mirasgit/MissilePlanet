using UnityEngine;

namespace MissilePlanet
{
    /// <summary>
    /// Optional SDK seam for Crashlytics/Sentry/Backtrace/etc. The game compiles and runs
    /// without a provider. Attach a provider implementation after installing your crash SDK.
    /// </summary>
    public abstract class CrashReportingProviderBehaviour : MonoBehaviour
    {
        public abstract bool IsReady { get; }
        public abstract void Record(string message, string stackTrace, LogType type);
    }

    public sealed class CrashReportingBridge : MonoBehaviour
    {
        [SerializeField] private CrashReportingProviderBehaviour _provider;
        [SerializeField] private bool _forwardErrors = true;

        private void Awake()
        {
            if (_provider == null)
                _provider = GetComponent<CrashReportingProviderBehaviour>();
        }

        private void OnEnable()
        {
            if (_forwardErrors)
                Application.logMessageReceived += OnLogMessageReceived;
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= OnLogMessageReceived;
        }

        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (_provider == null || !_provider.IsReady)
                return;

            if (type != LogType.Exception && type != LogType.Error && type != LogType.Assert)
                return;

            _provider.Record(condition, stackTrace, type);
        }
    }
}

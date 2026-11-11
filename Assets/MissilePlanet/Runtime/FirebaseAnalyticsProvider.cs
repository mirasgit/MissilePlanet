using Firebase;
using Firebase.Analytics;
using MissilePlanet;
using UnityEngine;

public sealed class FirebaseAnalyticsProvider : AnalyticsProviderBehaviour
{
    private bool _ready;

    public override bool IsReady => _ready;

    private async void Awake()
    {
        var status = await FirebaseApp.CheckAndFixDependenciesAsync();

        if (status == DependencyStatus.Available)
            _ready = true;
        else
            Debug.LogError($"Firebase init failed: {status}");
    }

    public override void LogEvent(string eventName, AnalyticsParam[] parameters)
    {
        if (!_ready)
            return;

        var converted = new Parameter[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
            converted[i] = new Parameter(parameters[i].Key, parameters[i].Value);

        FirebaseAnalytics.LogEvent(eventName, converted);
    }
}
using System;
using Firebase.Crashlytics;
using MissilePlanet;
using UnityEngine;

public sealed class FirebaseCrashlyticsProvider : CrashReportingProviderBehaviour
{
    public override bool IsReady => true;

    private void Awake()
    {
        Crashlytics.ReportUncaughtExceptionsAsFatal = true;
    }

    public override void Record(string message, string stackTrace, LogType type)
    {
        Crashlytics.Log(message);
        Crashlytics.Log(stackTrace);

        if (type == LogType.Exception)
            Crashlytics.LogException(new Exception(message));
    }
}
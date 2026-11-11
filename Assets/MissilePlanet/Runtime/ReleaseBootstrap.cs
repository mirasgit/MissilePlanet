using UnityEngine;

namespace MissilePlanet
{
    /// <summary>Small production bootstrap kept independent from generated scenes.</summary>
    public static class ReleaseBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            GameSave.Initialize();
            GameSettings.ApplyRuntime();
            Screen.orientation = ScreenOrientation.Portrait;
        }
    }
}

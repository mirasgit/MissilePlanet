using UnityEngine;
using UnityEngine.Rendering;

namespace MissilePlanet
{
    /// <summary>
    /// Missile Planet does not use Unity's runtime Rendering Debugger UI.
    /// Device Simulator/touch gestures can accidentally invoke that overlay,
    /// which creates a Debug Canvas + EventSystem over the game and can interfere
    /// with interaction while testing. Disable it before any scene is loaded.
    /// </summary>
    internal static class RuntimeDebugUiGuard
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void DisableRuntimeRenderingDebugger()
        {
            DebugManager.instance.enableRuntimeUI = false;
        }
    }
}

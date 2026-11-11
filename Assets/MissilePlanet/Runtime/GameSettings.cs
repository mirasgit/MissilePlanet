using UnityEngine;

namespace MissilePlanet
{
    /// <summary>Persistent player-facing settings backed by GameSave.</summary>
    public static class GameSettings
    {
        public static bool SfxEnabled
        {
            get => GameSave.Data.sfxEnabled;
            set { GameSave.Data.sfxEnabled = value; GameSave.MarkDirty(true); }
        }

        public static bool MusicEnabled
        {
            get => GameSave.Data.musicEnabled;
            set { GameSave.Data.musicEnabled = value; GameSave.MarkDirty(true); }
        }

        public static bool HapticsEnabled
        {
            get => GameSave.Data.hapticsEnabled;
            set { GameSave.Data.hapticsEnabled = value; GameSave.MarkDirty(true); }
        }

        public static bool ScreenShakeEnabled
        {
            get => GameSave.Data.screenShakeEnabled;
            set { GameSave.Data.screenShakeEnabled = value; GameSave.MarkDirty(true); }
        }

        public static int TargetFps
        {
            get => GameSave.NormalizeFps(GameSave.Data.targetFps);
            set
            {
                GameSave.Data.targetFps = GameSave.NormalizeFps(value);
                GameSave.MarkDirty(true);
                ApplyRuntime();
            }
        }

        public static int CycleTargetFps()
        {
            int next = TargetFps <= 30 ? 60 : TargetFps <= 60 ? 120 : 30;
            TargetFps = next;
            return next;
        }

        public static void ApplyRuntime()
        {
            GameSave.Initialize();
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = TargetFps;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }
    }
}

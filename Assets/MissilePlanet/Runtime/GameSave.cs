using System;
using System.IO;
using UnityEngine;

namespace MissilePlanet
{
    [Serializable]
    public sealed class MissilePlanetSaveData
    {
        public int schemaVersion = 1;
        public bool legacyMigrated;

        public int bestScore;
        public int cores;

        public int planetOwnedMask = 1;
        public int rocketOwnedMask = 1;
        public int explosionOwnedMask = 1;
        public int asteroidOwnedMask = 1;
        public int spaceOwnedMask = 1;

        public int equippedPlanet;
        public int equippedRocket;
        public int equippedExplosion;
        public int equippedAsteroid;
        public int equippedSpace;

        public bool sfxEnabled = true;
        public bool musicEnabled = true;
        public bool hapticsEnabled = false;
        public bool screenShakeEnabled = true;
        public int targetFps = 60;

        public int seenTipMask;

        // Small lifetime counters are deliberately provider-agnostic. They are useful
        // for local QA and can later be mirrored to Firebase/another analytics backend.
        public int gamesStarted;
        public int bossesDefeated;
        public int highestWave;
        public int lifetimeAsteroidsDestroyed;
    }

    /// <summary>
    /// Versioned persistent save. Uses a JSON file under Application.persistentDataPath,
    /// keeps a backup, and migrates the PlayerPrefs keys used by older Missile Planet builds.
    /// Run-only state (coins, current wave/shop upgrades) intentionally does not live here.
    /// </summary>
    public static class GameSave
    {
        private const string FileName = "missileplanet_save_v1.json";
        private const string BackupFileName = "missileplanet_save_v1.backup.json";
        private const string EmergencyPrefsKey = "MissilePlanet.Save.EmergencyJson";

        private static MissilePlanetSaveData _data;
        private static bool _initialized;
        private static bool _dirty;

        public static MissilePlanetSaveData Data
        {
            get
            {
                Initialize();
                return _data;
            }
        }

        public static string SavePath => Path.Combine(Application.persistentDataPath, FileName);
        public static string BackupPath => Path.Combine(Application.persistentDataPath, BackupFileName);

        public static void Initialize()
        {
            if (_initialized)
                return;

            _initialized = true;
            _data = TryRead(SavePath) ?? TryRead(BackupPath) ?? TryReadEmergencyPrefs() ?? new MissilePlanetSaveData();
            Sanitize();
            MigrateLegacyPlayerPrefs();
            SaveNow();
        }

        public static void MarkDirty(bool saveNow = false)
        {
            Initialize();
            _dirty = true;
            if (saveNow)
                SaveNow();
        }

        public static void SetBestScore(int value, bool saveNow = false)
        {
            Initialize();
            value = Mathf.Max(0, value);
            if (value <= _data.bestScore)
                return;
            _data.bestScore = value;
            MarkDirty(saveNow);
        }

        public static void SetHighestWave(int wave, bool saveNow = false)
        {
            Initialize();
            wave = Mathf.Max(0, wave);
            if (wave <= _data.highestWave)
                return;
            _data.highestWave = wave;
            MarkDirty(saveNow);
        }

        public static bool HasSeenTip(int bitIndex)
        {
            Initialize();
            if (bitIndex < 0 || bitIndex > 30) return true;
            return (_data.seenTipMask & (1 << bitIndex)) != 0;
        }

        public static void MarkTipSeen(int bitIndex)
        {
            Initialize();
            if (bitIndex < 0 || bitIndex > 30) return;
            int mask = 1 << bitIndex;
            if ((_data.seenTipMask & mask) != 0) return;
            _data.seenTipMask |= mask;
            MarkDirty();
        }

        public static void SaveNow()
        {
            InitializeWithoutRecursion();
            if (_data == null)
                return;

            Sanitize();
            string json = JsonUtility.ToJson(_data, true);
            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                string temp = SavePath + ".tmp";
                File.WriteAllText(temp, json);

                if (File.Exists(SavePath))
                    File.Copy(SavePath, BackupPath, true);

                File.Copy(temp, SavePath, true);
                File.Delete(temp);
                PlayerPrefs.SetString(EmergencyPrefsKey, json);
                PlayerPrefs.Save();
                _dirty = false;
            }
            catch (Exception ex)
            {
                // PlayerPrefs is retained only as an emergency fallback if the device/filesystem
                // refuses file writes. Gameplay continues rather than corrupting the session.
                Debug.LogWarning($"Missile Planet save write failed: {ex.Message}");
                try
                {
                    PlayerPrefs.SetString(EmergencyPrefsKey, json);
                    PlayerPrefs.Save();
                }
                catch { }
            }
        }

        public static void FlushIfDirty()
        {
            if (_dirty)
                SaveNow();
        }

        private static void InitializeWithoutRecursion()
        {
            if (_initialized)
                return;
            _initialized = true;
            _data = new MissilePlanetSaveData();
        }

        private static MissilePlanetSaveData TryRead(string path)
        {
            try
            {
                if (!File.Exists(path))
                    return null;
                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                    return null;
                return JsonUtility.FromJson<MissilePlanetSaveData>(json);
            }
            catch
            {
                return null;
            }
        }

        private static MissilePlanetSaveData TryReadEmergencyPrefs()
        {
            try
            {
                string json = PlayerPrefs.GetString(EmergencyPrefsKey, string.Empty);
                if (string.IsNullOrWhiteSpace(json))
                    return null;
                return JsonUtility.FromJson<MissilePlanetSaveData>(json);
            }
            catch
            {
                return null;
            }
        }

        private static void Sanitize()
        {
            if (_data == null)
                _data = new MissilePlanetSaveData();

            _data.schemaVersion = Mathf.Max(1, _data.schemaVersion);
            _data.bestScore = Mathf.Max(0, _data.bestScore);
            _data.cores = Mathf.Max(0, _data.cores);
            _data.planetOwnedMask |= 1;
            _data.rocketOwnedMask |= 1;
            _data.explosionOwnedMask |= 1;
            _data.asteroidOwnedMask |= 1;
            _data.spaceOwnedMask |= 1;

            _data.equippedPlanet = ClampOwnedSelection(_data.equippedPlanet, _data.planetOwnedMask, 3);
            _data.equippedRocket = ClampOwnedSelection(_data.equippedRocket, _data.rocketOwnedMask, 3);
            _data.equippedExplosion = ClampOwnedSelection(_data.equippedExplosion, _data.explosionOwnedMask, 3);
            _data.equippedAsteroid = ClampOwnedSelection(_data.equippedAsteroid, _data.asteroidOwnedMask, 1);
            _data.equippedSpace = ClampOwnedSelection(_data.equippedSpace, _data.spaceOwnedMask, 3);

            _data.targetFps = NormalizeFps(_data.targetFps);
            _data.gamesStarted = Mathf.Max(0, _data.gamesStarted);
            _data.bossesDefeated = Mathf.Max(0, _data.bossesDefeated);
            _data.highestWave = Mathf.Max(0, _data.highestWave);
            _data.lifetimeAsteroidsDestroyed = Mathf.Max(0, _data.lifetimeAsteroidsDestroyed);
        }

        private static int ClampOwnedSelection(int value, int ownedMask, int maxValue)
        {
            if (value < 0 || value > maxValue) return 0;
            if ((ownedMask & (1 << value)) == 0) return 0;
            return value;
        }

        public static int NormalizeFps(int value)
        {
            if (value <= 30) return 30;
            if (value >= 120) return 120;
            return 60;
        }

        private static void MigrateLegacyPlayerPrefs()
        {
            if (_data.legacyMigrated)
                return;

            _data.bestScore = Mathf.Max(_data.bestScore, PlayerPrefs.GetInt("MissilePlanet.BestScore", 0));
            _data.cores = Mathf.Max(_data.cores, PlayerPrefs.GetInt("MissilePlanet.Meta.Cores", 0));

            MigrateOwned("MissilePlanet.Cosmetic.Mars.Owned", 1, ref _data.planetOwnedMask);
            MigrateOwned("MissilePlanet.Cosmetic.Venus.Owned", 2, ref _data.planetOwnedMask);
            MigrateOwned("MissilePlanet.Cosmetic.Neptune.Owned", 3, ref _data.planetOwnedMask);

            MigrateOwned("MissilePlanet.Cosmetic.Orbs.Owned", 1, ref _data.rocketOwnedMask);
            MigrateOwned("MissilePlanet.Cosmetic.SolarRocket.Owned", 2, ref _data.rocketOwnedMask);
            MigrateOwned("MissilePlanet.Cosmetic.CryoRocket.Owned", 3, ref _data.rocketOwnedMask);

            MigrateOwned("MissilePlanet.Cosmetic.VioletExplosion.Owned", 1, ref _data.explosionOwnedMask);
            MigrateOwned("MissilePlanet.Cosmetic.SolarExplosion.Owned", 2, ref _data.explosionOwnedMask);
            MigrateOwned("MissilePlanet.Cosmetic.EmeraldExplosion.Owned", 3, ref _data.explosionOwnedMask);

            MigrateOwned("MissilePlanet.Cosmetic.Kamikaze.Owned", 1, ref _data.asteroidOwnedMask);

            MigrateOwned("MissilePlanet.Cosmetic.PurpleSpace.Owned", 1, ref _data.spaceOwnedMask);
            MigrateOwned("MissilePlanet.Cosmetic.CrimsonSpace.Owned", 2, ref _data.spaceOwnedMask);
            MigrateOwned("MissilePlanet.Cosmetic.FrozenSpace.Owned", 3, ref _data.spaceOwnedMask);

            _data.equippedPlanet = PlayerPrefs.GetInt("MissilePlanet.Cosmetic.Planet.Equipped", _data.equippedPlanet);
            _data.equippedRocket = PlayerPrefs.GetInt("MissilePlanet.Cosmetic.Rocket.Equipped", _data.equippedRocket);
            _data.equippedExplosion = PlayerPrefs.GetInt("MissilePlanet.Cosmetic.Explosion.Equipped", _data.equippedExplosion);
            _data.equippedAsteroid = PlayerPrefs.GetInt("MissilePlanet.Cosmetic.Asteroid.Equipped", _data.equippedAsteroid);
            _data.equippedSpace = PlayerPrefs.GetInt("MissilePlanet.Cosmetic.Space.Equipped", _data.equippedSpace);

            _data.legacyMigrated = true;
            _dirty = true;
        }

        private static void MigrateOwned(string key, int enumValue, ref int mask)
        {
            if (PlayerPrefs.GetInt(key, 0) == 1)
                mask |= 1 << enumValue;
        }
    }
}

using UnityEngine;

namespace MissilePlanet
{
    public enum PlanetCosmetic { Earth = 0, Mars = 1, Venus = 2, Neptune = 3 }
    // InvokerOrbs is kept as the internal enum name for backwards save compatibility.
    // The release-facing UI uses the original name "Tri-Arcana" instead of third-party IP names.
    public enum RocketCosmetic { Classic = 0, InvokerOrbs = 1, SolarLance = 2, CryoPulse = 3 }
    public enum ExplosionCosmetic { Classic = 0, VioletNova = 1, SolarBurst = 2, EmeraldShock = 3 }
    public enum AsteroidCosmetic { Classic = 0, KamikazeShips = 1 }
    public enum SpaceCosmetic { Classic = 0, PurpleNebula = 1, CrimsonVoid = 2, FrozenNight = 3 }

    /// <summary>
    /// Persistent meta currency + cosmetic ownership. Run coins intentionally do not live here.
    /// Storage is versioned JSON through GameSave; old PlayerPrefs are migrated automatically once.
    /// </summary>
    public static class MetaProgression
    {
        public static int Cores => Mathf.Max(0, GameSave.Data.cores);
        public static PlanetCosmetic Planet => SafeEnum(GameSave.Data.equippedPlanet, PlanetCosmetic.Earth, 3);
        public static RocketCosmetic Rocket => SafeEnum(GameSave.Data.equippedRocket, RocketCosmetic.Classic, 3);
        public static ExplosionCosmetic Explosion => SafeEnum(GameSave.Data.equippedExplosion, ExplosionCosmetic.Classic, 3);
        public static AsteroidCosmetic Asteroid => SafeEnum(GameSave.Data.equippedAsteroid, AsteroidCosmetic.Classic, 1);
        public static SpaceCosmetic Space => SafeEnum(GameSave.Data.equippedSpace, SpaceCosmetic.Classic, 3);

        public static void AddCores(int amount)
        {
            if (amount <= 0) return;
            GameSave.Data.cores = Mathf.Max(0, GameSave.Data.cores + amount);
            GameSave.MarkDirty(true);
        }

        public static bool TrySpendCores(int amount)
        {
            amount = Mathf.Max(0, amount);
            if (Cores < amount) return false;
            GameSave.Data.cores -= amount;
            GameSave.MarkDirty(true);
            return true;
        }

        public static bool IsOwned(PlanetCosmetic item) => IsOwnedMask(GameSave.Data.planetOwnedMask, (int)item);
        public static bool IsOwned(RocketCosmetic item) => IsOwnedMask(GameSave.Data.rocketOwnedMask, (int)item);
        public static bool IsOwned(ExplosionCosmetic item) => IsOwnedMask(GameSave.Data.explosionOwnedMask, (int)item);
        public static bool IsOwned(AsteroidCosmetic item) => IsOwnedMask(GameSave.Data.asteroidOwnedMask, (int)item);
        public static bool IsOwned(SpaceCosmetic item) => IsOwnedMask(GameSave.Data.spaceOwnedMask, (int)item);

        public static bool BuyAndEquip(PlanetCosmetic item, int cost)
        {
            if (!EnsureOwned(GameSave.Data.planetOwnedMask, (int)item, cost, out int ownedMask)) return false;
            GameSave.Data.planetOwnedMask = ownedMask;
            GameSave.Data.equippedPlanet = (int)item;
            GameSave.MarkDirty(true);
            return true;
        }

        public static bool BuyAndEquip(RocketCosmetic item, int cost)
        {
            if (!EnsureOwned(GameSave.Data.rocketOwnedMask, (int)item, cost, out int ownedMask)) return false;
            GameSave.Data.rocketOwnedMask = ownedMask;
            GameSave.Data.equippedRocket = (int)item;
            GameSave.MarkDirty(true);
            return true;
        }

        public static bool BuyAndEquip(ExplosionCosmetic item, int cost)
        {
            if (!EnsureOwned(GameSave.Data.explosionOwnedMask, (int)item, cost, out int ownedMask)) return false;
            GameSave.Data.explosionOwnedMask = ownedMask;
            GameSave.Data.equippedExplosion = (int)item;
            GameSave.MarkDirty(true);
            return true;
        }

        public static bool BuyAndEquip(AsteroidCosmetic item, int cost)
        {
            if (!EnsureOwned(GameSave.Data.asteroidOwnedMask, (int)item, cost, out int ownedMask)) return false;
            GameSave.Data.asteroidOwnedMask = ownedMask;
            GameSave.Data.equippedAsteroid = (int)item;
            GameSave.MarkDirty(true);
            return true;
        }

        public static bool BuyAndEquip(SpaceCosmetic item, int cost)
        {
            if (!EnsureOwned(GameSave.Data.spaceOwnedMask, (int)item, cost, out int ownedMask)) return false;
            GameSave.Data.spaceOwnedMask = ownedMask;
            GameSave.Data.equippedSpace = (int)item;
            GameSave.MarkDirty(true);
            return true;
        }

        public static bool SetPlanet(PlanetCosmetic item)
        {
            if (!IsOwned(item)) return false;
            GameSave.Data.equippedPlanet = (int)item;
            GameSave.MarkDirty(true);
            return true;
        }

        public static bool SetRocket(RocketCosmetic item)
        {
            if (!IsOwned(item)) return false;
            GameSave.Data.equippedRocket = (int)item;
            GameSave.MarkDirty(true);
            return true;
        }

        public static bool SetExplosion(ExplosionCosmetic item)
        {
            if (!IsOwned(item)) return false;
            GameSave.Data.equippedExplosion = (int)item;
            GameSave.MarkDirty(true);
            return true;
        }

        public static bool SetAsteroid(AsteroidCosmetic item)
        {
            if (!IsOwned(item)) return false;
            GameSave.Data.equippedAsteroid = (int)item;
            GameSave.MarkDirty(true);
            return true;
        }

        public static bool SetSpace(SpaceCosmetic item)
        {
            if (!IsOwned(item)) return false;
            GameSave.Data.equippedSpace = (int)item;
            GameSave.MarkDirty(true);
            return true;
        }

        public static void EquipDefaults()
        {
            GameSave.Data.equippedPlanet = (int)PlanetCosmetic.Earth;
            GameSave.Data.equippedRocket = (int)RocketCosmetic.Classic;
            GameSave.Data.equippedExplosion = (int)ExplosionCosmetic.Classic;
            GameSave.Data.equippedAsteroid = (int)AsteroidCosmetic.Classic;
            GameSave.Data.equippedSpace = (int)SpaceCosmetic.Classic;
            GameSave.MarkDirty(true);
        }

        private static bool EnsureOwned(int mask, int value, int cost, out int updatedMask)
        {
            updatedMask = mask;
            if (IsOwnedMask(mask, value)) return true;
            if (!TrySpendCores(cost)) return false;
            updatedMask = mask | (1 << Mathf.Clamp(value, 0, 30));
            return true;
        }

        private static bool IsOwnedMask(int mask, int value)
        {
            if (value < 0 || value > 30) return false;
            return (mask & (1 << value)) != 0;
        }

        private static T SafeEnum<T>(int value, T fallback, int maxValue) where T : struct
        {
            if (value < 0 || value > maxValue)
                return fallback;
            return (T)System.Enum.ToObject(typeof(T), value);
        }
    }
}

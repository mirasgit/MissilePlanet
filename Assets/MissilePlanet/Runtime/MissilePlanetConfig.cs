using UnityEngine;

namespace MissilePlanet
{
    [CreateAssetMenu(fileName = "MissilePlanetConfig", menuName = "Missile Planet/Config")]
    public sealed class MissilePlanetConfig : ScriptableObject
    {
        [Header("Planet")]
        [Min(1f)] public float PlanetRadius = 1.8f;
        public float PlanetRotationSpeed = 7f;

        [Header("Waves")]
        [Min(1)] public int StartingAsteroids = 7;
        [Min(0)] public int AsteroidsPerWave = 3;
        [Min(0.05f)] public float SpawnInterval = 0.85f;
        [Min(0f)] public float InterWaveDelay = 2.2f;

        [Header("Asteroids")]
        [Min(0.1f)] public float AsteroidBaseSpeed = 2.35f;
        [Min(0f)] public float AsteroidSpeedPerWave = 0.12f;
        [Min(1f)] public float AsteroidTurnSpeedDegrees = 125f;
        [Min(0.05f)] public float AsteroidImpactDistance = 0.36f;
        [Min(0.01f)] public float AsteroidCollisionRadius = 0.14f;
        [Min(0.1f)] public float AsteroidTargetHitRadius = 0.7f;
        [Min(0f)] public float AsteroidSpawnTopMargin = 0.9f;
        [Range(0f, 0.45f)] public float AsteroidSpawnHorizontalPadding = 0.06f;

        [Header("Red Splitters")]
        [Min(1)] public int RedAsteroidUnlockAfterWave = 5;
        [Range(0f, 1f)] public float RedAsteroidChance = 0.20f;
        [Min(1f)] public float RedAsteroidScaleMultiplier = 1.4f;
        [Min(1)] public int RedSplitCountBeforeUpgrade = 2;
        [Min(1)] public int RedSplitCountAfterUpgrade = 3;
        [Min(1)] public int RedUpgradeAfterWave = 15;
        [Min(0.1f)] public float RedFragmentSpeedMultiplier = 1.08f;
        [Range(0.2f, 0.8f)] public float RedSplitTravelFraction = 0.50f;
        [Min(0.05f)] public float RedBlinkSlowInterval = 0.55f;
        [Min(0.03f)] public float RedBlinkFastInterval = 0.08f;

        [Header("Green Armored")]
        [Min(1)] public int GreenAsteroidUnlockAfterWave = 10;
        [Range(0f, 1f)] public float GreenAsteroidChance = 0.10f;
        [Range(0.2f, 1f)] public float GreenAsteroidSpeedMultiplier = 0.72f;
        [Min(2)] public int GreenHitsBeforeUpgrade = 2;
        [Min(2)] public int GreenHitsAfterUpgrade = 3;
        [Min(1)] public int GreenUpgradeAfterWave = 15;

        [Header("Yellow Dodgers")]
        [Min(1)] public int YellowAsteroidUnlockAfterWave = 20;
        [Min(1)] public int YellowGuaranteedWave = 21;
        [Range(0f, 1f)] public float YellowReplaceGreenChance = 0.30f;
        [Min(1f)] public float YellowAsteroidDashSpeed = 14.5f;
        [Min(1)] public int YellowMinManeuvers = 4;
        [Min(1)] public int YellowMaxManeuvers = 5;
        [Min(0.05f)] public float YellowWaypointReachDistance = 0.32f;
        [Min(0f)] public int YellowAsteroidScore = 350;

        [Header("Pulse Carriers")]
        [Min(1)] public int PulseCarrierWaveInterval = 5;
        [Min(1f)] public float PulseCarrierSpeedMultiplier = 1.8f;
        [Min(0)] public int PulseCarrierScore = 250;
        [Min(0)] public int StartingPulseCharges = 1;
        [Min(1)] public int MaxPulseCharges = 3;

        [Header("Bosses")]
        [Min(1)] public int BossWaveInterval = 10;
        [Min(0.1f)] public float BossFireRateIncreasePerDestroyedEngine = 0.30f;
        [Min(0f)] public float BossReloadRateIncreasePerDestroyedEngine = 0.14f;
        [Min(0f)] public float BossTierFireRateIncrease = 0.16f;
        [Min(0f)] public float BossTierReloadRateIncrease = 0.10f;
        [Min(0)] public int BossEngineScore = 1000;
        [Min(0)] public int BossDefeatScore = 5000;
        [Min(0f)] public float BossSpawnY = 4.6f;
        [Min(0f)] public float BossMoveAmplitude = 1.25f;
        [Min(0.1f)] public float BossMoveAngularSpeed = 0.85f;
        [Min(0.5f)] public float BossCoreBarrageInterval = 4.2f;
        [Min(0.1f)] public float BossCoreTelegraphDuration = 0.85f;
        [Min(1)] public int BossCoreBaseProjectiles = 3;
        [Min(0.1f)] public float BossCoreProjectileSpeedMultiplier = 1.18f;
        [Min(1)] public int BossEngineBaseHitPoints = 2;
        [Min(0)] public int BossEngineHitPointsPerTwoTiers = 1;
        [Min(0.05f)] public float BossEngineHitCooldown = 0.18f;
        [Min(0)] public int BossRetaliationProjectiles = 3;
        [Min(0.1f)] public float BossRetaliationSpeedMultiplier = 1.30f;
        [Min(0f)] public float BossEngineGlobalLockoutDuration = 5f;

        [Header("Missiles")]
        [Min(1)] public int MissilesPerWave = 18;
        [Min(0)] public int MissileBonusPerWave = 1;
        [Min(0.1f)] public float MissileSpeed = 12f;

        [Header("Run Economy / Boss Shop")]
        [Min(1)] public int ShopMissilesPerUpgrade = 3;
        [Min(0)] public int ShopMissileUpgradeBaseCost = 25;
        [Min(0)] public int ShopMissileUpgradeCostStep = 15;
        [Min(1)] public int ShopMissileUpgradeMaxLevel = 5;
        [Min(0)] public int ShopPowerUpgradeBaseCost = 50;
        [Min(2)] public int ShopPowerUpgradeCostMultiplier = 5;
        [Min(1)] public int ShopPowerUpgradeMaxLevel = 4;
        [Min(0.01f)] public float ShopPowerRadiusBonusPerLevel = 0.18f;
        [Min(0.005f)] public float ShopPowerDurationBonusPerLevel = 0.04f;
        [Min(0)] public int ShopShieldChargeCost = 35;
        [Min(0)] public int ShopGuardianCost = 120;
        [Min(0)] public int ShopGuardianCostStep = 140;
        [Min(1)] public int ShopGuardianMaxCount = 5;
        [Min(0)] public int ShopCannonCost = 90;
        [Min(0)] public int ShopCannonCostStep = 260;
        [Min(1)] public int ShopCannonMaxCount = 3;
        [Min(0)] public int CoinPerNormalAsteroid = 1;
        [Min(0)] public int CoinPerRedAsteroid = 2;
        [Min(0)] public int CoinPerGreenAsteroid = 3;
        [Min(0)] public int CoinPerYellowAsteroid = 4;
        [Min(0)] public int CoinPerCarrier = 5;
        [Min(0)] public int CoinPerBossEngine = 4;
        [Min(0)] public int CoinPerBossDefeat = 10;
        [Min(0)] public int CoinPerSurvivingCity = 1;

        [Header("Meta Economy")]
        [Min(1)] public int BossCoreReward = 1;
        [Min(1)] public int AdCoreReward = 1;
        [Min(1)] public int ReviveBaseCoreCost = 1;
        [Min(2)] public int ReviveCoreMultiplier = 2;
        [Min(0)] public int MarsSkinCost = 6;
        [Min(0)] public int VenusSkinCost = 7;
        [Min(0)] public int NeptuneSkinCost = 9;
        [Min(0)] public int InvokerRocketPackCost = 8;
        [Min(0)] public int SolarRocketThemeCost = 6;
        [Min(0)] public int CryoRocketThemeCost = 7;
        [Min(0)] public int VioletExplosionCost = 4;
        [Min(0)] public int SolarExplosionCost = 5;
        [Min(0)] public int EmeraldExplosionCost = 5;
        [Min(0)] public int KamikazeAsteroidCost = 7;
        [Min(0)] public int PurpleSpaceCost = 5;
        [Min(0)] public int CrimsonSpaceCost = 6;
        [Min(0)] public int FrozenSpaceCost = 6;

        [Header("Orbital Helpers")]
        [Min(0.1f)] public float GuardianOrbitRadius = 2.45f;
        public float GuardianAngularSpeed = 24f;
        [Min(0.05f)] public float GuardianCollisionRadius = 0.42f;
        [Min(0.1f)] public float CannonOrbitRadius = 2.85f;
        public float CannonAngularSpeed = -31f;
        [Min(0.05f)] public float CannonCollisionRadius = 0.28f;
        [Min(0.2f)] public float CannonFireInterval = 2.2f;
        [Min(0.1f)] public float CannonMissileSpeed = 15.5f;
        [Min(0.1f)] public float CannonExplosionRadius = 0.58f;
        [Min(0.05f)] public float CannonExplosionDuration = 0.36f;

        [Header("Explosion")]
        [Min(0.1f)] public float ExplosionRadius = 1.2f;
        [Min(0.05f)] public float ExplosionDuration = 0.8f;
        [Min(0.1f)] public float ChainExplosionRadius = 0.72f;
        [Min(0.05f)] public float ChainExplosionDuration = 0.48f;
        [Min(0.1f)] public float ImpactExplosionRadius = 0.5f;
        [Min(0.05f)] public float ImpactExplosionDuration = 0.35f;

        [Header("Planet Pulse")]
        [Min(0.1f)] public float PlanetPulseDuration = 1.15f;
        [Min(0f)] public float PlanetPulseScreenPadding = 1.2f;

        [Header("Scoring")]
        [Min(0)] public int ScorePerAsteroid = 100;
        [Min(0)] public int RedAsteroidScore = 175;
        [Min(0)] public int GreenAsteroidScore = 225;

        [Header("Camera shake")]
        [Min(0f)] public float ImpactShakeDuration = 0.16f;
        [Min(0f)] public float ImpactShakeMagnitude = 0.12f;
        [Min(0f)] public float PlanetPulseShakeDuration = 0.2f;
        [Min(0f)] public float PlanetPulseShakeMagnitude = 0.08f;
        [Min(0f)] public float BossEngineShakeDuration = 0.22f;
        [Min(0f)] public float BossEngineShakeMagnitude = 0.12f;
    }
}

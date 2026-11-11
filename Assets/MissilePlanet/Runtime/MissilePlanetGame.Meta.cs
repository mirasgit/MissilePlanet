using System;
using System.Collections;
using UnityEngine;

namespace MissilePlanet
{
    public sealed partial class MissilePlanetGame
    {
        private MonetizationBridge _monetization;
        private int _coreReviveCount;
        private bool _adReviveUsed;
        private int _missileCosmeticCycle;

        public int Cores => MetaProgression.Cores;
        public bool RewardedAdAvailable => _monetization != null && _monetization.RewardedAvailable;
        public bool CorePurchasesAvailable => _monetization != null && _monetization.PurchasesAvailable;
        public bool CanUseAdRevive => IsGameOver && !_adReviveUsed && RewardedAdAvailable;
        public int CurrentCoreReviveCost
        {
            get
            {
                int baseCost = _config != null ? Mathf.Max(1, _config.ReviveBaseCoreCost) : 1;
                int multiplier = _config != null ? Mathf.Max(2, _config.ReviveCoreMultiplier) : 2;
                int result = baseCost;
                for (int i = 0; i < _coreReviveCount; i++)
                    result = Mathf.Min(9999, result * multiplier);
                return result;
            }
        }

        public bool UseKamikazeAsteroids => MetaProgression.Asteroid == AsteroidCosmetic.KamikazeShips;
        public PlanetCosmetic EquippedPlanet => MetaProgression.Planet;
        public RocketCosmetic EquippedRocket => MetaProgression.Rocket;
        public ExplosionCosmetic EquippedExplosion => MetaProgression.Explosion;
        public SpaceCosmetic EquippedSpace => MetaProgression.Space;

        public bool MarsEquipped => EquippedPlanet == PlanetCosmetic.Mars;
        public bool VenusEquipped => EquippedPlanet == PlanetCosmetic.Venus;
        public bool NeptuneEquipped => EquippedPlanet == PlanetCosmetic.Neptune;
        public bool InvokerRocketsEquipped => EquippedRocket == RocketCosmetic.InvokerOrbs;
        public bool SolarRocketsEquipped => EquippedRocket == RocketCosmetic.SolarLance;
        public bool CryoRocketsEquipped => EquippedRocket == RocketCosmetic.CryoPulse;
        public bool VioletExplosionEquipped => EquippedExplosion == ExplosionCosmetic.VioletNova;
        public bool SolarExplosionEquipped => EquippedExplosion == ExplosionCosmetic.SolarBurst;
        public bool EmeraldExplosionEquipped => EquippedExplosion == ExplosionCosmetic.EmeraldShock;
        public bool PurpleSpaceEquipped => EquippedSpace == SpaceCosmetic.PurpleNebula;
        public bool CrimsonSpaceEquipped => EquippedSpace == SpaceCosmetic.CrimsonVoid;
        public bool FrozenSpaceEquipped => EquippedSpace == SpaceCosmetic.FrozenNight;

        private void InitializeMetaSystems()
        {
            _monetization = GetComponent<MonetizationBridge>();
            if (_monetization == null)
                _monetization = gameObject.AddComponent<MonetizationBridge>();

            _analytics = GetComponent<AnalyticsBridge>();
            if (_analytics == null)
                _analytics = gameObject.AddComponent<AnalyticsBridge>();

            _haptics = GetComponent<HapticsBridge>();
            if (_haptics == null)
                _haptics = gameObject.AddComponent<HapticsBridge>();

            _musicPlayer = UnityEngine.Object.FindFirstObjectByType<MusicPlayer>();
            _musicPlayer?.SetMode(MusicMode.Menu, true);
            _analytics.Track("session_start",
                new AnalyticsParam("best_score", BestScore),
                new AnalyticsParam("cores", Cores),
                new AnalyticsParam("highest_wave", GameSave.Data.highestWave));
        }

        private void ResetRunMetaState()
        {
            _coreReviveCount = 0;
            _adReviveUsed = false;
            _missileCosmeticCycle = 0;
        }

        private void AddCoins(int amount)
        {
            _shopPoints += Mathf.Max(0, amount);
        }

        private int GetCoinRewardForAsteroid(AsteroidVariant variant)
        {
            if (_config == null) return 0;
            if (variant == AsteroidVariant.RedSplitter) return _config.CoinPerRedAsteroid;
            if (variant == AsteroidVariant.GreenArmored) return _config.CoinPerGreenAsteroid;
            if (variant == AsteroidVariant.YellowDodger) return _config.CoinPerYellowAsteroid;
            if (variant == AsteroidVariant.PulseCarrier) return _config.CoinPerCarrier;
            return _config.CoinPerNormalAsteroid;
        }

        private void AwardEndOfWaveCityCoins()
        {
            if (_config == null || _config.CoinPerSurvivingCity <= 0)
                return;
            AddCoins(CountAlive(PlanetTargetType.City) * _config.CoinPerSurvivingCity);
        }

        public void RequestRewardedCore()
        {
            if (!RewardedAdAvailable) return;
            _monetization?.ShowRewarded(
                "main_menu_core",
                () =>
                {
                    int amount = _config != null ? _config.AdCoreReward : 1;
                    MetaProgression.AddCores(amount);
                    _analytics?.Track("rewarded_complete", new AnalyticsParam("placement", "main_menu_core"), new AnalyticsParam("reward", amount));
                });
        }

        public void RequestAdRevive()
        {
            if (!CanUseAdRevive)
                return;

            _monetization?.ShowRewarded(
                "game_over_revive",
                () =>
                {
                    _adReviveUsed = true;
                    _analytics?.Track("revive", new AnalyticsParam("method", "rewarded"), new AnalyticsParam("wave", Wave));
                    PerformRevive();
                });
        }

        public bool TryCoreRevive()
        {
            if (!IsGameOver)
                return false;

            int cost = CurrentCoreReviveCost;
            if (!MetaProgression.TrySpendCores(cost))
                return false;

            _coreReviveCount++;
            _analytics?.Track("revive", new AnalyticsParam("method", "cores"), new AnalyticsParam("cost", cost), new AnalyticsParam("wave", Wave));
            PerformRevive();
            return true;
        }

        private void PerformRevive()
        {
            if (!IsGameOver || _config == null)
                return;

            Time.timeScale = 1f;
            AudioListener.pause = false;
            _paused = false;
            _shopOpen = false;
            _bossDefeatInProgress = false;

            ClearDebugCombatants();
            ReviveCriticalSurfaceTargets();
            RespawnOrbitalHelpers();

            _playing = true;
            Ammo = GetWaveAmmo();
            SetPulseShieldVisible(_planetPulseCharges > 0);

            // GameLoop increments first, so step back once to replay the failed wave.
            Wave = Mathf.Max(0, Wave - 1);
            _gameLoop = StartCoroutine(GameLoop());
        }

        private void ReviveCriticalSurfaceTargets()
        {
            if (CountAlive(PlanetTargetType.City) == 0)
                ReviveOneTarget(PlanetTargetType.City);
            if (CountAlive(PlanetTargetType.Launcher) == 0)
                ReviveOneTarget(PlanetTargetType.Launcher);
        }

        private void ReviveOneTarget(PlanetTargetType type)
        {
            if (_targets == null) return;
            for (int i = 0; i < _targets.Length; i++)
            {
                PlanetTarget target = _targets[i];
                if (target != null && target.TargetType == type && !target.IsAlive)
                {
                    target.Revive();
                    return;
                }
            }
        }

        public Color GetNextMissileCosmeticColor()
        {
            int phase = _missileCosmeticCycle++;
            switch (EquippedRocket)
            {
                case RocketCosmetic.InvokerOrbs:
                    switch (phase % 3)
                    {
                        case 0: return new Color(0.30f, 0.86f, 1f, 1f);
                        case 1: return new Color(0.62f, 0.30f, 1f, 1f);
                        default: return new Color(1f, 0.34f, 0.08f, 1f);
                    }
                case RocketCosmetic.SolarLance:
                    switch (phase % 3)
                    {
                        case 0: return new Color(1f, 0.82f, 0.18f, 1f);
                        case 1: return new Color(1f, 0.42f, 0.06f, 1f);
                        default: return new Color(1f, 0.96f, 0.72f, 1f);
                    }
                case RocketCosmetic.CryoPulse:
                    switch (phase % 3)
                    {
                        case 0: return new Color(0.30f, 0.95f, 1f, 1f);
                        case 1: return new Color(0.34f, 0.56f, 1f, 1f);
                        default: return new Color(0.86f, 0.98f, 1f, 1f);
                    }
                default:
                    return new Color(0.72f, 0.95f, 1f, 1f);
            }
        }

        private Color GetPlayerExplosionColor(float alpha)
        {
            switch (EquippedExplosion)
            {
                case ExplosionCosmetic.VioletNova: return new Color(0.72f, 0.24f, 1f, alpha);
                case ExplosionCosmetic.SolarBurst: return new Color(1f, 0.58f, 0.08f, alpha);
                case ExplosionCosmetic.EmeraldShock: return new Color(0.20f, 1f, 0.48f, alpha);
                default: return new Color(0.2f, 0.85f, 1f, alpha);
            }
        }

        public void ApplyCosmeticsVisuals()
        {
            ApplyPlanetCosmetic();
            ApplySpaceCosmetic();
        }

        private void ApplyPlanetCosmetic()
        {
            if (_planet == null) return;

            Color discColor;
            Color atmosphereColor;
            Color landColor;
            switch (EquippedPlanet)
            {
                case PlanetCosmetic.Mars:
                    discColor = new Color(0.58f, 0.17f, 0.08f, 1f);
                    atmosphereColor = new Color(1f, 0.32f, 0.12f, 0.13f);
                    landColor = new Color(0.32f, 0.075f, 0.045f, 0.92f);
                    break;
                case PlanetCosmetic.Venus:
                    discColor = new Color(0.72f, 0.46f, 0.16f, 1f);
                    atmosphereColor = new Color(1f, 0.82f, 0.35f, 0.18f);
                    landColor = new Color(0.48f, 0.25f, 0.08f, 0.92f);
                    break;
                case PlanetCosmetic.Neptune:
                    discColor = new Color(0.08f, 0.20f, 0.72f, 1f);
                    atmosphereColor = new Color(0.20f, 0.72f, 1f, 0.20f);
                    landColor = new Color(0.10f, 0.38f, 0.86f, 0.92f);
                    break;
                default:
                    discColor = new Color(0.055f, 0.23f, 0.46f, 1f);
                    atmosphereColor = new Color(0.14f, 0.62f, 1f, 0.18f);
                    landColor = new Color(0.08f, 0.55f, 0.39f, 0.95f);
                    break;
            }

            Transform disc = _planet.Find("PlanetDisc");
            if (disc != null && disc.TryGetComponent(out SpriteRenderer discRenderer))
                discRenderer.color = discColor;

            Transform atmosphere = _planet.Find("Atmosphere");
            if (atmosphere != null && atmosphere.TryGetComponent(out SpriteRenderer atmRenderer))
                atmRenderer.color = atmosphereColor;

            for (int i = 1; i <= 7; i++)
            {
                Transform continent = _planet.Find($"Continent_{i}");
                if (continent != null && continent.TryGetComponent(out SpriteRenderer r))
                    r.color = landColor;
            }
        }

        private void ApplySpaceCosmetic()
        {
            Color background;
            Color starA;
            Color starB;
            switch (EquippedSpace)
            {
                case SpaceCosmetic.PurpleNebula:
                    background = new Color(0.045f, 0.008f, 0.075f, 1f);
                    starA = new Color(0.20f, 0.72f, 1f, 0.65f);
                    starB = new Color(0.52f, 0.92f, 1f, 1f);
                    break;
                case SpaceCosmetic.CrimsonVoid:
                    background = new Color(0.075f, 0.006f, 0.018f, 1f);
                    starA = new Color(1f, 0.30f, 0.22f, 0.62f);
                    starB = new Color(1f, 0.72f, 0.32f, 1f);
                    break;
                case SpaceCosmetic.FrozenNight:
                    background = new Color(0.004f, 0.032f, 0.070f, 1f);
                    starA = new Color(0.24f, 0.88f, 1f, 0.62f);
                    starB = new Color(0.78f, 0.98f, 1f, 1f);
                    break;
                default:
                    background = new Color(0.008f, 0.012f, 0.035f, 1f);
                    starA = new Color(0.68f, 0.82f, 1f, 0.65f);
                    starB = new Color(0.95f, 0.98f, 1f, 1f);
                    break;
            }

            if (_camera != null)
                _camera.backgroundColor = background;

            GameObject starsGo = GameObject.Find("Stars");
            ParticleSystem stars = starsGo != null ? starsGo.GetComponent<ParticleSystem>() : null;
            if (stars == null) return;

            ParticleSystem.MainModule main = stars.main;
            main.startColor = new ParticleSystem.MinMaxGradient(starA, starB);
            stars.Clear();
            stars.Play();
        }

        private bool BuyOrTogglePlanet(PlanetCosmetic cosmetic, int cost)
        {
            bool ok = !MetaProgression.IsOwned(cosmetic)
                ? MetaProgression.BuyAndEquip(cosmetic, cost)
                : MetaProgression.SetPlanet(EquippedPlanet == cosmetic ? PlanetCosmetic.Earth : cosmetic);
            if (ok)
            {
                ApplyCosmeticsVisuals();
                _analytics?.Track("cosmetic_selected", new AnalyticsParam("category", "planet"), new AnalyticsParam("item", cosmetic));
            }
            return ok;
        }

        private bool BuyOrToggleRocket(RocketCosmetic cosmetic, int cost)
        {
            bool ok = !MetaProgression.IsOwned(cosmetic)
                ? MetaProgression.BuyAndEquip(cosmetic, cost)
                : MetaProgression.SetRocket(EquippedRocket == cosmetic ? RocketCosmetic.Classic : cosmetic);
            if (ok) _analytics?.Track("cosmetic_selected", new AnalyticsParam("category", "rocket"), new AnalyticsParam("item", cosmetic));
            return ok;
        }

        private bool BuyOrToggleExplosion(ExplosionCosmetic cosmetic, int cost)
        {
            bool ok = !MetaProgression.IsOwned(cosmetic)
                ? MetaProgression.BuyAndEquip(cosmetic, cost)
                : MetaProgression.SetExplosion(EquippedExplosion == cosmetic ? ExplosionCosmetic.Classic : cosmetic);
            if (ok) _analytics?.Track("cosmetic_selected", new AnalyticsParam("category", "explosion"), new AnalyticsParam("item", cosmetic));
            return ok;
        }

        private bool BuyOrToggleSpace(SpaceCosmetic cosmetic, int cost)
        {
            bool ok = !MetaProgression.IsOwned(cosmetic)
                ? MetaProgression.BuyAndEquip(cosmetic, cost)
                : MetaProgression.SetSpace(EquippedSpace == cosmetic ? SpaceCosmetic.Classic : cosmetic);
            if (ok)
            {
                ApplyCosmeticsVisuals();
                _analytics?.Track("cosmetic_selected", new AnalyticsParam("category", "space"), new AnalyticsParam("item", cosmetic));
            }
            return ok;
        }

        public bool BuyOrEquipMars() => BuyOrTogglePlanet(PlanetCosmetic.Mars, MarsSkinCost);
        public bool BuyOrEquipVenus() => BuyOrTogglePlanet(PlanetCosmetic.Venus, VenusSkinCost);
        public bool BuyOrEquipNeptune() => BuyOrTogglePlanet(PlanetCosmetic.Neptune, NeptuneSkinCost);
        public bool BuyOrEquipInvokerRockets() => BuyOrToggleRocket(RocketCosmetic.InvokerOrbs, InvokerRocketPackCost);
        public bool BuyOrEquipSolarRockets() => BuyOrToggleRocket(RocketCosmetic.SolarLance, SolarRocketThemeCost);
        public bool BuyOrEquipCryoRockets() => BuyOrToggleRocket(RocketCosmetic.CryoPulse, CryoRocketThemeCost);
        public bool BuyOrEquipVioletExplosion() => BuyOrToggleExplosion(ExplosionCosmetic.VioletNova, VioletExplosionCost);
        public bool BuyOrEquipSolarExplosion() => BuyOrToggleExplosion(ExplosionCosmetic.SolarBurst, SolarExplosionCost);
        public bool BuyOrEquipEmeraldExplosion() => BuyOrToggleExplosion(ExplosionCosmetic.EmeraldShock, EmeraldExplosionCost);

        public bool BuyOrEquipKamikazeAsteroids()
        {
            bool ok = !MetaProgression.IsOwned(AsteroidCosmetic.KamikazeShips)
                ? MetaProgression.BuyAndEquip(AsteroidCosmetic.KamikazeShips, _config.KamikazeAsteroidCost)
                : MetaProgression.SetAsteroid(UseKamikazeAsteroids ? AsteroidCosmetic.Classic : AsteroidCosmetic.KamikazeShips);
            if (ok) _analytics?.Track("cosmetic_selected", new AnalyticsParam("category", "asteroid"), new AnalyticsParam("item", MetaProgression.Asteroid));
            return ok;
        }

        public bool BuyOrEquipPurpleSpace() => BuyOrToggleSpace(SpaceCosmetic.PurpleNebula, PurpleSpaceCost);
        public bool BuyOrEquipCrimsonSpace() => BuyOrToggleSpace(SpaceCosmetic.CrimsonVoid, CrimsonSpaceCost);
        public bool BuyOrEquipFrozenSpace() => BuyOrToggleSpace(SpaceCosmetic.FrozenNight, FrozenSpaceCost);

        public void EquipClassicPlanet()
        {
            MetaProgression.SetPlanet(PlanetCosmetic.Earth);
            ApplyCosmeticsVisuals();
        }

        public void EquipClassicRockets() => MetaProgression.SetRocket(RocketCosmetic.Classic);
        public void EquipClassicExplosion() => MetaProgression.SetExplosion(ExplosionCosmetic.Classic);
        public void EquipClassicAsteroids() => MetaProgression.SetAsteroid(AsteroidCosmetic.Classic);

        public void EquipClassicSpace()
        {
            MetaProgression.SetSpace(SpaceCosmetic.Classic);
            ApplyCosmeticsVisuals();
        }

        // Kept for backwards compatibility with older generated scenes/tools.
        public void EquipDefaultCosmetics()
        {
            MetaProgression.EquipDefaults();
            ApplyCosmeticsVisuals();
        }

        public int MarsSkinCost => _config != null ? _config.MarsSkinCost : 0;
        public int VenusSkinCost => _config != null ? _config.VenusSkinCost : 0;
        public int NeptuneSkinCost => _config != null ? _config.NeptuneSkinCost : 0;
        public int InvokerRocketPackCost => _config != null ? _config.InvokerRocketPackCost : 0;
        public int SolarRocketThemeCost => _config != null ? _config.SolarRocketThemeCost : 0;
        public int CryoRocketThemeCost => _config != null ? _config.CryoRocketThemeCost : 0;
        public int VioletExplosionCost => _config != null ? _config.VioletExplosionCost : 0;
        public int SolarExplosionCost => _config != null ? _config.SolarExplosionCost : 0;
        public int EmeraldExplosionCost => _config != null ? _config.EmeraldExplosionCost : 0;
        public int KamikazeAsteroidCost => _config != null ? _config.KamikazeAsteroidCost : 0;
        public int PurpleSpaceCost => _config != null ? _config.PurpleSpaceCost : 0;
        public int CrimsonSpaceCost => _config != null ? _config.CrimsonSpaceCost : 0;
        public int FrozenSpaceCost => _config != null ? _config.FrozenSpaceCost : 0;

        public string GetCorePackPrice(int pack)
        {
            if (_monetization == null) return "STORE";
            if (pack == 0) return _monetization.GetLocalizedPrice("cores_5", "$0.99");
            if (pack == 1) return _monetization.GetLocalizedPrice("cores_18", "$2.99");
            return _monetization.GetLocalizedPrice("cores_50", "$6.99");
        }

        public void PurchaseCorePack(int pack)
        {
            if (_monetization == null || !CorePurchasesAvailable) return;
            switch (pack)
            {
                case 0: _monetization.PurchaseCorePack("cores_5", 5, () => _analytics?.Track("iap_complete", new AnalyticsParam("product", "cores_5"))); break;
                case 1: _monetization.PurchaseCorePack("cores_18", 18, () => _analytics?.Track("iap_complete", new AnalyticsParam("product", "cores_18"))); break;
                default: _monetization.PurchaseCorePack("cores_50", 50, () => _analytics?.Track("iap_complete", new AnalyticsParam("product", "cores_50"))); break;
            }
        }
    }
}

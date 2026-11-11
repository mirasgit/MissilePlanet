#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MissilePlanet.Editor
{
    public static class MissilePlanetBuilder
    {
        private const string Root = "Assets/MissilePlanet/Generated";
        private const string Sprites = Root + "/Sprites";
        private const string Prefabs = Root + "/Prefabs";
        private const string Materials = Root + "/Materials";
        private const string Configs = Root + "/Config";
        private const string Scenes = Root + "/Scenes";
        private const string ScenePath = Scenes + "/MissilePlanetDemo.unity";

        [MenuItem("Tools/Missile Planet/Build Demo Scene")]
        public static void BuildDemoScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EnsureFolders();

            Sprite circle = CreateOrLoadSprite("Circle", PixelInsideCircle);
            Sprite square = CreateOrLoadSprite("Square", (x, y, size) => true);
            Sprite triangle = CreateOrLoadSprite("Triangle", PixelInsideTriangle);
            Sprite pulseRing = CreateOrLoadSprite("PulseRing", PixelInsideRing);
            Sprite shieldRing = CreateOrLoadSprite("ShieldRing", PixelInsideThinRing);
            Sprite kamikazeShip = CreateOrLoadSprite("KamikazeShip", PixelInsideKamikazeShip);

            MissilePlanetConfig config = CreateOrLoadConfig();
            // Layout migration for v2: planet diameter is 20% of the 18-unit-tall portrait view.
            // This also updates projects that already generated the old 3.2-radius config asset.
            config.PlanetRadius = 1.8f;
            config.AsteroidTargetHitRadius = 0.7f;
            config.AsteroidSpawnTopMargin = 0.9f;
            config.AsteroidSpawnHorizontalPadding = 0.06f;
            config.PlanetPulseDuration = 1.15f;
            config.PlanetPulseScreenPadding = 1.2f;
            config.PulseCarrierWaveInterval = 5;
            config.PulseCarrierSpeedMultiplier = 1.8f;
            config.PulseCarrierScore = 250;
            config.StartingPulseCharges = 1;
            config.MaxPulseCharges = 3;
            config.RedAsteroidUnlockAfterWave = 5;
            config.RedAsteroidChance = 0.20f;
            config.RedAsteroidScaleMultiplier = 1.4f;
            config.RedSplitCountBeforeUpgrade = 2;
            config.RedSplitCountAfterUpgrade = 3;
            config.RedSplitTravelFraction = 0.50f;
            config.RedBlinkSlowInterval = 0.55f;
            config.RedBlinkFastInterval = 0.08f;
            config.RedUpgradeAfterWave = 15;
            config.RedFragmentSpeedMultiplier = 1.08f;
            config.GreenAsteroidUnlockAfterWave = 10;
            config.GreenAsteroidChance = 0.10f;
            config.GreenAsteroidSpeedMultiplier = 0.72f;
            config.GreenHitsBeforeUpgrade = 2;
            config.GreenHitsAfterUpgrade = 3;
            config.GreenUpgradeAfterWave = 15;
            config.YellowAsteroidUnlockAfterWave = 20;
            config.YellowGuaranteedWave = 21;
            config.YellowReplaceGreenChance = 0.30f;
            config.YellowAsteroidDashSpeed = 14.5f;
            config.YellowMinManeuvers = 4;
            config.YellowMaxManeuvers = 5;
            config.YellowWaypointReachDistance = 0.32f;
            config.YellowAsteroidScore = 350;
            config.BossWaveInterval = 10;
            config.BossFireRateIncreasePerDestroyedEngine = 0.30f;
            config.BossReloadRateIncreasePerDestroyedEngine = 0.14f;
            config.BossTierFireRateIncrease = 0.16f;
            config.BossTierReloadRateIncrease = 0.10f;
            config.BossEngineScore = 1000;
            config.BossDefeatScore = 5000;
            config.BossSpawnY = 4.6f;
            config.BossMoveAmplitude = 1.25f;
            config.BossMoveAngularSpeed = 0.85f;
            config.BossCoreBarrageInterval = 4.2f;
            config.BossCoreTelegraphDuration = 0.85f;
            config.BossCoreBaseProjectiles = 3;
            config.BossCoreProjectileSpeedMultiplier = 1.18f;
            config.BossEngineBaseHitPoints = 2;
            config.BossEngineHitPointsPerTwoTiers = 1;
            config.BossEngineHitCooldown = 0.18f;
            config.BossRetaliationProjectiles = 3;
            config.BossRetaliationSpeedMultiplier = 1.30f;
            config.BossEngineGlobalLockoutDuration = 5f;
            config.ShopMissilesPerUpgrade = 3;
            config.ShopMissileUpgradeBaseCost = 25;
            config.ShopMissileUpgradeCostStep = 15;
            config.ShopMissileUpgradeMaxLevel = 5;
            config.ShopPowerUpgradeBaseCost = 50;
            config.ShopPowerUpgradeCostMultiplier = 5;
            config.ShopPowerUpgradeMaxLevel = 4;
            config.ShopPowerRadiusBonusPerLevel = 0.18f;
            config.ShopPowerDurationBonusPerLevel = 0.04f;
            config.ShopShieldChargeCost = 35;
            config.ShopGuardianCost = 120;
            config.ShopGuardianCostStep = 140;
            config.ShopGuardianMaxCount = 5;
            config.ShopCannonCost = 90;
            config.ShopCannonCostStep = 260;
            config.ShopCannonMaxCount = 3;
            config.CoinPerNormalAsteroid = 1;
            config.CoinPerRedAsteroid = 2;
            config.CoinPerGreenAsteroid = 3;
            config.CoinPerYellowAsteroid = 4;
            config.CoinPerCarrier = 5;
            config.CoinPerBossEngine = 4;
            config.CoinPerBossDefeat = 10;
            config.CoinPerSurvivingCity = 1;
            config.BossCoreReward = 1;
            config.AdCoreReward = 1;
            config.ReviveBaseCoreCost = 1;
            config.ReviveCoreMultiplier = 2;
            config.MarsSkinCost = 6;
            config.VenusSkinCost = 7;
            config.NeptuneSkinCost = 9;
            config.InvokerRocketPackCost = 8;
            config.SolarRocketThemeCost = 6;
            config.CryoRocketThemeCost = 7;
            config.VioletExplosionCost = 4;
            config.SolarExplosionCost = 5;
            config.EmeraldExplosionCost = 5;
            config.KamikazeAsteroidCost = 7;
            config.PurpleSpaceCost = 5;
            config.CrimsonSpaceCost = 6;
            config.FrozenSpaceCost = 6;
            config.GuardianOrbitRadius = 2.45f;
            config.GuardianAngularSpeed = 24f;
            config.GuardianCollisionRadius = 0.42f;
            config.CannonOrbitRadius = 2.85f;
            config.CannonAngularSpeed = -31f;
            config.CannonCollisionRadius = 0.28f;
            config.CannonFireInterval = 2.2f;
            config.CannonMissileSpeed = 15.5f;
            config.CannonExplosionRadius = 0.58f;
            config.CannonExplosionDuration = 0.36f;
            config.RedAsteroidScore = 175;
            config.GreenAsteroidScore = 225;
            config.BossEngineShakeDuration = 0.22f;
            config.BossEngineShakeMagnitude = 0.12f;
            EditorUtility.SetDirty(config);

            Material asteroidTrail = CreateOrLoadMaterial("AsteroidTrail", new Color(1f, 0.35f, 0.08f, 0.75f));
            Material missileTrail = CreateOrLoadMaterial("MissileTrail", new Color(0.55f, 0.92f, 1f, 0.9f));
            GameObject asteroidPrefab = CreateAsteroidPrefab(circle, kamikazeShip, asteroidTrail);
            GameObject missilePrefab = CreateMissilePrefab(triangle, missileTrail);
            GameObject explosionPrefab = CreateExplosionPrefab(circle);
            GameObject planetPulsePrefab = CreatePlanetPulsePrefab(pulseRing);
            GameObject alienBossPrefab = CreateAlienBossPrefab(circle, square);
            GameObject orbitalHelperPrefab = CreateOrbitalHelperPrefab(circle, square);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Camera camera = CreateCamera();
            CreateStars(circle, camera);

            GameObject poolGo = new GameObject("Pool");
            SimplePool pool = poolGo.AddComponent<SimplePool>();

            GameObject planet = CreatePlanet(circle, square, triangle, shieldRing, config);
            PlanetController planetController = planet.GetComponent<PlanetController>();
            SpriteRenderer pulseShieldRenderer = planet.transform.Find("PulseReadyShield")?.GetComponent<SpriteRenderer>();

            CameraShake shake = camera.gameObject.AddComponent<CameraShake>();

            GameObject sfxGo = new GameObject("SFX");
            // 2D AudioSources do not care about world position. Keeping the mixer offscreen
            // prevents its AudioSource gizmo from appearing as a speaker icon in Game view.
            sfxGo.transform.position = new Vector3(1000f, 1000f, 0f);
            AudioListener listener = sfxGo.AddComponent<AudioListener>();
            listener.hideFlags = HideFlags.HideInInspector;
            SfxPlayer sfxPlayer = sfxGo.AddComponent<SfxPlayer>();
            sfxPlayer.Configure(camera);
            sfxGo.AddComponent<MusicPlayer>();

            GameObject gameGo = new GameObject("MissilePlanetGame");
            gameGo.AddComponent<MonetizationBridge>();
            gameGo.AddComponent<AnalyticsBridge>();
            gameGo.AddComponent<CrashReportingBridge>();
            gameGo.AddComponent<HapticsBridge>();
            MissilePlanetGame game = gameGo.AddComponent<MissilePlanetGame>();
            gameGo.AddComponent<TutorialHints>();
            MissilePlanetHUD hud = gameGo.AddComponent<MissilePlanetHUD>();
            hud.SetGame(game);

            game.Configure(
                config,
                camera,
                planet.transform,
                planetController,
                pool,
                shake,
                sfxPlayer,
                asteroidPrefab,
                missilePrefab,
                explosionPrefab,
                planetPulsePrefab,
                alienBossPrefab,
                orbitalHelperPrefab,
                pulseShieldRenderer);

            CreateInstructionsLabel(gameGo.transform);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            EditorUtility.DisplayDialog(
                "Missile Planet",
                "Release-candidate scene created.\n\nOpen Game view, choose Portrait 9:16, then press Play.\nUse Tools > Missile Planet > Validate Release Setup before Android build.",
                "OK");
        }

        private static void EnsureFolders()
        {
            CreateFolderIfMissing("Assets/MissilePlanet", "Generated");
            CreateFolderIfMissing(Root, "Sprites");
            CreateFolderIfMissing(Root, "Prefabs");
            CreateFolderIfMissing(Root, "Materials");
            CreateFolderIfMissing(Root, "Config");
            CreateFolderIfMissing(Root, "Scenes");
        }

        private static void CreateFolderIfMissing(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, child);
        }

        private delegate bool PixelPredicate(int x, int y, int size);

        private static Sprite CreateOrLoadSprite(string name, PixelPredicate predicate)
        {
            string assetPath = $"{Sprites}/{name}.png";
            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);

            if (!File.Exists(fullPath))
            {
                const int size = 64;
                Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                Color[] pixels = new Color[size * size];

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                        pixels[y * size + x] = predicate(x, y, size) ? Color.white : Color.clear;
                }

                texture.SetPixels(pixels);
                texture.Apply();
                File.WriteAllBytes(fullPath, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            }

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 64f;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static bool PixelInsideCircle(int x, int y, int size)
        {
            float center = (size - 1) * 0.5f;
            float dx = x - center;
            float dy = y - center;
            float radius = size * 0.48f;
            return dx * dx + dy * dy <= radius * radius;
        }

        private static bool PixelInsideRing(int x, int y, int size)
        {
            float center = (size - 1) * 0.5f;
            float dx = x - center;
            float dy = y - center;
            float distanceSqr = dx * dx + dy * dy;
            float outer = size * 0.48f;
            float inner = size * 0.405f;
            return distanceSqr <= outer * outer && distanceSqr >= inner * inner;
        }

        private static bool PixelInsideThinRing(int x, int y, int size)
        {
            float center = (size - 1) * 0.5f;
            float dx = x - center;
            float dy = y - center;
            float distanceSqr = dx * dx + dy * dy;
            float outer = size * 0.49f;
            float inner = size * 0.455f;
            return distanceSqr <= outer * outer && distanceSqr >= inner * inner;
        }

        private static bool PixelInsideKamikazeShip(int x, int y, int size)
        {
            float nx = x / (float)(size - 1);
            float ny = y / (float)(size - 1);
            // Simple symmetric dart/spacecraft silhouette: nose up, swept wings, narrow tail.
            float cy = ny;
            float halfWidth;
            if (cy > 0.62f)
                halfWidth = Mathf.Lerp(0.34f, 0.02f, (cy - 0.62f) / 0.38f);
            else if (cy > 0.30f)
                halfWidth = Mathf.Lerp(0.12f, 0.34f, (cy - 0.30f) / 0.32f);
            else
                halfWidth = Mathf.Lerp(0.06f, 0.12f, cy / 0.30f);
            return Mathf.Abs(nx - 0.5f) <= halfWidth && cy > 0.04f && cy < 0.98f;
        }

        private static bool PixelInsideTriangle(int x, int y, int size)
        {
            float nx = x / (float)(size - 1);
            float ny = y / (float)(size - 1);
            if (ny < 0.05f || ny > 0.95f)
                return false;

            float halfWidth = (1f - ny) * 0.5f + 0.04f;
            return Mathf.Abs(nx - 0.5f) <= halfWidth;
        }

        private static MissilePlanetConfig CreateOrLoadConfig()
        {
            string path = Configs + "/MissilePlanetConfig.asset";
            MissilePlanetConfig config = AssetDatabase.LoadAssetAtPath<MissilePlanetConfig>(path);
            if (config != null)
                return config;

            config = ScriptableObject.CreateInstance<MissilePlanetConfig>();
            AssetDatabase.CreateAsset(config, path);
            return config;
        }

        private static Material CreateOrLoadMaterial(string name, Color color)
        {
            string path = Materials + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader == null)
                    shader = Shader.Find("Unlit/Transparent");

                material = new Material(shader);
                material.name = name;
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateOrLoadParticleMaterial(string name, Texture texture)
        {
            string path = Materials + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                // Sprite shader is deliberately preferred here: it is transparent, tiny and
                // respects ParticleSystem vertex color/alpha without realtime lighting.
                Shader shader = Shader.Find("Sprites/Default");
                if (shader == null)
                    shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (shader == null)
                    shader = Shader.Find("Particles/Standard Unlit");

                material = new Material(shader);
                material.name = name;
                AssetDatabase.CreateAsset(material, path);
            }

            if (material.HasProperty("_MainTex"))
                material.mainTexture = texture;
            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", texture);
            material.color = Color.white;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject CreateAsteroidPrefab(Sprite circle, Sprite kamikazeShip, Material trailMaterial)
        {
            string path = Prefabs + "/Asteroid.prefab";
            GameObject go = new GameObject("Asteroid");
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = circle;
            renderer.color = new Color(0.72f, 0.44f, 0.24f, 1f);
            renderer.sortingOrder = 20;

            TrailRenderer trail = go.AddComponent<TrailRenderer>();
            trail.material = trailMaterial;
            trail.time = 0.55f;
            trail.startWidth = 0.13f;
            trail.endWidth = 0f;
            trail.startColor = new Color(1f, 0.42f, 0.12f, 0.85f);
            trail.endColor = new Color(1f, 0.1f, 0.02f, 0f);
            trail.sortingOrder = 19;

            // Cheap pseudo-glow used only by Pulse Carrier asteroids. No realtime Light.
            GameObject glow = new GameObject("CarrierGlow");
            glow.transform.SetParent(go.transform, false);
            glow.transform.localScale = Vector3.one * 2.4f;
            SpriteRenderer glowRenderer = glow.AddComponent<SpriteRenderer>();
            glowRenderer.sprite = circle;
            glowRenderer.color = new Color(0.18f, 1f, 0.92f, 0.3f);
            glowRenderer.sortingOrder = 18;
            glowRenderer.enabled = false;

            Asteroid asteroid = go.AddComponent<Asteroid>();
            asteroid.ConfigureSkins(circle, kamikazeShip);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateAlienBossPrefab(Sprite circle, Sprite square)
        {
            string path = Prefabs + "/AlienBoss.prefab";
            GameObject root = new GameObject("AlienBoss");
            root.AddComponent<AlienBoss>();

            CreateSpriteObject(
                "Body",
                circle,
                new Color(0.34f, 0.16f, 0.52f, 1f),
                root.transform,
                Vector3.zero,
                new Vector3(5.3f, 1.7f, 1f),
                24);

            CreateSpriteObject(
                "Cockpit",
                circle,
                new Color(0.24f, 0.88f, 0.96f, 0.86f),
                root.transform,
                new Vector3(0f, 0.30f, -0.05f),
                new Vector3(2.25f, 0.76f, 1f),
                25);

            CreateSpriteObject(
                "CoreEmitter",
                circle,
                new Color(1f, 0.18f, 0.72f, 0.72f),
                root.transform,
                new Vector3(0f, -0.38f, -0.06f),
                new Vector3(0.62f, 0.62f, 1f),
                27);

            CreateSpriteObject(
                "EyeLeft",
                circle,
                new Color(0.88f, 1f, 1f, 1f),
                root.transform,
                new Vector3(-0.48f, 0.36f, -0.08f),
                Vector3.one * 0.17f,
                26);
            CreateSpriteObject(
                "EyeRight",
                circle,
                new Color(0.88f, 1f, 1f, 1f),
                root.transform,
                new Vector3(0.48f, 0.36f, -0.08f),
                Vector3.one * 0.17f,
                26);

            float[] engineX = { -1.75f, -0.58f, 0.58f, 1.75f };
            for (int i = 0; i < engineX.Length; i++)
            {
                GameObject engine = CreateSpriteObject(
                    $"Engine_{i + 1}",
                    square,
                    new Color(0.95f, 0.46f, 0.12f, 1f),
                    root.transform,
                    new Vector3(engineX[i], -0.77f, -0.08f),
                    new Vector3(0.56f, 0.48f, 1f),
                    28);
                engine.AddComponent<BossEngine>();

                GameObject glow = CreateSpriteObject(
                    "ReloadGlow",
                    circle,
                    new Color(0.32f, 0.9f, 1f, 0.32f),
                    engine.transform,
                    new Vector3(0f, 0f, 0.05f),
                    Vector3.one * 1.75f,
                    27);
                glow.GetComponent<SpriteRenderer>().enabled = false;
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateOrbitalHelperPrefab(Sprite circle, Sprite square)
        {
            string path = Prefabs + "/OrbitalHelper.prefab";
            GameObject root = new GameObject("OrbitalHelper");

            SpriteRenderer body = root.AddComponent<SpriteRenderer>();
            body.sprite = circle;
            body.color = Color.white;
            body.sortingOrder = 21;

            GameObject core = CreateSpriteObject(
                "Core",
                square,
                Color.white,
                root.transform,
                new Vector3(0f, 0f, -0.05f),
                new Vector3(0.42f, 0.42f, 1f),
                22);
            core.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

            OrbitalHelper helper = root.AddComponent<OrbitalHelper>();
            _ = helper;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateMissilePrefab(Sprite triangle, Material trailMaterial)
        {
            string path = Prefabs + "/Missile.prefab";
            GameObject go = new GameObject("Missile");
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = triangle;
            renderer.color = new Color(0.72f, 0.95f, 1f, 1f);
            renderer.sortingOrder = 30;

            TrailRenderer trail = go.AddComponent<TrailRenderer>();
            trail.material = trailMaterial;
            trail.time = 0.35f;
            trail.startWidth = 0.08f;
            trail.endWidth = 0f;
            trail.startColor = new Color(0.7f, 0.95f, 1f, 0.95f);
            trail.endColor = new Color(0.3f, 0.7f, 1f, 0f);
            trail.sortingOrder = 29;

            go.AddComponent<Missile>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateExplosionPrefab(Sprite circle)
        {
            string path = Prefabs + "/Explosion.prefab";
            GameObject go = new GameObject("Explosion");
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = circle;
            renderer.color = new Color(0.2f, 0.85f, 1f, 0.36f);
            renderer.sortingOrder = 10;
            go.AddComponent<Explosion>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreatePlanetPulsePrefab(Sprite ring)
        {
            string path = Prefabs + "/PlanetPulse.prefab";
            GameObject go = new GameObject("PlanetPulse");
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = ring;
            renderer.color = new Color(0.25f, 0.92f, 1f, 0.86f);
            renderer.sortingOrder = 16;
            go.AddComponent<PlanetPulseWave>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static Camera CreateCamera()
        {
            GameObject go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            Camera camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 9f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.backgroundColor = new Color(0.008f, 0.012f, 0.035f, 1f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            return camera;
        }

        private static void CreateStars(Sprite circle, Camera camera)
        {
            // One ParticleSystem replaces 85 individual star GameObjects. Particles are stationary
            // and fade in/out over lifetime, producing a cheap twinkle on mobile.
            GameObject root = new GameObject("Stars");
            root.transform.position = new Vector3(0f, 0f, 2f);

            ParticleSystem particles = root.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.loop = true;
            main.prewarm = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.3f, 3.6f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.085f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.68f, 0.82f, 1f, 0.65f),
                new Color(0.95f, 0.98f, 1f, 1f));
            main.maxParticles = 95;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 34f;

            float halfHeight = camera.orthographicSize;
            float halfWidth = halfHeight * (9f / 16f);
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(halfWidth * 2f, halfHeight * 2f, 0.05f);

            Gradient twinkle = new Gradient();
            twinkle.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(0.78f, 0.9f, 1f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.22f),
                    new GradientAlphaKey(0.28f, 0.52f),
                    new GradientAlphaKey(1f, 0.74f),
                    new GradientAlphaKey(0f, 1f)
                });

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(twinkle);

            ParticleSystemRenderer renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = -50;
            renderer.material = CreateOrLoadParticleMaterial("StarParticles", circle.texture);
        }

        private static GameObject CreatePlanet(Sprite circle, Sprite square, Sprite triangle, Sprite shieldRing, MissilePlanetConfig config)
        {
            GameObject root = new GameObject("Planet");
            root.transform.position = new Vector3(0f, -6.9f, 0f);
            PlanetController controller = root.AddComponent<PlanetController>();
            controller.RotationSpeed = config.PlanetRotationSpeed;

            GameObject atmosphere = CreateSpriteObject(
                "Atmosphere",
                circle,
                new Color(0.14f, 0.62f, 1f, 0.18f),
                root.transform,
                new Vector3(0f, 0f, 0.3f),
                Vector3.one * config.PlanetRadius * 2.12f,
                -7);

            GameObject disc = CreateSpriteObject(
                "PlanetDisc",
                circle,
                new Color(0.055f, 0.23f, 0.46f, 1f),
                root.transform,
                Vector3.zero,
                Vector3.one * config.PlanetRadius * 2f,
                -6);

            GameObject pulseReadyShield = CreateSpriteObject(
                "PulseReadyShield",
                shieldRing,
                new Color(0.28f, 0.9f, 1f, 0.78f),
                root.transform,
                new Vector3(0f, 0f, -0.12f),
                Vector3.one * config.PlanetRadius * 2.18f,
                2);

            _ = pulseReadyShield;
            _ = atmosphere;
            _ = disc;
            CreateContinents(root.transform, circle, config.PlanetRadius);
            CreateTargets(root.transform, square, triangle, config.PlanetRadius);
            return root;
        }

        private static void CreateContinents(Transform parent, Sprite circle, float radius)
        {
            Vector2[] positions =
            {
                new Vector2(-1.25f, 0.85f), new Vector2(-0.55f, 1.2f), new Vector2(1.05f, 1.1f),
                new Vector2(1.45f, 0.3f), new Vector2(0.55f, -0.65f), new Vector2(-1.2f, -0.55f),
                new Vector2(0.15f, 0.35f)
            };

            Vector2[] scales =
            {
                new Vector2(1.2f, 0.6f), new Vector2(0.65f, 0.45f), new Vector2(1.05f, 0.5f),
                new Vector2(0.7f, 0.42f), new Vector2(1.1f, 0.5f), new Vector2(0.75f, 0.42f),
                new Vector2(0.7f, 0.32f)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                float factor = radius / 3.2f;
                CreateSpriteObject(
                    $"Continent_{i + 1}",
                    circle,
                    new Color(0.08f, 0.55f, 0.39f, 0.95f),
                    parent,
                    new Vector3(positions[i].x * factor, positions[i].y * factor, -0.1f),
                    new Vector3(scales[i].x * factor, scales[i].y * factor, 1f),
                    -5);
            }
        }

        private static void CreateTargets(Transform parent, Sprite square, Sprite triangle, float radius)
        {
            float[] cityAngles = { 20f, 66f, 108f, 154f, 218f, 278f };
            float[] launcherAngles = { 0f, 136f, 320f };

            for (int i = 0; i < cityAngles.Length; i++)
            {
                GameObject city = CreateSurfaceTarget(
                    $"City_{i + 1}",
                    square,
                    new Color(1f, 0.82f, 0.32f, 1f),
                    parent,
                    radius + 0.22f,
                    cityAngles[i],
                    new Vector3(0.36f, 0.22f, 1f),
                    PlanetTargetType.City);

                CreateCityTop(city.transform, square);
            }

            for (int i = 0; i < launcherAngles.Length; i++)
            {
                CreateSurfaceTarget(
                    $"Launcher_{i + 1}",
                    triangle,
                    new Color(0.3f, 1f, 0.78f, 1f),
                    parent,
                    radius + 0.3f,
                    launcherAngles[i],
                    new Vector3(0.3f, 0.48f, 1f),
                    PlanetTargetType.Launcher);
            }
        }

        private static GameObject CreateSurfaceTarget(
            string name,
            Sprite sprite,
            Color color,
            Transform parent,
            float radius,
            float angle,
            Vector3 scale,
            PlanetTargetType type)
        {
            float radians = angle * Mathf.Deg2Rad;
            Vector3 localPosition = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f) * radius;
            GameObject go = CreateSpriteObject(name, sprite, color, parent, localPosition, scale, 4);
            go.transform.localRotation = Quaternion.Euler(0f, 0f, angle - 90f);
            PlanetTarget target = go.AddComponent<PlanetTarget>();
            target.Configure(type, 1);
            return go;
        }

        private static void CreateCityTop(Transform city, Sprite square)
        {
            GameObject top = CreateSpriteObject(
                "Tower",
                square,
                new Color(1f, 0.94f, 0.62f, 1f),
                city,
                new Vector3(0f, 0.55f, -0.05f),
                new Vector3(0.5f, 0.85f, 1f),
                5);
            _ = top;
        }

        private static GameObject CreateSpriteObject(
            string name,
            Sprite sprite,
            Color color,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            int sortingOrder)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;

            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return go;
        }

        private static void CreateInstructionsLabel(Transform parent)
        {
            GameObject marker = new GameObject("README_in_Hierarchy");
            marker.transform.SetParent(parent, false);
            marker.AddComponent<MissilePlanetSceneNote>();
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool exists = false;

            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].path == scenePath)
                {
                    scenes[i] = new EditorBuildSettingsScene(scenePath, true);
                    exists = true;
                    break;
                }
            }

            if (!exists)
                scenes.Add(new EditorBuildSettingsScene(scenePath, true));

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif

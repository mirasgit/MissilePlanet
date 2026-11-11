#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MissilePlanet.Editor
{
    public static class MissilePlanetReleaseValidator
    {
        private const string ScenePath = "Assets/MissilePlanet/Generated/Scenes/MissilePlanetDemo.unity";
        private const string ConfigPath = "Assets/MissilePlanet/Generated/Config/MissilePlanetConfig.asset";

        [MenuItem("Tools/Missile Planet/Validate Release Setup")]
        public static void ValidateReleaseSetup()
        {
            List<string> ok = new List<string>();
            List<string> warnings = new List<string>();

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) ok.Add("Demo scene exists");
            else warnings.Add("Demo scene is missing — run Build Demo Scene");

            if (AssetDatabase.LoadAssetAtPath<MissilePlanetConfig>(ConfigPath) != null) ok.Add("Config asset exists");
            else warnings.Add("Config asset is missing — run Build Demo Scene");

            bool sceneEnabled = false;
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.path == ScenePath && scene.enabled)
                {
                    sceneEnabled = true;
                    break;
                }
            }
            if (sceneEnabled) ok.Add("Scene is enabled in Build Settings");
            else warnings.Add("Generated scene is not enabled in Build Settings");

            if (PlayerSettings.defaultInterfaceOrientation == UIOrientation.Portrait) ok.Add("Orientation is Portrait");
            else warnings.Add("Set Default Orientation to Portrait");

            if (PlayerSettings.Android.targetArchitectures.HasFlag(AndroidArchitecture.ARM64)) ok.Add("ARM64 is enabled");
            else warnings.Add("Enable ARM64 for Google Play");

            if (PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android) == ScriptingImplementation.IL2CPP) ok.Add("Android backend is IL2CPP");
            else warnings.Add("IL2CPP is recommended for the production Android build");

            string applicationId = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
            if (!string.IsNullOrWhiteSpace(applicationId) && !applicationId.Contains("Company.ProductName"))
                ok.Add("Application identifier is configured");
            else
                warnings.Add("Set a unique Android package/application identifier");

            if (!string.IsNullOrWhiteSpace(PlayerSettings.bundleVersion) && PlayerSettings.bundleVersion != "0.1") ok.Add("Bundle version is configured");
            else warnings.Add("Set a release bundle version (for example 1.0.0)");

            if (EditorUserBuildSettings.buildAppBundle) ok.Add("Android App Bundle (AAB) is enabled");
            else warnings.Add("Enable Build App Bundle (Google Play requires AAB for normal new-app publishing)");

            string targetSdk = PlayerSettings.Android.targetSdkVersion.ToString();
            if (targetSdk.Contains("36"))
                ok.Add($"Android target SDK: {targetSdk}");
            else if (targetSdk.Contains("Auto"))
                warnings.Add("Android Target API is Automatic — verify the installed SDK/build resolves to API 36+ before Play upload");
            else
                warnings.Add($"Android target SDK is {targetSdk}; Google Play submissions in late 2026 require API 36+");

            string message = "READY CHECK\n\n";
            for (int i = 0; i < ok.Count; i++) message += "✓ " + ok[i] + "\n";
            if (warnings.Count > 0)
            {
                message += "\nNEEDS ATTENTION\n";
                for (int i = 0; i < warnings.Count; i++) message += "• " + warnings[i] + "\n";
            }
            else
            {
                message += "\nNo local project blockers detected. External SDK/store/privacy setup is still your responsibility.";
            }

            Debug.Log(message);
            EditorUtility.DisplayDialog("Missile Planet — Release Validation", message, "OK");
        }

        [MenuItem("Tools/Missile Planet/Prepare Android Release Defaults")]
        public static void PrepareAndroidReleaseDefaults()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            EditorUserBuildSettings.buildAppBundle = true;

            // Prefer API 36 when this installed Unity/Android module exposes the enum.
            // Otherwise keep Auto and the validator will remind you to verify the resolved SDK.
            if (Enum.TryParse("AndroidApiLevel36", out AndroidSdkVersions api36))
                PlayerSettings.Android.targetSdkVersion = api36;
            else
                PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

            AssetDatabase.SaveAssets();
            Debug.Log("Missile Planet Android release defaults applied: Portrait, ARM64, IL2CPP, AAB, target API 36 when available.");
            EditorUtility.DisplayDialog(
                "Missile Planet — Android Defaults",
                "Applied Portrait, ARM64, IL2CPP and AAB. Target SDK was set to API 36 when this Unity installation exposes it; otherwise Auto is used. Set your package ID, version, keystore and signing credentials yourself, then run Validate Release Setup.",
                "OK");
        }

    }
}
#endif

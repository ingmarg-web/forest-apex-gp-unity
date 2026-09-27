using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ForestApex.Editor
{
    public static class ForestApexBuild
    {
        private const string ScenePath = "Assets/Scenes/ForestApex.unity";

        [MenuItem("Forest Apex/Generate Playable Scene")]
        public static void GeneratePlayableScene()
        {
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Forest Apex GP");
            root.AddComponent<ForestApexRuntime>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void ConfigureAndroidPlayerSettings()
        {
            PlayerSettings.productName = "Forest Apex GP";
            PlayerSettings.companyName = "Forest Apex";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.forestapex.gp.unity");
            PlayerSettings.bundleVersion = "0.2.0-unity";
            PlayerSettings.Android.bundleVersionCode = 2;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            EditorUserBuildSettings.buildAppBundle = false;
        }

        // Unity Build Automation: set this exact method as the Pre-export method.
        public static void PrepareCloudBuild()
        {
            GeneratePlayableScene();
            ConfigureAndroidPlayerSettings();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Forest Apex/Build Android APK")]
        public static void BuildAndroid()
        {
            PrepareCloudBuild();

            Directory.CreateDirectory("build/Android");
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "build/Android/ForestApexGP.apk",
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException("Forest Apex GP Android build failed.");

            Debug.Log("Forest Apex GP APK built at build/Android/ForestApexGP.apk");
        }
    }
}

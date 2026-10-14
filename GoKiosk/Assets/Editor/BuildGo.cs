using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class BuildGo
{
    const string ScenePath = "Assets/Main.unity";

    static void Configure()
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

        PlayerSettings.companyName = "wtf";
        PlayerSettings.productName = "GoKiosk";
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.wtf.gokiosk");
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel21;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel29; // only 29/30 are bundled; still runs on Go's API 25
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
        PlayerSettings.colorSpace = ColorSpace.Gamma;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.Android.forceSDCardPermission = false;

        // Project keystore (JKS) — the user's default debug.keystore is unreadable by Unity's bundled JDK 8.
        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName = Path.GetFullPath("gokiosk.keystore");
        PlayerSettings.Android.keystorePass = "gokiosk";
        PlayerSettings.Android.keyaliasName = "gokiosk";
        PlayerSettings.Android.keyaliasPass = "gokiosk";

        // Legacy built-in VR with the Oculus SDK (the last path that supports Go).
        PlayerSettings.virtualRealitySupported = true;
        UnityEditorInternal.VR.VREditor.SetVREnabledDevicesOnTargetGroup(BuildTargetGroup.Android, new[] { "Oculus" });
        PlayerSettings.stereoRenderingPath = StereoRenderingPath.SinglePass;

        IncludeShaders("Unlit/Texture", "Unlit/Transparent Cutout", "Skybox/Panoramic");

        if (!File.Exists(ScenePath))
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
    }

    // Shaders only referenced from code get stripped (shows as pink on device); force them in.
    static void IncludeShaders(params string[] names)
    {
        var settings = new SerializedObject(AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/GraphicsSettings.asset"));
        var list = settings.FindProperty("m_AlwaysIncludedShaders");
        // Shaders from "unity default resources" (e.g. GUI/Text Shader) ship anyway and break the build if listed.
        for (int i = list.arraySize - 1; i >= 0; i--)
        {
            var s = list.GetArrayElementAtIndex(i).objectReferenceValue;
            if (s != null && AssetDatabase.GetAssetPath(s) == "Library/unity default resources")
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = null;
                list.DeleteArrayElementAtIndex(i);
            }
        }
        foreach (var name in names)
        {
            var shader = Shader.Find(name);
            if (shader == null) { Debug.LogError("Shader not found: " + name); continue; }
            bool present = false;
            for (int i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == shader) present = true;
            if (present) continue;
            list.InsertArrayElementAtIndex(list.arraySize);
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = shader;
        }
        settings.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
    }

    public static void Build()
    {
        Configure();
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = "Build/GoKiosk.apk",
            target = BuildTarget.Android,
            options = BuildOptions.None,
        });
        Debug.Log("BUILD RESULT: " + report.summary.result + " size=" + report.summary.totalSize);
        EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }
}

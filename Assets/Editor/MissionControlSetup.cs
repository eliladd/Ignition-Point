#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

[InitializeOnLoad]
public static class MissionControlSetup
{
    private const string ScenePath = "Assets/Scenes/MissionControl.unity";

    static MissionControlSetup()
    {
        EditorApplication.delayCall += RunAutoSetupOnce;
    }

    private static void RunAutoSetupOnce()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            return;
        }

        ApplyItchIoWebGLSettings(silent: true);
    }

    [MenuItem("Tools/Ignition Point/Apply WebGL (itch.io) Settings")]
    public static void ApplyWebReleaseSettingsMenu()
    {
        ApplyItchIoWebGLSettings(silent: false);
    }

    public static void ApplyItchIoWebGLSettings(bool silent)
    {
        NamedBuildTarget webTarget = NamedBuildTarget.WebGL;

        PlayerSettings.productName = "Ignition Point";
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.stripEngineCode = true;
        PlayerSettings.SetManagedStrippingLevel(webTarget, ManagedStrippingLevel.Medium);

        EnsureSceneInBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();

        if (!silent)
        {
            Debug.Log(
                "[Ignition Point] WebGL Player Settings applied for itch.io:\n" +
                $"  compressionFormat = {PlayerSettings.WebGL.compressionFormat}\n" +
                $"  decompressionFallback = {PlayerSettings.WebGL.decompressionFallback}\n" +
                $"  dataCaching = {PlayerSettings.WebGL.dataCaching}\n" +
                $"  stripEngineCode = {PlayerSettings.stripEngineCode}");
        }
    }

    private static void EnsureSceneInBuildSettings(string sceneAssetPath)
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool found = false;
        for (int i = 0; i < scenes.Count; i++)
        {
            if (scenes[i].path == sceneAssetPath)
            {
                scenes[i].enabled = true;
                found = true;
            }
        }

        if (!found && System.IO.File.Exists(sceneAssetPath))
        {
            scenes.Insert(0, new EditorBuildSettingsScene(sceneAssetPath, true));
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
#endif

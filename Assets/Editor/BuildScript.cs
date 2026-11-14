using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildScript
{
    public static void BuildStandaloneWindows64()
    {
        string buildDir = Path.Combine(Directory.GetCurrentDirectory(), "Builds", "Windows");
        if (!Directory.Exists(buildDir))
        {
            Directory.CreateDirectory(buildDir);
        }

        string exePath = Path.Combine(buildDir, "AresSols2038.exe");

        string[] scenes = new string[]
        {
            "Assets/Scenes/MissionControl.unity"
        };

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = exePath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        Debug.Log($"[BuildScript] Starting build to: {exePath}");
        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[BuildScript] Build succeeded: {summary.totalSize} bytes in {summary.totalTime.TotalSeconds:F1}s");
        }
        else
        {
            Debug.LogError($"[BuildScript] Build failed with result: {summary.result}, errors: {summary.totalErrors}");
            EditorApplication.Exit(1);
        }
    }
}

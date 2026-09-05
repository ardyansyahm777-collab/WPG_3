#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace WpgGame.EditorTools
{
    /// <summary>
    /// Build headless untuk WPG_3. Dipanggil oleh CLI:
    ///   unity build /path/to/WPG_3 --editor-version 6000.6.0f1 --target StandaloneWindows64 --execute-method WpgGame.EditorTools.Builder.PerformBuild
    /// </summary>
    public static class Builder
    {
        public const string BuildDir = "Builds/Windows";

        public static void PerformBuild()
        {
            var scenes = new string[EditorBuildSettings.scenes.Length];
            for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
            {
                scenes[i] = EditorBuildSettings.scenes[i].path;
            }

            if (scenes.Length == 0)
            {
                throw new BuildFailedException("Tidak ada scene di EditorBuildSettings. Tambahkan Assets/Scenes/Main.unity dulu.");
            }

            var output = Path.Combine(Directory.GetCurrentDirectory(), BuildDir);
            Directory.CreateDirectory(output);

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(output, PlayerSettings.productName + ".exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[Builder] BUILD OK: {summary.totalSize} bytes -> {options.locationPathName} ({summary.totalTime.TotalSeconds:F1}s)");
            }
            else
            {
                foreach (var step in report.steps)
                {
                    foreach (var message in step.messages)
                    {
                        if (message.type == LogType.Error || message.type == LogType.Exception)
                        {
                            Debug.LogError($"[Builder] {message.content}");
                        }
                    }
                }
                Debug.LogError($"[Builder] BUILD GAGAL: {summary.result}");
                throw new BuildFailedException($"Build gagal: {summary.result}");
            }
        }
    }
}
#endif
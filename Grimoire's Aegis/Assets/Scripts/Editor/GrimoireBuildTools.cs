#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace GrimoireAegis.EditorTools
{
    /// <summary>
    /// Command-line build:
    ///   Unity.exe -batchmode -quit -projectPath "Grimoire's Aegis" -executeMethod GrimoireAegis.EditorTools.GrimoireBuildTools.BuildWindows
    /// Output: Builds/Windows/GrimoiresAegis.exe (git-ignored).
    /// </summary>
    public static class GrimoireBuildTools
    {
        [MenuItem("Grimoire/Build Windows Player")]
        public static void BuildWindows()
        {
            string outDir = Path.Combine("Builds", "Windows");
            Directory.CreateDirectory(outDir);
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/SampleScene.unity" },
                locationPathName = Path.Combine(outDir, "GrimoiresAegis.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(opts);
            Debug.Log($"[GrimoireBuild] result={report.summary.result} errors={report.summary.totalErrors} size={report.summary.totalSize}");
            if (Application.isBatchMode && report.summary.result != BuildResult.Succeeded)
            {
                EditorApplication.Exit(1);
            }
        }
    }
}
#endif

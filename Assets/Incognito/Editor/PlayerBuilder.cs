using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace Incognito.EditorTools
{
    /// <summary>
    /// U.INC-T27: the project had no scripted build entry point. Command-line Windows build
    /// (<c>-executeMethod Incognito.EditorTools.PlayerBuilder.BuildWindows</c>), driven by
    /// <c>tools/build.ps1</c>, the same pattern as Unity.Chain's and Unity.GridGame's own
    /// PlayerBuilder/GaidenBuild. Builds every enabled scene from
    /// <see cref="EditorBuildSettings"/> (Titlescreen, Street00, Street01, Shop, title first) so a
    /// scene added to Build Settings is picked up with no change here. Output goes to the
    /// gitignored <c>Builds/</c> folder.
    /// </summary>
    public static class PlayerBuilder
    {
        private const string OutputPath = "Builds/Windows/Incognito.exe";

        [MenuItem("Incognito/Build Windows Player")]
        public static void BuildWindows()
        {
            var scenePaths = EnabledScenePaths();
            if (scenePaths.Count == 0)
            {
                throw new InvalidOperationException(
                    "No enabled scenes in EditorBuildSettings; run Incognito/Build Room Scenes first.");
            }

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenePaths.ToArray(),
                locationPathName = OutputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            });

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new Exception(
                    $"Windows build failed: result={report.summary.result}, errors={report.summary.totalErrors}");
            }
        }

        private static List<string> EnabledScenePaths()
        {
            var scenes = EditorBuildSettings.scenes;
            var scenePaths = new List<string>(scenes.Length);
            foreach (var scene in scenes)
            {
                // Match a normal Editor build: a disabled entry stays in the list for later, but
                // is not part of the build.
                if (scene.enabled)
                {
                    scenePaths.Add(scene.path);
                }
            }

            return scenePaths;
        }
    }
}

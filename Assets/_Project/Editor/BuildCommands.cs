using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace AutismVR.Editor
{
    // Entry point for `unity build --execute-method AutismVR.Editor.BuildCommands.BuildQuest -o <apk>`.
    public static class BuildCommands
    {
        public const string DefaultOutputPath = "Builds/Quest/AutismVR.apk";

        public static void BuildQuest()
        {
            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
                locationPathName = OutputPathFromCommandLine() ?? DefaultOutputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
            };
            EditorUserBuildSettings.buildAppBundle = false;

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException(
                    $"Quest build {report.summary.result} with {report.summary.totalErrors} error(s)");
        }

        static string OutputPathFromCommandLine()
        {
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, "-buildOutput");
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }
    }
}

using System;
using System.Linq;
using Unity.Pipeline.Commands;
using UnityEditor.PackageManager.UI;

namespace AutismVR.Editor
{
    public static class SampleImportCommands
    {
        static readonly (string package, string version, string sample)[] RequiredSamples =
        {
            ("com.unity.xr.interaction.toolkit", "3.6.1", "Starter Assets"),
            ("com.unity.xr.interaction.toolkit", "3.6.1", "Hands Interaction Demo"),
            ("com.unity.xr.interaction.toolkit", "3.6.1", "XR Interaction Simulator"),
            ("com.unity.xr.hands", "1.9.0", "HandVisualizer"),
        };

        [CliCommand("autismvr_import_xr_samples", "Import the XRI and XR Hands samples the Room scene uses")]
        public static string ImportXrSamples()
        {
            foreach (var (package, version, sampleName) in RequiredSamples)
            {
                var sample = Sample.FindByPackage(package, version).FirstOrDefault(s => s.displayName == sampleName);
                if (string.IsNullOrEmpty(sample.displayName))
                    throw new InvalidOperationException($"Sample {sampleName} not found in {package}@{version}");
                if (!sample.isImported)
                    sample.Import(Sample.ImportOptions.HideImportWindow);
            }
            return string.Join(", ", RequiredSamples.Select(s => s.sample));
        }
    }
}

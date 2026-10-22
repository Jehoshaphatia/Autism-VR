using System.IO;
using System.Linq;
using System.Xml;
using UnityEditor.Android;

namespace AutismVR.Editor
{
    // Store-facing feature flags: eye tracking is optional (Quest 2 and Quest 3 have none; Meta VR
    // Glasses use it), and hand tracking is required because the app has no controller support.
    public class QuestManifestPostprocessor : IPostGenerateGradleAndroidProject
    {
        const string AndroidNs = "http://schemas.android.com/apk/res/android";
        const string ToolsNs = "http://schemas.android.com/tools";
        const string EyeTrackingFeature = "oculus.software.eye_tracking";
        const string HandTrackingFeature = "oculus.software.handtracking";

        // After the OpenXR and XR Hands manifest hooks.
        public int callbackOrder => 10000;

        public void OnPostGenerateGradleAndroidProject(string unityLibraryPath)
        {
            foreach (var module in new[] { unityLibraryPath, Path.Combine(unityLibraryPath, "..", "launcher") })
            {
                var manifestPath = Path.Combine(module, "src", "main", "AndroidManifest.xml");
                if (!File.Exists(manifestPath))
                    continue;

                var doc = new XmlDocument();
                doc.Load(manifestPath);
                var changed = MakeEyeTrackingOptional(doc);
                changed |= RequireHandTracking(doc);
                if (changed)
                    doc.Save(manifestPath);
            }
        }

        // The required entry is contributed by a library manifest during Gradle's manifest merge, after
        // every Unity hook has run, so declare it here with tools:replace to win that merge.
        public static bool RequireHandTracking(XmlDocument manifest) => SetFeatureRequired(manifest, HandTrackingFeature, true);

        public static bool MakeEyeTrackingOptional(XmlDocument manifest) => SetFeatureRequired(manifest, EyeTrackingFeature, false);

        // Library manifests contribute these uses-feature entries during Gradle's manifest merge, after
        // every Unity hook has run, so declare them here with tools:replace to win that merge.
        static bool SetFeatureRequired(XmlDocument manifest, string featureName, bool required)
        {
            var root = manifest.DocumentElement;
            var value = required ? "true" : "false";
            var feature = manifest.GetElementsByTagName("uses-feature").Cast<XmlElement>()
                .FirstOrDefault(element => element.GetAttribute("name", AndroidNs) == featureName);
            var changed = false;

            if (feature == null)
            {
                feature = manifest.CreateElement("uses-feature");
                feature.SetAttribute("name", AndroidNs, featureName);
                root.PrependChild(feature);
                changed = true;
            }
            if (feature.GetAttribute("required", AndroidNs) != value)
            {
                feature.SetAttribute("required", AndroidNs, value);
                changed = true;
            }
            if (feature.GetAttribute("replace", ToolsNs) != "android:required")
            {
                if (!root.HasAttribute("xmlns:tools"))
                    root.SetAttribute("xmlns:tools", ToolsNs);
                feature.SetAttribute("replace", ToolsNs, "android:required");
                changed = true;
            }
            return changed;
        }
    }
}

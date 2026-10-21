using System.IO;
using System.Linq;
using System.Xml;
using UnityEditor.Android;

namespace AutismVR.Editor
{
    // The OpenXR package marks oculus.software.eye_tracking as required whenever eye gaze is
    // enabled and Quest Pro is a target device. Eye gaze is optional here (Meta VR Glasses and
    // Quest Pro use it; Quest 2 and Quest 3 have no eye tracking), so a required entry would hide
    // the app from Quest 2 and Quest 3 in the store.
    public class QuestManifestPostprocessor : IPostGenerateGradleAndroidProject
    {
        const string AndroidNs = "http://schemas.android.com/apk/res/android";
        const string ToolsNs = "http://schemas.android.com/tools";
        const string EyeTrackingFeature = "oculus.software.eye_tracking";

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
                if (MakeEyeTrackingOptional(doc))
                    doc.Save(manifestPath);
            }
        }

        // The required entry is contributed by a library manifest during Gradle's manifest merge, after
        // every Unity hook has run, so declare it here with tools:replace to win that merge.
        public static bool MakeEyeTrackingOptional(XmlDocument manifest)
        {
            var root = manifest.DocumentElement;
            var feature = manifest.GetElementsByTagName("uses-feature").Cast<XmlElement>()
                .FirstOrDefault(element => element.GetAttribute("name", AndroidNs) == EyeTrackingFeature);
            var changed = false;

            if (feature == null)
            {
                feature = manifest.CreateElement("uses-feature");
                feature.SetAttribute("name", AndroidNs, EyeTrackingFeature);
                root.PrependChild(feature);
                changed = true;
            }
            if (feature.GetAttribute("required", AndroidNs) != "false")
            {
                feature.SetAttribute("required", AndroidNs, "false");
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

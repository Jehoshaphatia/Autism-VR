using System.Xml;
using AutismVR.Editor;
using NUnit.Framework;

namespace AutismVR.Tests.EditMode
{
    public class AndroidManifestTests
    {
        const string AndroidNs = "http://schemas.android.com/apk/res/android";
        const string ToolsNs = "http://schemas.android.com/tools";

        static XmlDocument Manifest(string required)
        {
            var doc = new XmlDocument();
            doc.LoadXml($@"<manifest xmlns:android=""{AndroidNs}"">
  <uses-feature android:name=""oculus.software.eye_tracking"" android:required=""{required}"" />
  <uses-feature android:name=""oculus.software.handtracking"" android:required=""false"" />
</manifest>");
            return doc;
        }

        static string RequiredOf(XmlDocument doc, string feature)
        {
            foreach (XmlElement element in doc.GetElementsByTagName("uses-feature"))
                if (element.GetAttribute("name", AndroidNs) == feature)
                    return element.GetAttribute("required", AndroidNs);
            return null;
        }

        // Quest 2 and Quest 3 have no eye tracking; a required feature hides the app from them.
        [Test]
        public void MakeEyeTrackingOptional_RequiredEntry_BecomesOptional()
        {
            var doc = Manifest("true");

            Assert.That(QuestManifestPostprocessor.MakeEyeTrackingOptional(doc), Is.True);
            Assert.That(RequiredOf(doc, "oculus.software.eye_tracking"), Is.EqualTo("false"));
        }

        [Test]
        public void MakeEyeTrackingOptional_LeavesOtherFeaturesAlone()
        {
            var doc = Manifest("true");

            QuestManifestPostprocessor.MakeEyeTrackingOptional(doc);

            Assert.That(RequiredOf(doc, "oculus.software.handtracking"), Is.EqualTo("false"));
        }

        // The required entry arrives later, from a library manifest during Gradle's merge, so the
        // app manifest must declare it optional with tools:replace to win the merge.
        [Test]
        public void MakeEyeTrackingOptional_NoEntry_AddsOptionalEntryThatOverridesLibraries()
        {
            var doc = new XmlDocument();
            doc.LoadXml($@"<manifest xmlns:android=""{AndroidNs}""><application /></manifest>");

            Assert.That(QuestManifestPostprocessor.MakeEyeTrackingOptional(doc), Is.True);
            Assert.That(RequiredOf(doc, "oculus.software.eye_tracking"), Is.EqualTo("false"));
            var entry = (XmlElement)doc.GetElementsByTagName("uses-feature")[0];
            Assert.That(entry.GetAttribute("replace", ToolsNs), Is.EqualTo("android:required"));
        }

        [Test]
        public void MakeEyeTrackingOptional_AlreadyOptionalWithReplace_ReportsNoChange()
        {
            var doc = new XmlDocument();
            doc.LoadXml($@"<manifest xmlns:android=""{AndroidNs}"" xmlns:tools=""{ToolsNs}"">
  <uses-feature android:name=""oculus.software.eye_tracking"" android:required=""false"" tools:replace=""android:required"" />
</manifest>");

            Assert.That(QuestManifestPostprocessor.MakeEyeTrackingOptional(doc), Is.False);
        }
    }
}

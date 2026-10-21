using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AutismVR.Tests.EditMode
{
    public class ProjectBaselineTests
    {
        static readonly Regex RemovedSdkReference =
            new Regex(@"\b(Gvr[A-Z]\w*|GoogleVR|OVR[A-Z]\w*|VRTK|Valve\.VR|IBM\.Watson|MORPH3D)\b");

        [Test]
        public void EditorIsUnity6000_6()
        {
            Assert.That(Application.unityVersion, Does.StartWith("6000.6."));
        }

        [Test]
        public void AssetsAreSerializedAsText()
        {
            Assert.That(EditorSettings.serializationMode, Is.EqualTo(SerializationMode.ForceText));
        }

        [Test]
        public void MetaFilesAreVisible()
        {
            Assert.That(VersionControlSettings.mode, Is.EqualTo("Visible Meta Files"));
        }

        [TestCase("Assets/GoogleVR")]
        [TestCase("Assets/Oculus")]
        [TestCase("Assets/SpeechSandbox")]
        [TestCase("Assets/Watson")]
        [TestCase("Assets/MORPH3D")]
        [TestCase("Assets/TextMesh Pro")]
        public void LegacySdkFolderIsGone(string folder)
        {
            Assert.That(AssetDatabase.IsValidFolder(folder), Is.False);
        }

        [Test]
        public void NoScriptReferencesRemovedSdks()
        {
            // The tests folder is skipped because this file spells out the SDK names.
            var offendingPaths = Directory.GetFiles("Assets", "*.cs", SearchOption.AllDirectories)
                .Where(path => !path.Replace('\\', '/').StartsWith("Assets/_Project/Tests/"))
                .Where(path => RemovedSdkReference.IsMatch(File.ReadAllText(path)))
                .ToList();

            Assert.That(offendingPaths, Is.Empty, string.Join("\n", offendingPaths));
        }
    }
}

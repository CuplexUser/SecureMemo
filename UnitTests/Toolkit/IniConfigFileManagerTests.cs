using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SecureMemo.Toolkit.ConfigHelper;
using UnitTests.TestSupport;

namespace UnitTests.Toolkit
{
    [TestClass]
    public class IniConfigFileManagerTests
    {
        private TempDirectory _folder;

        [TestInitialize]
        public void Setup()
        {
            _folder = new TempDirectory("sm_ini_");
        }

        [TestCleanup]
        public void Cleanup()
        {
            _folder.Dispose();
        }

        [TestMethod]
        public void SaveThenLoad_RoundTripsSectionItems()
        {
            string path = _folder.Combine("test.ini");
            var writer = new IniConfigFileManager();
            writer.ConfigurationData.ConfigSections.Add("General", new IniConfigFileSection());
            writer.ConfigurationData.ConfigSections["General"].ConfigItems["Name"] = "Secure Memo";
            writer.SaveConfigFile(path);

            var reader = new IniConfigFileManager();
            Assert.IsTrue(reader.LoadConfigFile(path));

            Assert.AreEqual("Secure Memo", reader.ConfigurationData.ConfigSections["General"].ConfigItems["Name"]);
        }

        [TestMethod]
        public void Load_ValueContainingEqualsSign_KeepsTheWholeValue()
        {
            string path = _folder.Combine("test.ini");
            File.WriteAllLines(path, new[] {"[Paths]", "Sync=C:\\a=b\\c"});

            var reader = new IniConfigFileManager();
            reader.LoadConfigFile(path);

            Assert.AreEqual("C:\\a=b\\c", reader.ConfigurationData.ConfigSections["Paths"].ConfigItems["Sync"]);
        }

        [TestMethod]
        public void Load_IgnoresLinesThatAreNotSectionsOrItems()
        {
            string path = _folder.Combine("test.ini");
            File.WriteAllLines(path, new[] {"; comment", "[General]", "not an item", "Key=Value", "Empty=", "[General]", "Other=1"});

            var reader = new IniConfigFileManager();
            Assert.IsTrue(reader.LoadConfigFile(path));

            IniConfigItemCollection items = reader.ConfigurationData.ConfigSections["General"].ConfigItems;
            Assert.AreEqual("Value", items["Key"]);
            Assert.AreEqual("1", items["Other"], "A repeated section header continues the same section");
            Assert.AreEqual("", items["Empty"]);
        }

        [TestMethod]
        public void ConfigItems_UnknownKey_ReturnsEmptyString()
        {
            var items = new IniConfigItemCollection();

            Assert.AreEqual("", items["Missing"]);
        }

        [TestMethod]
        public void Load_MissingOrNonIniFile_ReturnsFalse()
        {
            string txtPath = _folder.Combine("test.txt");
            File.WriteAllText(txtPath, "[General]");

            Assert.IsFalse(new IniConfigFileManager().LoadConfigFile(_folder.Combine("missing.ini")));
            Assert.IsFalse(new IniConfigFileManager().LoadConfigFile(txtPath));
        }

        [TestMethod]
        public void Save_NonIniFileName_WritesNothing()
        {
            string path = _folder.Combine("test.txt");

            new IniConfigFileManager().SaveConfigFile(path);

            Assert.IsFalse(File.Exists(path));
        }
    }
}

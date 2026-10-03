using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SecureMemo.DataModels;
using SecureMemo.Toolkit.Utility;
using SecureMemo.Utility;
using UnitTests.TestSupport;

namespace UnitTests.Utility
{
    [TestClass]
    public class ConfigHelperTests
    {
        [TestMethod]
        public void GetDefaultSettings_CreatesAFreshSaltThatLoadSettingsAccepts()
        {
            SecureMemoAppSettings first = ConfigHelper.GetDefaultSettings();
            SecureMemoAppSettings second = ConfigHelper.GetDefaultSettings();

            Assert.AreEqual(1024, first.ApplicationSaltValue.Length);
            Assert.AreNotEqual(first.ApplicationSaltValue, second.ApplicationSaltValue);
            Assert.AreEqual(3, first.DefaultEmptyTabPages);
            Assert.IsFalse(first.AlwaysOnTop);
            Assert.IsNull(first.PasswordDerivedString);
            Assert.AreEqual(first.FontSettings.FontFamily.Name, first.FontSettings.FontFamilyName);
        }

        [TestMethod]
        public void AssemblyTitle_IsTheApplicationName()
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(ConfigHelper.AssemblyTitle));
        }

        [TestMethod]
        public void IsValidDirectory_OnlyAcceptsExistingDirectories()
        {
            using var folder = new TempDirectory();

            Assert.IsTrue(FileSystem.IsValidDirectory(folder.Path));
            Assert.IsFalse(FileSystem.IsValidDirectory(Path.Combine(folder.Path, "missing")));
            Assert.IsFalse(FileSystem.IsValidDirectory(" "));
            Assert.IsFalse(FileSystem.IsValidDirectory(null));
        }
    }
}

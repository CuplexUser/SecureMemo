using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SecureMemo.DataModels;
using SecureMemo.Services;
using SecureMemo.Toolkit.ConfigHelper;
using SecureMemo.Utility;
using UnitTests.TestSupport;

namespace UnitTests.Services
{
    [TestClass]
    public class AppSettingsServiceTests
    {
        private TempDirectory _settingsFolder;
        private string _iniFilePath;

        [TestInitialize]
        public void Setup()
        {
            _settingsFolder = new TempDirectory("sm_settings_");
            _iniFilePath = _settingsFolder.Combine("ApplicationSettings.ini");
        }

        [TestCleanup]
        public void Cleanup()
        {
            _settingsFolder.Dispose();
        }

        private AppSettingsService CreateService()
        {
            return new AppSettingsService(ConfigHelper.GetDefaultSettings(), new IniConfigFileManager(), _iniFilePath);
        }

        private string ValidSaltLine()
        {
            return "ApplicationSaltValue=" + new string('A', 1024);
        }

        [TestMethod]
        public void LoadSettings_NoSettingsFile_WritesTheDefaults()
        {
            AppSettingsService service = CreateService();

            service.LoadSettings();

            Assert.IsTrue(File.Exists(_iniFilePath));
            StringAssert.Contains(File.ReadAllText(_iniFilePath), "DefaultEmptyTabPages=3");
        }

        [TestMethod]
        public void SaveThenLoad_RoundTripsEverySetting()
        {
            AppSettingsService writer = CreateService();
            writer.Settings.DefaultEmptyTabPages = 5;
            writer.Settings.AlwaysOnTop = true;
            writer.Settings.PasswordDerivedString = "ABCDEF0123";
            writer.Settings.MainWindowWith = 640;
            writer.Settings.MainWindowHeight = 480;
            writer.Settings.UseSharedSyncFolder = true;
            writer.Settings.SyncFolderPath = @"D:\Dropbox\SecureMemo";
            writer.Settings.FontSettings = new SecureMemoFontSettings {FontFamilyName = "Courier New", FontSize = 10.5f, Style = FontStyle.Bold | FontStyle.Italic};
            writer.SaveSettings();

            AppSettingsService reader = CreateService();
            reader.LoadSettings();

            Assert.AreEqual(5, reader.Settings.DefaultEmptyTabPages);
            Assert.AreEqual(writer.Settings.ApplicationSaltValue, reader.Settings.ApplicationSaltValue);
            Assert.AreEqual("ABCDEF0123", reader.Settings.PasswordDerivedString);
            Assert.IsTrue(reader.Settings.AlwaysOnTop);
            Assert.AreEqual(640, reader.Settings.MainWindowWith);
            Assert.AreEqual(480, reader.Settings.MainWindowHeight);
            Assert.IsTrue(reader.Settings.UseSharedSyncFolder);
            Assert.AreEqual(@"D:\Dropbox\SecureMemo", reader.Settings.SyncFolderPath);
            Assert.AreEqual("Courier New", reader.Settings.FontSettings.FontFamilyName);
            Assert.AreEqual(10.5f, reader.Settings.FontSettings.FontSize);
            Assert.AreEqual(FontStyle.Bold | FontStyle.Italic, reader.Settings.FontSettings.Style);
        }

        [TestMethod]
        public void SaveThenLoad_SyncFolderPathContainingEqualsSign_IsKeptWhole()
        {
            // Regression test: INI values were split on every '=', so anything after a second '='
            // in a value (here, a folder name) was silently dropped on load.
            AppSettingsService writer = CreateService();
            writer.Settings.SyncFolderPath = @"C:\Users\me\Sync=Shared";
            writer.SaveSettings();

            AppSettingsService reader = CreateService();
            reader.LoadSettings();

            Assert.AreEqual(@"C:\Users\me\Sync=Shared", reader.Settings.SyncFolderPath);
        }

        [TestMethod]
        public void LoadSettings_WindowSizeOutsideLimits_IsClampedBetweenDefaultsAndScreenSize()
        {
            AppSettingsService writer = CreateService();
            writer.Settings.MainWindowWith = 10;
            writer.Settings.MainWindowHeight = 100000;
            writer.SaveSettings();

            AppSettingsService reader = CreateService();
            reader.LoadSettings();

            Assert.AreEqual(400, reader.Settings.MainWindowWith);
            Assert.AreEqual(Screen.PrimaryScreen.WorkingArea.Height, reader.Settings.MainWindowHeight);
        }

        [TestMethod]
        public void LoadSettings_SettingsFromBeforeSyncAndFontOptionsExisted_UseDefaults()
        {
            File.WriteAllLines(_iniFilePath, new[]
            {
                "[General]",
                "DefaultEmptyTabPages=2",
                ValidSaltLine(),
                "PasswordDerivedString=0011",
                "AlwaysOnTop=False",
                "MainWindowWith=500",
                "MainWindowHeight=400"
            });

            AppSettingsService service = CreateService();
            service.LoadSettings();

            Assert.AreEqual(2, service.Settings.DefaultEmptyTabPages);
            Assert.IsFalse(service.Settings.UseSharedSyncFolder);
            Assert.AreEqual("", service.Settings.SyncFolderPath);
            Assert.IsNotNull(service.Settings.FontSettings.FontFamily);
        }

        [TestMethod]
        public void LoadSettings_SaltOfWrongLength_Throws()
        {
            File.WriteAllLines(_iniFilePath, new[]
            {
                "[General]",
                "DefaultEmptyTabPages=3",
                "ApplicationSaltValue=TooShort",
                "AlwaysOnTop=False"
            });

            Assert.ThrowsExactly<Exception>(() => CreateService().LoadSettings());
        }

        [TestMethod]
        public void LoadSettings_UnreadableFile_Throws()
        {
            // An existing path that IniConfigFileManager refuses (it only reads *.ini files).
            var service = new AppSettingsService(ConfigHelper.GetDefaultSettings(), new IniConfigFileManager(), _settingsFolder.Combine("settings.txt"));
            File.WriteAllText(_settingsFolder.Combine("settings.txt"), "[General]");

            Assert.ThrowsExactly<Exception>(() => service.LoadSettings());
        }
    }
}

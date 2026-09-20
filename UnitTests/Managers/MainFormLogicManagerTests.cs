using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SecureMemo.Toolkit.ConfigHelper;
using SecureMemo.Toolkit.Storage.Memory;
using SecureMemo.Managers;
using SecureMemo.Services;
using SecureMemo.Utility;

namespace UnitTests.Managers
{
    [TestClass]
    public class MainFormLogicManagerTests
    {
        private string _testDirectory;
        private AppSettingsService _appSettingsService;
        private MemoStorageService _memoStorageService;
        private PasswordStorage _passwordStorage;

        [TestInitialize]
        public void Setup()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), "sm_logicmgr_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testDirectory);

            _appSettingsService = new AppSettingsService(ConfigHelper.GetDefaultSettings(), new IniConfigFileManager(), Path.Combine(_testDirectory, "ApplicationSettings.ini"));
            _memoStorageService = new MemoStorageService(_appSettingsService, _testDirectory);
            _passwordStorage = new PasswordStorage();
            _passwordStorage.Set("SecureMemo", "TestPassword123!");
        }

        [TestCleanup]
        public void Cleanup()
        {
            _passwordStorage.Dispose();
            if (Directory.Exists(_testDirectory))
                Directory.Delete(_testDirectory, true);
        }

        private MainFormLogicManager CreateLogicManager()
        {
            return new MainFormLogicManager(_memoStorageService, new FileStorageService(), _passwordStorage, null, _appSettingsService);
        }

        [TestMethod]
        public void OpenDatabase_AfterSave_RetrievesPersistedContentInNewInstance()
        {
            // Simulates closing and reopening the app: a fresh MainFormLogicManager backed by the same on-disk database.
            MainFormLogicManager writer = CreateLogicManager();
            writer.CreateNewDatabase();
            writer.SetTabPageText(0, "content written before closing");
            writer.SaveDatabase();

            MainFormLogicManager reader = CreateLogicManager();
            bool opened = reader.OpenDatabase();

            Assert.IsTrue(opened);
            Assert.AreEqual(3, reader.PageCount);
            Assert.AreEqual("content written before closing", reader.GetTabPageText(0));
        }

        [TestMethod]
        public void OpenDatabase_NoExistingDatabase_ReturnsFalse()
        {
            MainFormLogicManager reader = CreateLogicManager();

            Assert.IsFalse(reader.OpenDatabase());
        }
    }
}

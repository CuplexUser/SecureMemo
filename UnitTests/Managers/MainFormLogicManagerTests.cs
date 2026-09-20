using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SecureMemo.Toolkit.ConfigHelper;
using SecureMemo.Toolkit.Storage.Memory;
using SecureMemo.DataModels;
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

        [TestMethod]
        public async Task RemoveTabPageAsync_KeepsRemainingTabsWithCorrectContent()
        {
            // Regression test: ReindexTabPageCollectionAfterRemovalAsync used to compute its loop
            // bound from TabPageDictionary.Count *after* clearing the dictionary (always 0), so the
            // repopulation loop never ran and removing any single tab wiped every tab.
            MainFormLogicManager logicManager = CreateLogicManager();
            logicManager.CreateNewDatabase();
            logicManager.SetTabPageText(0, "first");
            logicManager.SetTabPageText(1, "second");
            logicManager.SetTabPageText(2, "third");

            bool removed = await logicManager.RemoveTabPageAsync(1);

            Assert.IsTrue(removed);
            Assert.AreEqual(2, logicManager.PageCount);
            Assert.AreEqual("first", logicManager.GetTabPageText(0));
            Assert.AreEqual("third", logicManager.GetTabPageText(1));
        }

        [TestMethod]
        public void ReplaceTabPageCollection_AppliesNewOrderAndContent()
        {
            // Regression test: FormTabEdit's "Manage Tabs" dialog used to only mutate its own local
            // copy of the tab list; clicking OK never applied add/remove/reorder edits to the real
            // collection MainFormLogicManager and the UI actually read from.
            MainFormLogicManager logicManager = CreateLogicManager();
            logicManager.CreateNewDatabase();
            logicManager.SetTabPageText(0, "first");
            logicManager.SetTabPageText(1, "second");
            logicManager.SetTabPageText(2, "third");

            // Simulate the dialog: drop "second", reorder so "third" comes before "first", and add a new page.
            var reordered = new List<TabPageData>
            {
                new TabPageData {TabPageLabel = "Page3", TabPageText = "third", UniqueId = Guid.NewGuid().ToString()},
                new TabPageData {TabPageLabel = "Page1", TabPageText = "first", UniqueId = Guid.NewGuid().ToString()},
                new TabPageData {TabPageLabel = "NewPage", TabPageText = "brand new", UniqueId = Guid.NewGuid().ToString()}
            };

            logicManager.ReplaceTabPageCollection(reordered);

            Assert.AreEqual(3, logicManager.PageCount);
            Assert.AreEqual("third", logicManager.GetTabPageText(0));
            Assert.AreEqual("first", logicManager.GetTabPageText(1));
            Assert.AreEqual("brand new", logicManager.GetTabPageText(2));
        }

        [TestMethod]
        public void SaveToSharedFolder_ThenRestoreFromSync_RoundTripsCurrentContent()
        {
            // Regression test: SaveToSharedFolder used to copy whatever was already on disk instead
            // of the current in-memory state, and a successful RestoreBackupFromSyncFolder never
            // carried the password forward or reloaded the restored data into memory.
            string sharedFolder = Path.Combine(Path.GetTempPath(), "sm_synced_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(sharedFolder);
            try
            {
                _appSettingsService.Settings.SyncFolderPath = sharedFolder;
                _appSettingsService.Settings.UseSharedSyncFolder = true;
                _appSettingsService.Settings.PasswordDerivedString = "unit-test-derived-string";

                MainFormLogicManager writer = CreateLogicManager();
                writer.CreateNewDatabase();
                writer.SetTabPageText(0, "content before shared save (never explicitly saved locally)");

                _passwordStorage.Set("SharedFolderPassword", "TestPassword123!");
                bool saved = writer.SaveToSharedFolder();
                Assert.IsTrue(saved);

                // Simulate a different machine: wipe the local db, restore it from the shared folder.
                File.Delete(Path.Combine(_testDirectory, "MemoDatabase.dat"));

                MainFormLogicManager restorer = CreateLogicManager();
                _passwordStorage.Set("RestoreDatabaseFromSync", "TestPassword123!");
                RestoreSyncDataResult result = restorer.RestoreBackupFromSyncFolder();

                Assert.IsTrue(result.Successful, result.ErrorText);
                Assert.AreEqual("content before shared save (never explicitly saved locally)", restorer.GetTabPageText(0));
            }
            finally
            {
                Directory.Delete(sharedFolder, true);
            }
        }
    }
}

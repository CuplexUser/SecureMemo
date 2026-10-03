using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SecureMemo.FileStorageModels;
using SecureMemo.Toolkit.ConfigHelper;
using SecureMemo.Toolkit.Storage.Memory;
using SecureMemo.DataModels;
using SecureMemo.EventHandlers;
using SecureMemo.Managers;
using SecureMemo.Services;
using SecureMemo.Utility;

namespace UnitTests.Managers
{
    [TestClass]
    public class MainFormLogicManagerTests
    {
        private const string Password = "TestPassword123!";
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
            _passwordStorage.Set("SecureMemo", Password);
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
            return new MainFormLogicManager(_memoStorageService, CreateFileStorageService(), _passwordStorage, _appSettingsService);
        }

        private FileStorageService CreateFileStorageService()
        {
            return new FileStorageService(Path.Combine(_testDirectory, FileStorageService.ContainerFileName));
        }

        private void StoreFile(string fileName, string content)
        {
            FileStorageService fileStorage = CreateFileStorageService();
            fileStorage.Load(_passwordStorage.Get("SecureMemo"));
            fileStorage.FileSystem.AddFile(fileStorage.FileSystem.GetRootDirectory(), fileName, Encoding.UTF8.GetBytes(content));
            fileStorage.Save(_passwordStorage.Get("SecureMemo"));
        }

        private MainFormLogicManager CreateDatabaseWithPages(params string[] pageTexts)
        {
            MainFormLogicManager logicManager = CreateLogicManager();
            logicManager.CreateNewDatabase();
            for (int i = 0; i < pageTexts.Length; i++)
            {
                if (i >= logicManager.PageCount)
                    logicManager.AppendNewTabPage();

                logicManager.SetTabPageText(i, pageTexts[i]);
            }

            return logicManager;
        }

        [TestMethod]
        public void NewInstance_StartsWithTheConfiguredNumberOfEmptyPages()
        {
            _appSettingsService.Settings.DefaultEmptyTabPages = 4;

            MainFormLogicManager logicManager = CreateLogicManager();

            Assert.AreEqual(4, logicManager.PageCount);
            Assert.AreEqual(0, logicManager.ActivePageIndex);
            CollectionAssert.AreEqual(new[] {"Page1", "Page2", "Page3", "Page4"}, Enumerable.Range(0, 4).Select(logicManager.GetTabPageLabel).ToArray());
        }

        [TestMethod]
        public void HasExistingDatabase_ReflectsADatabaseCreatedAfterTheFirstCheck()
        {
            // Regression test: the first answer was cached for the lifetime of the manager, so after
            // creating a database on a fresh install FormMain kept treating it as missing and disabled
            // Save the next time it rebuilt the tabs (e.g. after adding a tab).
            MainFormLogicManager logicManager = CreateLogicManager();
            Assert.IsFalse(logicManager.HasExistingDatabase);

            logicManager.CreateNewDatabase();

            Assert.IsTrue(logicManager.HasExistingDatabase);
        }

        [TestMethod]
        public void CreateNewDatabase_SavesDatabaseAndRaisesNewDatabaseCreated()
        {
            MainFormLogicManager logicManager = CreateLogicManager();
            var changes = new List<TabPageCollectionStateChange>();
            logicManager.OnTabPageCollectionChange += (_, e) => changes.Add(e.ActiveChange);

            logicManager.CreateNewDatabase();

            Assert.IsTrue(_memoStorageService.DatabaseExists());
            CollectionAssert.AreEqual(new[] {TabPageCollectionStateChange.NewDatabaseCreated}, changes);
        }

        [TestMethod]
        public void OpenDatabase_AfterSave_RetrievesPersistedContentInNewInstance()
        {
            // Simulates closing and reopening the app: a fresh MainFormLogicManager backed by the same on-disk database.
            MainFormLogicManager writer = CreateLogicManager();
            writer.CreateNewDatabase();
            writer.SetTabPageText(0, "content written before closing");
            writer.SetTabPageLabel(1, "Renamed");
            writer.SetActivePageIndex(2);
            writer.SaveDatabase();

            MainFormLogicManager reader = CreateLogicManager();
            bool opened = reader.OpenDatabase();

            Assert.IsTrue(opened);
            Assert.AreEqual(3, reader.PageCount);
            Assert.AreEqual("content written before closing", reader.GetTabPageText(0));
            Assert.AreEqual("Renamed", reader.GetTabPageLabel(1));
            Assert.AreEqual(2, reader.ActivePageIndex);
        }

        [TestMethod]
        public void OpenDatabase_NoExistingDatabase_ReturnsFalse()
        {
            MainFormLogicManager reader = CreateLogicManager();

            Assert.IsFalse(reader.OpenDatabase());
        }

        [TestMethod]
        public void OpenDatabase_WrongPassword_ReturnsFalseAndKeepsCurrentPages()
        {
            CreateDatabaseWithPages("secret").SaveDatabase();
            MainFormLogicManager reader = CreateLogicManager();
            reader.SetTabPageText(0, "unsaved");
            _passwordStorage.Set("SecureMemo", "WrongPassword1");

            Assert.IsFalse(reader.OpenDatabase());
            Assert.AreEqual("unsaved", reader.GetTabPageText(0));
        }

        [TestMethod]
        public void OpenDatabase_DatabaseWithBrokenPageIndex_IsRepairedAndSavedBack()
        {
            // A database whose page keys have a gap (0, 2) used to pass the integrity check, and then
            // reading page 1 threw KeyNotFoundException while FormMain built the tabs.
            var broken = new TabPageDataCollection();
            broken.TabPageDictionary.Add(0, new TabPageData {PageIndex = 0, TabPageLabel = "A", TabPageText = "first", UniqueId = Guid.NewGuid().ToString()});
            broken.TabPageDictionary.Add(2, new TabPageData {PageIndex = 2, TabPageLabel = "C", TabPageText = "third", UniqueId = Guid.NewGuid().ToString()});
            _memoStorageService.SaveTabPageCollection(broken, Password);

            MainFormLogicManager logicManager = CreateLogicManager();
            Assert.IsTrue(logicManager.OpenDatabase());

            Assert.AreEqual(2, logicManager.PageCount);
            Assert.AreEqual("first", logicManager.GetTabPageText(0));
            Assert.AreEqual("third", logicManager.GetTabPageText(1));

            // The repaired copy was written back, so the next load finds no errors.
            _memoStorageService.LoadTabPageCollection(Password);
            Assert.IsFalse(_memoStorageService.FoundDatabaseErrors);
        }

        [TestMethod]
        public void AppendNewTabPage_AddsAnActivePageLabeledLikeTheDefaultPages()
        {
            // Regression test: appended pages were labeled with the zero-based count and a space
            // ("Page 3" next to the default "Page3"), so the fourth tab duplicated the third's name.
            MainFormLogicManager logicManager = CreateLogicManager();
            var changes = new List<TabPageCollectionStateChange>();
            logicManager.OnTabPageCollectionChange += (_, e) => changes.Add(e.ActiveChange);

            logicManager.AppendNewTabPage();

            Assert.AreEqual(4, logicManager.PageCount);
            Assert.AreEqual(3, logicManager.ActivePageIndex);
            Assert.AreEqual("Page4", logicManager.GetTabPageLabel(3));
            Assert.IsNull(logicManager.GetTabPageText(3));
            CollectionAssert.AreEqual(new[] {TabPageCollectionStateChange.PageAdded}, changes);
        }

        [TestMethod]
        public void SetActivePageIndex_RaisesEventWithPreviousAndNewIndex()
        {
            MainFormLogicManager logicManager = CreateLogicManager();
            ActivatePageIndexChangedArgs raised = null;
            logicManager.OnActivePageIndexChange += (_, e) => raised = e;

            logicManager.SetActivePageIndex(2);

            Assert.AreEqual(2, logicManager.ActivePageIndex);
            Assert.AreEqual((0, 2), (raised.PreviousIndex, raised.CurrentIndex));
        }

        [TestMethod]
        public void PageAccessors_OutOfRangeIndex_Throw()
        {
            MainFormLogicManager logicManager = CreateLogicManager();

            Assert.ThrowsExactly<ArgumentException>(() => logicManager.SetActivePageIndex(3));
            Assert.ThrowsExactly<ArgumentException>(() => logicManager.SetActivePageIndex(-1));
            Assert.ThrowsExactly<ArgumentException>(() => logicManager.GetTabPageText(3));
            Assert.ThrowsExactly<ArgumentException>(() => logicManager.GetTabPageLabel(-1));
        }

        [TestMethod]
        public void SetActiveTabPageText_UpdatesOnlyTheActivePage()
        {
            MainFormLogicManager logicManager = CreateLogicManager();
            logicManager.SetActivePageIndex(1);

            logicManager.SetActiveTabPageText("pasted text");

            Assert.AreEqual("pasted text", logicManager.GetTabPageText(1));
            Assert.IsNull(logicManager.GetTabPageText(0));
        }

        [TestMethod]
        public async Task RemoveTabPageAsync_KeepsRemainingTabsWithCorrectContent()
        {
            // Regression test: ReindexTabPageCollectionAfterRemovalAsync used to compute its loop
            // bound from TabPageDictionary.Count *after* clearing the dictionary (always 0), so the
            // repopulation loop never ran and removing any single tab wiped every tab.
            MainFormLogicManager logicManager = CreateDatabaseWithPages("first", "second", "third");
            var changes = new List<TabPageCollectionStateChange>();
            logicManager.OnTabPageCollectionChange += (_, e) => changes.Add(e.ActiveChange);

            bool removed = await logicManager.RemoveTabPageAsync(1);

            Assert.IsTrue(removed);
            Assert.AreEqual(2, logicManager.PageCount);
            Assert.AreEqual("first", logicManager.GetTabPageText(0));
            Assert.AreEqual("third", logicManager.GetTabPageText(1));
            CollectionAssert.AreEqual(new[] {TabPageCollectionStateChange.PageRemoved}, changes);
        }

        [TestMethod]
        public async Task RemoveTabPageAsync_ActiveLastPage_MovesActiveIndexToTheNewLastPage()
        {
            MainFormLogicManager logicManager = CreateDatabaseWithPages("first", "second", "third");
            logicManager.SetActivePageIndex(2);

            await logicManager.RemoveTabPageAsync(2);

            Assert.AreEqual(1, logicManager.ActivePageIndex);
            Assert.AreEqual("second", logicManager.GetTabPageText(logicManager.ActivePageIndex));
        }

        [TestMethod]
        public async Task RemoveTabPageAsync_UnknownIndex_ReturnsFalseAndChangesNothing()
        {
            MainFormLogicManager logicManager = CreateLogicManager();

            Assert.IsFalse(await logicManager.RemoveTabPageAsync(7));
            Assert.AreEqual(3, logicManager.PageCount);
        }

        [TestMethod]
        public void GetTabPageDataCollection_ReturnsCopies_SoDialogEditsDoNotLeakIntoTheModel()
        {
            // Regression test: the Manage Tabs dialog renames and reorders the pages it gets from
            // here as the user works. It used to receive the live objects, so pressing Cancel still
            // kept every rename, and a reordered PageIndex made a later "delete tab" remove the wrong tab.
            MainFormLogicManager logicManager = CreateDatabaseWithPages("first", "second", "third");

            List<TabPageData> pages = logicManager.GetTabPageDataCollection();
            pages[0].TabPageLabel = "Renamed in dialog";
            pages[0].PageIndex = 2;
            pages[2].PageIndex = 0;

            Assert.AreEqual("Page1", logicManager.GetTabPageLabel(0));
            CollectionAssert.AreEqual(new[] {"first", "second", "third"}, pages.Select(p => p.TabPageText).ToArray());
        }

        [TestMethod]
        public async Task GetTabPageDataCollection_CancelledDialogEdits_DoNotAffectWhichTabIsRemoved()
        {
            MainFormLogicManager logicManager = CreateDatabaseWithPages("first", "second", "third");
            List<TabPageData> pages = logicManager.GetTabPageDataCollection();
            pages[0].PageIndex = 1;
            pages[1].PageIndex = 0;

            await logicManager.RemoveTabPageAsync(0);

            CollectionAssert.AreEqual(new[] {"second", "third"}, new[] {logicManager.GetTabPageText(0), logicManager.GetTabPageText(1)});
        }

        [TestMethod]
        public void ReplaceTabPageCollection_AppliesNewOrderAndContent()
        {
            // Regression test: FormTabEdit's "Manage Tabs" dialog used to only mutate its own local
            // copy of the tab list; clicking OK never applied add/remove/reorder edits to the real
            // collection MainFormLogicManager and the UI actually read from.
            MainFormLogicManager logicManager = CreateDatabaseWithPages("first", "second", "third");

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
        public void ReplaceTabPageCollection_FewerPagesThanActiveIndex_ClampsActiveIndex()
        {
            MainFormLogicManager logicManager = CreateLogicManager();
            logicManager.SetActivePageIndex(2);

            logicManager.ReplaceTabPageCollection(new List<TabPageData> {new TabPageData {TabPageLabel = "Only"}});

            Assert.AreEqual(0, logicManager.ActivePageIndex);
        }

        [TestMethod]
        public void ResetToDefaultDatabase_DiscardsPagesAndRestoresDefaults()
        {
            MainFormLogicManager logicManager = CreateDatabaseWithPages("first", "second", "third", "fourth");
            logicManager.SetActivePageIndex(3);

            logicManager.ResetToDefaultDatabase();

            Assert.AreEqual(3, logicManager.PageCount);
            Assert.AreEqual(0, logicManager.ActivePageIndex);
            Assert.IsNull(logicManager.GetTabPageText(0));
        }

        [TestMethod]
        public void CreateBackup_CopiesTheDatabaseIntoTheBackupFolder()
        {
            CreateDatabaseWithPages("backed up").CreateBackup();

            BackupFileInfo backup = _memoStorageService.GetBackupFiles().Single();
            StringAssert.EndsWith(backup.Name, "MemoDatabase.dat");
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

                _passwordStorage.Set("SharedFolderPassword", Password);
                bool saved = writer.SaveToSharedFolder();
                Assert.IsTrue(saved);
                Assert.IsNull(_passwordStorage.Get("SharedFolderPassword"), "The shared folder password should not be kept in memory");

                // Simulate a different machine: wipe the local db, restore it from the shared folder.
                File.Delete(Path.Combine(_testDirectory, "MemoDatabase.dat"));

                MainFormLogicManager restorer = CreateLogicManager();
                _passwordStorage.Set("RestoreDatabaseFromSync", Password);
                RestoreSyncDataResult result = restorer.RestoreBackupFromSyncFolder();

                Assert.IsTrue(result.Successful, result.ErrorText);
                Assert.AreEqual("content before shared save (never explicitly saved locally)", restorer.GetTabPageText(0));
                Assert.IsNull(_passwordStorage.Get("RestoreDatabaseFromSync"), "The restore password should not be kept in memory");
            }
            finally
            {
                Directory.Delete(sharedFolder, true);
            }
        }

        [TestMethod]
        public void ChangePassword_ReencryptsTheDatabaseAndTheStoredFiles()
        {
            MainFormLogicManager logicManager = CreateDatabaseWithPages("memo text");
            StoreFile("notes.txt", "stored file");

            logicManager.ChangePassword("NewPassword456!");

            Assert.AreEqual("NewPassword456!", _passwordStorage.Get("SecureMemo"));
            Assert.AreEqual("memo text", _memoStorageService.LoadTabPageCollection("NewPassword456!").TabPageDictionary[0].TabPageText);
            Assert.IsNull(_memoStorageService.LoadTabPageCollection(Password));

            FileStorageService fileStorage = CreateFileStorageService();
            fileStorage.Load("NewPassword456!");
            StorageFile file = fileStorage.FileSystem.GetFiles(StorageFileSystem.RootDirectoryId).Single();
            Assert.AreEqual("stored file", Encoding.UTF8.GetString(fileStorage.FileSystem.ReadFile(file.Id)));
        }

        [TestMethod]
        public void ChangePassword_StoredFilesCannotBeDecrypted_ChangesNothing()
        {
            MainFormLogicManager logicManager = CreateDatabaseWithPages("memo text");
            logicManager.SaveDatabase();
            StoreFile("notes.txt", "stored file");
            _passwordStorage.Set("SecureMemo", "NotTheRightOne1");

            Assert.Throws<CryptographicException>(() => logicManager.ChangePassword("NewPassword456!"));

            Assert.AreEqual("NotTheRightOne1", _passwordStorage.Get("SecureMemo"));
            Assert.AreEqual("memo text", _memoStorageService.LoadTabPageCollection(Password).TabPageDictionary[0].TabPageText);
            FileStorageService fileStorage = CreateFileStorageService();
            fileStorage.Load(Password);
            Assert.AreEqual(1, fileStorage.FileSystem.GetFiles(StorageFileSystem.RootDirectoryId).Count);
        }

        [TestMethod]
        public void ChangePassword_EmptyPassword_Throws()
        {
            Assert.ThrowsExactly<ArgumentException>(() => CreateLogicManager().ChangePassword(""));
        }

        [TestMethod]
        public void CreateNewDatabase_DeletesFilesStoredWithThePreviousDatabase()
        {
            CreateDatabaseWithPages("old");
            StoreFile("old.txt", "encrypted with the old password");
            Assert.IsTrue(CreateFileStorageService().ContainerExists);

            CreateLogicManager().CreateNewDatabase();

            Assert.IsFalse(CreateFileStorageService().ContainerExists);
        }

        [TestMethod]
        public void ExportStoredFiles_UsesTheDatabasePassword()
        {
            MainFormLogicManager logicManager = CreateDatabaseWithPages("memo");
            StoreFile("notes.txt", "stored file");
            string target = Path.Combine(_testDirectory, "export");

            Assert.IsTrue(logicManager.HasStoredFiles);
            Assert.AreEqual(1, logicManager.ExportStoredFiles(target));
            Assert.AreEqual("stored file", File.ReadAllText(Path.Combine(target, "notes.txt")));
        }

        [TestMethod]
        public void DeleteStoredFiles_RemovesTheContainer()
        {
            MainFormLogicManager logicManager = CreateDatabaseWithPages("memo");
            StoreFile("notes.txt", "stored file");

            logicManager.DeleteStoredFiles();

            Assert.IsFalse(logicManager.HasStoredFiles);
        }

        [TestMethod]
        public void SaveToSharedFolder_WithoutPassword_Throws()
        {
            Assert.ThrowsExactly<InvalidOperationException>(() => CreateLogicManager().SaveToSharedFolder());
        }

        [TestMethod]
        public void RestoreBackupFromSyncFolder_WithoutPassword_Throws()
        {
            Assert.ThrowsExactly<InvalidOperationException>(() => CreateLogicManager().RestoreBackupFromSyncFolder());
        }

        [TestMethod]
        public void RestoreBackupFromSyncFolder_SyncPasswordDiffersFromDatabasePassword_FailsWithoutChangingAnything()
        {
            // Regression test: the sync copy of the database keeps the database's own password. When
            // the sync password differed, the restore replaced the local database, reported success,
            // and left the previous pages in memory - so the next Save overwrote the restored data.
            string sharedFolder = Path.Combine(Path.GetTempPath(), "sm_synced_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(sharedFolder);
            try
            {
                _appSettingsService.Settings.SyncFolderPath = sharedFolder;
                _appSettingsService.Settings.PasswordDerivedString = "derived";
                MainFormLogicManager writer = CreateDatabaseWithPages("synced content");
                _passwordStorage.Set("SharedFolderPassword", "DifferentSyncPassw0rd");
                Assert.IsTrue(writer.SaveToSharedFolder());

                MainFormLogicManager local = CreateDatabaseWithPages("local content");
                local.SaveDatabase();
                _passwordStorage.Set("RestoreDatabaseFromSync", "DifferentSyncPassw0rd");

                RestoreSyncDataResult result = local.RestoreBackupFromSyncFolder();

                Assert.IsFalse(result.Successful);
                Assert.IsTrue(result.ErrorCode.HasFlag(RestoreSyncDataErrorCodes.MemoDatabaseFileParseError));
                Assert.IsFalse(string.IsNullOrEmpty(result.ErrorText));
                Assert.AreEqual("local content", local.GetTabPageText(0));
                Assert.AreEqual(Password, _passwordStorage.Get("SecureMemo"));
                Assert.AreEqual("local content", _memoStorageService.LoadTabPageCollection(Password).TabPageDictionary[0].TabPageText);
            }
            finally
            {
                Directory.Delete(sharedFolder, true);
            }
        }
    }
}

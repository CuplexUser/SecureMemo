using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SecureMemo.DataModels;
using SecureMemo.Services;
using SecureMemo.Toolkit.ConfigHelper;
using SecureMemo.Utility;
using UnitTests.TestSupport;

namespace UnitTests.Services
{
    [TestClass]
    public class MemoStorageServiceTests
    {
        private const string Password = "TestPassword123!";

        // MemoDatabase.dat written by the storage code as it was before the 2026-10 cleanup
        // (password "Fixture-Passw0rd"). Existing users' databases must keep opening, so this must
        // never be regenerated to make a failing test pass.
        private const string LegacyDatabaseBase64 =
            "6Cfan+NdbyInnQSUAu+3dkJL61LC/ytzMMW8HAfH4S7bYt0YGAcl9PVjILnkdnSs+Q27fOVFNhCmeCSc8/MIVKmpjhlbpxl/yC5iXQfh3kSo9w1ai28KjysHKYqF8p7f+VbZiBNMWCACuuIXvR8sFeNxW3epaCulcEU0K2xCdxDgn4amdFh/jqOVm5zKWLyCgXztAdGO+BJ5hyo/r1ZoHYY9TAce0QCniTyLpHVa2TclgFaj9ippgEWYDdaS2Y6KSSWiueAUQB5obH4BEauKtzw8OxQkE9reTOG1RpzT8ql3DI9wjKYQ4Fx8iP0eicpNo6Zz/9fEV+7VJsXiS53l6Q==";

        private TempDirectory _databaseFolder;
        private TempDirectory _syncFolder;
        private AppSettingsService _appSettingsService;
        private MemoStorageService _memoStorageService;

        [TestInitialize]
        public void Setup()
        {
            _databaseFolder = new TempDirectory("smdb_test_");
            _syncFolder = new TempDirectory("smdb_sync_");
            _appSettingsService = new AppSettingsService(ConfigHelper.GetDefaultSettings(), new IniConfigFileManager(), _databaseFolder.Combine("ApplicationSettings.ini"));
            _appSettingsService.Settings.SyncFolderPath = _syncFolder.Path;
            _appSettingsService.Settings.PasswordDerivedString = "derived-string";
            _memoStorageService = new MemoStorageService(_appSettingsService, _databaseFolder.Path);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _databaseFolder.Dispose();
            _syncFolder.Dispose();
        }

        private static TabPageDataCollection CreateCollection(params string[] pageTexts)
        {
            var collection = new TabPageDataCollection();
            for (int i = 0; i < pageTexts.Length; i++)
                collection.TabPageDictionary.Add(i, new TabPageData {PageIndex = i, TabPageLabel = "Page" + (i + 1), TabPageText = pageTexts[i], UniqueId = Guid.NewGuid().ToString()});

            return collection;
        }

        [TestMethod]
        public void SaveThenLoad_RoundTripsThroughEncryptionAndCompression()
        {
            TabPageDataCollection collection = CreateCollection("Hello world, this is a test memo!", "Second page content here.");
            collection.ActiveTabIndex = 1;

            bool saveResult = _memoStorageService.SaveTabPageCollection(collection, Password);
            Assert.IsTrue(saveResult);
            Assert.IsTrue(_memoStorageService.DatabaseExists());

            TabPageDataCollection loaded = _memoStorageService.LoadTabPageCollection(Password);

            Assert.IsNotNull(loaded, "A correctly-saved database should not fail to load");
            Assert.IsFalse(_memoStorageService.FoundDatabaseErrors);
            Assert.AreEqual(2, loaded.TabPageDictionary.Count);
            Assert.AreEqual(1, loaded.ActiveTabIndex);
            Assert.AreEqual("Hello world, this is a test memo!", loaded.TabPageDictionary[0].TabPageText);
            Assert.AreEqual("Second page content here.", loaded.TabPageDictionary[1].TabPageText);
        }

        [TestMethod]
        public void SaveThenLoad_LargeMemoSpanningSeveralCompressionBlocks_RoundTrips()
        {
            // Data is compressed in 2 MB blocks across several threads; a single big page must
            // survive being split and reassembled.
            var random = new Random(42);
            var sb = new StringBuilder();
            while (sb.Length < 5 * 1024 * 1024)
                sb.Append((char) ('a' + random.Next(26))).Append(random.Next(10) == 0 ? "\r\n" : "");

            string largeText = sb.ToString();
            _memoStorageService.SaveTabPageCollection(CreateCollection(largeText, "small"), Password);

            TabPageDataCollection loaded = _memoStorageService.LoadTabPageCollection(Password);

            Assert.AreEqual(largeText, loaded.TabPageDictionary[0].TabPageText);
            Assert.AreEqual("small", loaded.TabPageDictionary[1].TabPageText);
        }

        [TestMethod]
        public void Load_DatabaseWrittenByEarlierVersion_StillOpens()
        {
            File.WriteAllBytes(_databaseFolder.Combine("MemoDatabase.dat"), Convert.FromBase64String(LegacyDatabaseBase64));

            TabPageDataCollection loaded = _memoStorageService.LoadTabPageCollection("Fixture-Passw0rd");

            Assert.IsNotNull(loaded);
            Assert.AreEqual(1, loaded.ActiveTabIndex);
            Assert.AreEqual("Notes", loaded.TabPageDictionary[0].TabPageLabel);
            Assert.AreEqual("First page\r\nwith two lines", loaded.TabPageDictionary[0].TabPageText);
            Assert.AreEqual("4b0f6f0e-6f1a-4f5e-9d61-3c2b1f7a9e01", loaded.TabPageDictionary[0].UniqueId);
            Assert.AreEqual("Unicode", loaded.TabPageDictionary[1].TabPageLabel);
            Assert.AreEqual("Hej på dig – ÅÄÖ åäö", loaded.TabPageDictionary[1].TabPageText);
        }

        [TestMethod]
        public void Load_WithWrongPassword_DoesNotReturnOriginalContent()
        {
            _memoStorageService.SaveTabPageCollection(CreateCollection("Secret content"), "CorrectPassword!");

            TabPageDataCollection loaded = _memoStorageService.LoadTabPageCollection("WrongPassword!");

            Assert.IsNull(loaded);
        }

        [TestMethod]
        public void Load_DatabaseWithGapInPageIndexes_IsRepairedAndFlagged()
        {
            TabPageDataCollection collection = CreateCollection("first");
            collection.TabPageDictionary.Add(2, new TabPageData {PageIndex = 2, TabPageLabel = "C", TabPageText = "third"});
            _memoStorageService.SaveTabPageCollection(collection, Password);

            TabPageDataCollection loaded = _memoStorageService.LoadTabPageCollection(Password);

            Assert.IsTrue(_memoStorageService.FoundDatabaseErrors);
            CollectionAssert.AreEqual(new[] {0, 1}, loaded.TabPageDictionary.Keys.OrderBy(k => k).ToArray());
            Assert.AreEqual("third", loaded.TabPageDictionary[1].TabPageText);
        }

        [TestMethod]
        public void Save_WithoutPassword_FailsAndKeepsTheExistingDatabase()
        {
            _memoStorageService.SaveTabPageCollection(CreateCollection("keep me"), Password);

            Assert.IsFalse(_memoStorageService.SaveTabPageCollection(CreateCollection("lost"), null));
            Assert.AreEqual("keep me", _memoStorageService.LoadTabPageCollection(Password).TabPageDictionary[0].TabPageText);
        }

        [TestMethod]
        public void DatabaseExists_BeforeAnySave_ReturnsFalse()
        {
            Assert.IsFalse(_memoStorageService.DatabaseExists());
        }

        [TestMethod]
        public void MakeBackup_ThenRestoreBackup_RestoresOriginalContent()
        {
            _memoStorageService.SaveTabPageCollection(CreateCollection("original content"), Password);
            _memoStorageService.MakeBackup();
            _memoStorageService.SaveTabPageCollection(CreateCollection("overwritten content"), Password);

            var backups = _memoStorageService.GetBackupFiles().ToList();
            Assert.AreEqual(1, backups.Count);

            _memoStorageService.RestoreBackup(backups[0]);

            TabPageDataCollection restored = _memoStorageService.LoadTabPageCollection(Password);
            Assert.IsNotNull(restored);
            Assert.AreEqual("original content", restored.TabPageDictionary[0].TabPageText);
            Assert.IsFalse(File.Exists(backups[0].FullName), "A restored backup is moved back into place, not copied");
        }

        [TestMethod]
        public void GetBackupFiles_NoBackupFolder_ReturnsNull()
        {
            Assert.IsNull(_memoStorageService.GetBackupFiles());
        }

        [TestMethod]
        public void MakeBackup_DatabaseFolderMissing_Throws()
        {
            var service = new MemoStorageService(_appSettingsService, _databaseFolder.Combine("does-not-exist"));

            Assert.ThrowsExactly<Exception>(() => service.MakeBackup());
        }

        [TestMethod]
        public void RestoreBackup_MissingBackupFile_Throws()
        {
            var missing = new BackupFileInfo {Name = "gone.dat", FullName = _databaseFolder.Combine("gone.dat")};

            Assert.ThrowsExactly<Exception>(() => _memoStorageService.RestoreBackup(missing));
        }

        [TestMethod]
        public void SaveToSharedFolder_ThenRestore_AppliesSyncedSettingsAndDatabase()
        {
            _memoStorageService.SaveTabPageCollection(CreateCollection("synced"), Password);
            _appSettingsService.Settings.DefaultEmptyTabPages = 6;
            _appSettingsService.Settings.MainWindowHeight = 555;
            _appSettingsService.Settings.FontSettings = new SecureMemoFontSettings {FontFamilyName = "Courier New", FontSize = 9.5f, Style = FontStyle.Bold};
            string syncedSalt = _appSettingsService.Settings.ApplicationSaltValue;
            Assert.IsTrue(_memoStorageService.SaveTabPageCollectionToSharedFolder(null, Password));

            // A second machine with its own settings and database.
            using var otherMachine = new TempDirectory("smdb_other_");
            var otherSettings = new AppSettingsService(ConfigHelper.GetDefaultSettings(), new IniConfigFileManager(), otherMachine.Combine("ApplicationSettings.ini"));
            otherSettings.Settings.SyncFolderPath = _syncFolder.Path;
            var otherStorage = new MemoStorageService(otherSettings, otherMachine.Path);
            otherStorage.SaveTabPageCollection(CreateCollection("old local"), "OldPassword1");

            RestoreSyncDataResult result = otherStorage.RestoreBackupFromSyncFolder(Password);

            Assert.IsTrue(result.Successful, result.ErrorText);
            Assert.AreEqual(RestoreSyncDataErrorCodes.None, result.ErrorCode);
            Assert.AreEqual(syncedSalt, otherSettings.Settings.ApplicationSaltValue);
            Assert.AreEqual("derived-string", otherSettings.Settings.PasswordDerivedString);
            Assert.AreEqual(6, otherSettings.Settings.DefaultEmptyTabPages);
            Assert.AreEqual(555, otherSettings.Settings.MainWindowHeight);
            Assert.AreEqual("Courier New", otherSettings.Settings.FontSettings.FontFamilyName);
            Assert.AreEqual("synced", otherStorage.LoadTabPageCollection(Password).TabPageDictionary[0].TabPageText);
        }

        [TestMethod]
        public void SaveToSharedFolder_Twice_ReplacesTheEarlierSyncedCopy()
        {
            _memoStorageService.SaveTabPageCollection(CreateCollection("first"), Password);
            Assert.IsTrue(_memoStorageService.SaveTabPageCollectionToSharedFolder(null, Password));
            _memoStorageService.SaveTabPageCollection(CreateCollection("second"), Password);
            Assert.IsTrue(_memoStorageService.SaveTabPageCollectionToSharedFolder(null, Password));

            File.Delete(_databaseFolder.Combine("MemoDatabase.dat"));
            Assert.IsTrue(_memoStorageService.RestoreBackupFromSyncFolder(Password).Successful);

            Assert.AreEqual("second", _memoStorageService.LoadTabPageCollection(Password).TabPageDictionary[0].TabPageText);
        }

        [TestMethod]
        public void SaveToSharedFolder_SyncFolderMissing_ReturnsFalse()
        {
            _memoStorageService.SaveTabPageCollection(CreateCollection("x"), Password);
            _appSettingsService.Settings.SyncFolderPath = _syncFolder.Combine("missing");

            Assert.IsFalse(_memoStorageService.SaveTabPageCollectionToSharedFolder(null, Password));
        }

        [TestMethod]
        public void RestoreFromSync_EmptySyncFolder_ReportsMissingDatabase()
        {
            RestoreSyncDataResult result = _memoStorageService.RestoreBackupFromSyncFolder(Password);

            Assert.IsFalse(result.Successful);
            Assert.AreEqual(RestoreSyncDataErrorCodes.MemoDatabaseFileNotFound, result.ErrorCode);
            Assert.IsFalse(string.IsNullOrEmpty(result.ErrorText), "FormMain shows ErrorText to the user");
        }

        [TestMethod]
        public void RestoreFromSync_SettingsFileMissing_ReportsMissingSettings()
        {
            _memoStorageService.SaveTabPageCollection(CreateCollection("x"), Password);
            _memoStorageService.SaveTabPageCollectionToSharedFolder(null, Password);
            File.Delete(Path.Combine(_syncFolder.Path, "ApplicationSettings.dat"));

            RestoreSyncDataResult result = _memoStorageService.RestoreBackupFromSyncFolder(Password);

            Assert.IsFalse(result.Successful);
            Assert.AreEqual(RestoreSyncDataErrorCodes.ApplicationSettingsFileNotFound, result.ErrorCode);
            Assert.IsFalse(string.IsNullOrEmpty(result.ErrorText));
        }

        [TestMethod]
        public void RestoreFromSync_WrongPassword_FailsAndKeepsLocalDatabase()
        {
            _memoStorageService.SaveTabPageCollection(CreateCollection("synced"), Password);
            _memoStorageService.SaveTabPageCollectionToSharedFolder(null, Password);
            _memoStorageService.SaveTabPageCollection(CreateCollection("local"), Password);

            RestoreSyncDataResult result = _memoStorageService.RestoreBackupFromSyncFolder("WrongPassword1");

            Assert.IsFalse(result.Successful);
            Assert.IsFalse(string.IsNullOrEmpty(result.ErrorText));
            Assert.AreEqual("local", _memoStorageService.LoadTabPageCollection(Password).TabPageDictionary[0].TabPageText);
        }
    }
}

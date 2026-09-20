using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SecureMemo.DataModels;
using SecureMemo.Services;

namespace UnitTests.Services
{
    [TestClass]
    public class MemoStorageServiceTests
    {
        private string _testDirectory;
        private MemoStorageService _memoStorageService;

        [TestInitialize]
        public void Setup()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), "smdb_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testDirectory);
            _memoStorageService = new MemoStorageService(null, _testDirectory);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_testDirectory))
                Directory.Delete(_testDirectory, true);
        }

        [TestMethod]
        public void SaveThenLoad_RoundTripsThroughEncryptionAndCompression()
        {
            var collection = new TabPageDataCollection();
            collection.TabPageDictionary.Add(0, new TabPageData { PageIndex = 0, TabPageLabel = "Page1", TabPageText = "Hello world, this is a test memo!", UniqueId = Guid.NewGuid().ToString() });
            collection.TabPageDictionary.Add(1, new TabPageData { PageIndex = 1, TabPageLabel = "Page2", TabPageText = "Second page content here.", UniqueId = Guid.NewGuid().ToString() });
            collection.ActiveTabIndex = 1;

            bool saveResult = _memoStorageService.SaveTabPageCollection(collection, "TestPassword123!");
            Assert.IsTrue(saveResult);
            Assert.IsTrue(_memoStorageService.DatabaseExists());

            TabPageDataCollection loaded = _memoStorageService.LoadTabPageCollection("TestPassword123!");

            Assert.IsNotNull(loaded, "A correctly-saved database should not fail to load");
            Assert.IsFalse(_memoStorageService.FoundDatabaseErrors);
            Assert.AreEqual(2, loaded.TabPageDictionary.Count);
            Assert.AreEqual(1, loaded.ActiveTabIndex);
            Assert.AreEqual("Hello world, this is a test memo!", loaded.TabPageDictionary[0].TabPageText);
            Assert.AreEqual("Second page content here.", loaded.TabPageDictionary[1].TabPageText);
        }

        [TestMethod]
        public void Load_WithWrongPassword_DoesNotReturnOriginalContent()
        {
            var collection = new TabPageDataCollection();
            collection.TabPageDictionary.Add(0, new TabPageData { PageIndex = 0, TabPageLabel = "Page1", TabPageText = "Secret content", UniqueId = Guid.NewGuid().ToString() });

            _memoStorageService.SaveTabPageCollection(collection, "CorrectPassword!");

            TabPageDataCollection loaded = _memoStorageService.LoadTabPageCollection("WrongPassword!");

            Assert.IsNull(loaded);
        }

        [TestMethod]
        public void DatabaseExists_BeforeAnySave_ReturnsFalse()
        {
            Assert.IsFalse(_memoStorageService.DatabaseExists());
        }

        [TestMethod]
        public void MakeBackup_ThenRestoreBackup_RestoresOriginalContent()
        {
            var original = new TabPageDataCollection();
            original.TabPageDictionary.Add(0, new TabPageData { PageIndex = 0, TabPageLabel = "Page1", TabPageText = "original content", UniqueId = Guid.NewGuid().ToString() });
            _memoStorageService.SaveTabPageCollection(original, "TestPassword123!");
            _memoStorageService.MakeBackup();

            var overwritten = new TabPageDataCollection();
            overwritten.TabPageDictionary.Add(0, new TabPageData { PageIndex = 0, TabPageLabel = "Page1", TabPageText = "overwritten content", UniqueId = Guid.NewGuid().ToString() });
            _memoStorageService.SaveTabPageCollection(overwritten, "TestPassword123!");

            var backups = _memoStorageService.GetBackupFiles().ToList();
            Assert.AreEqual(1, backups.Count);

            _memoStorageService.RestoreBackup(backups[0]);

            TabPageDataCollection restored = _memoStorageService.LoadTabPageCollection("TestPassword123!");
            Assert.IsNotNull(restored);
            Assert.AreEqual("original content", restored.TabPageDictionary[0].TabPageText);
        }
    }
}

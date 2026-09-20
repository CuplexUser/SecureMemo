using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SecureMemo.FileStorageModels;

namespace UnitTests.DataModels
{
    [TestClass]
    public class FileStorageTests
    {
        private string _testDirectory;

        [TestInitialize]
        public void Setup()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), "smfs_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testDirectory);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_testDirectory))
                Directory.Delete(_testDirectory, true);
        }

        [TestMethod]
        public void SaveThenLoad_RoundTripsDirectoryAndFileStructure()
        {
            StorageFileSystem storageFileSystem = StorageFileSystem.CreateNewFileSystem();
            StorageDirectory root = storageFileSystem.GetRootDirectory();
            int subDirId = storageFileSystem.CreateDirectory(root, "Documents");
            StorageDirectory subDir = storageFileSystem.GetDirectory(subDirId);
            storageFileSystem.CreateFile(subDir, "notes.txt");

            storageFileSystem.SaveToFile(_testDirectory);
            StorageFileSystem loaded = StorageFileSystem.LoadFileSystem(_testDirectory);

            Assert.IsNotNull(loaded, "Loading the saved file system should not return null");

            StorageDirectory loadedSubDir = loaded.GetDirectory(subDirId);
            Assert.IsNotNull(loadedSubDir);
            Assert.AreEqual("Documents", loadedSubDir.DirectoryName);

            var filesInSubDir = loaded.GetFiles(subDirId);
            Assert.AreEqual(1, filesInSubDir.Count);
            Assert.AreEqual("notes.txt", filesInSubDir[0].FileName);
        }

        [TestMethod]
        public void LoadFileSystem_MissingFile_ReturnsNull()
        {
            StorageFileSystem loaded = StorageFileSystem.LoadFileSystem(_testDirectory);

            Assert.IsNull(loaded);
        }
    }
}

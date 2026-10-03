using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SecureMemo.FileStorageModels;
using SecureMemo.Services;
using UnitTests.TestSupport;

namespace UnitTests.Services
{
    [TestClass]
    public class FileStorageServiceTests
    {
        private const string Password = "TestPassword123!";
        private TempDirectory _folder;

        [TestInitialize]
        public void Setup()
        {
            _folder = new TempDirectory("sm_files_");
        }

        [TestCleanup]
        public void Cleanup()
        {
            _folder.Dispose();
        }

        private FileStorageService CreateService()
        {
            return new FileStorageService(_folder.Combine(FileStorageService.ContainerFileName));
        }

        private FileStorageService CreateServiceWithFile(string fileName, byte[] content)
        {
            FileStorageService service = CreateService();
            service.Load(Password);
            service.FileSystem.AddFile(service.FileSystem.GetRootDirectory(), fileName, content);
            service.Save(Password);
            return service;
        }

        [TestMethod]
        public void Load_NoContainerYet_StartsAnEmptyFileSystem()
        {
            FileStorageService service = CreateService();

            service.Load(Password);

            Assert.IsFalse(service.ContainerExists);
            Assert.AreEqual(0, service.FileSystem.GetFiles(StorageFileSystem.RootDirectoryId).Count);
        }

        [TestMethod]
        public void SaveThenLoad_RoundTripsFoldersAndFileContents()
        {
            var binary = new byte[300_000];
            new Random(7).NextBytes(binary);

            FileStorageService writer = CreateService();
            writer.Load(Password);
            int folderId = writer.FileSystem.CreateDirectory(writer.FileSystem.GetRootDirectory(), "Scans");
            writer.FileSystem.AddFile(writer.FileSystem.GetDirectory(folderId), "scan.bin", binary);
            writer.FileSystem.AddFile(writer.FileSystem.GetRootDirectory(), "empty.txt", Array.Empty<byte>());
            writer.Save(Password);

            FileStorageService reader = CreateService();
            reader.Load(Password);

            StorageFile scan = reader.FileSystem.GetFiles(folderId).Single();
            CollectionAssert.AreEqual(binary, reader.FileSystem.ReadFile(scan.Id));
            StorageFile empty = reader.FileSystem.GetFiles(StorageFileSystem.RootDirectoryId).Single();
            Assert.AreEqual(0, reader.FileSystem.ReadFile(empty.Id).Length);
        }

        [TestMethod]
        public void Container_IsEncrypted()
        {
            byte[] secret = Encoding.UTF8.GetBytes("plain text that must not appear on disk, plain text that must not appear on disk");
            CreateServiceWithFile("secret-name.txt", secret);

            string onDisk = Encoding.UTF8.GetString(File.ReadAllBytes(_folder.Combine(FileStorageService.ContainerFileName)));

            Assert.IsFalse(onDisk.Contains("secret-name"));
            Assert.IsFalse(onDisk.Contains("plain text"));
        }

        [TestMethod]
        public void Load_WrongPassword_ThrowsAndLeavesNothingLoaded()
        {
            CreateServiceWithFile("a.txt", Encoding.UTF8.GetBytes("a"));
            FileStorageService service = CreateService();

            Assert.Throws<CryptographicException>(() => service.Load("WrongPassword1"));
            Assert.IsNull(service.FileSystem);
        }

        [TestMethod]
        public void Load_CorruptContainer_ThrowsCryptographicException()
        {
            File.WriteAllBytes(_folder.Combine(FileStorageService.ContainerFileName), new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16});

            Assert.Throws<CryptographicException>(() => CreateService().Load(Password));
        }

        [TestMethod]
        public void LoadOrSave_WithoutPassword_Throws()
        {
            FileStorageService service = CreateService();
            Assert.ThrowsExactly<InvalidOperationException>(() => service.Load(null));

            service.Load(Password);
            Assert.ThrowsExactly<InvalidOperationException>(() => service.Save(""));
            Assert.IsFalse(service.ContainerExists);
        }

        [TestMethod]
        public void Save_NothingLoaded_Throws()
        {
            Assert.ThrowsExactly<InvalidOperationException>(() => CreateService().Save(Password));
        }

        [TestMethod]
        public void Save_DoesNotLeaveATemporaryFileBehind()
        {
            CreateServiceWithFile("a.txt", Encoding.UTF8.GetBytes("a"));

            CollectionAssert.AreEqual(new[] {FileStorageService.ContainerFileName}, Directory.GetFiles(_folder.Path).Select(Path.GetFileName).ToArray());
        }

        [TestMethod]
        public void Unload_DropsTheDecryptedFileSystem()
        {
            FileStorageService service = CreateServiceWithFile("a.txt", Encoding.UTF8.GetBytes("a"));

            service.Unload();

            Assert.IsNull(service.FileSystem);
            Assert.IsTrue(service.ContainerExists);
        }

        [TestMethod]
        public void ChangePassword_ContainerOpensOnlyWithTheNewPassword()
        {
            CreateServiceWithFile("a.txt", Encoding.UTF8.GetBytes("content"));

            CreateService().ChangePassword(Password, "NewPassword456!");

            FileStorageService reader = CreateService();
            Assert.Throws<CryptographicException>(() => reader.Load(Password));
            reader.Load("NewPassword456!");
            StorageFile file = reader.FileSystem.GetFiles(StorageFileSystem.RootDirectoryId).Single();
            Assert.AreEqual("content", Encoding.UTF8.GetString(reader.FileSystem.ReadFile(file.Id)));
        }

        [TestMethod]
        public void ChangePassword_NoContainer_DoesNothing()
        {
            FileStorageService service = CreateService();

            service.ChangePassword(Password, "NewPassword456!");

            Assert.IsFalse(service.ContainerExists);
        }

        [TestMethod]
        public void ExportAll_WritesEveryFileInItsFolder()
        {
            FileStorageService service = CreateService();
            service.Load(Password);
            StorageFileSystem fs = service.FileSystem;
            int scans = fs.CreateDirectory(fs.GetRootDirectory(), "Scans");
            int receipts = fs.CreateDirectory(fs.GetDirectory(scans), "Receipts");
            fs.AddFile(fs.GetRootDirectory(), "top.txt", Encoding.UTF8.GetBytes("top"));
            fs.AddFile(fs.GetDirectory(receipts), "r1.txt", Encoding.UTF8.GetBytes("r1"));
            service.Save(Password);
            service.Unload();
            string target = _folder.Combine("export");

            int exported = service.ExportAll(Password, target);

            Assert.AreEqual(2, exported);
            Assert.AreEqual("top", File.ReadAllText(Path.Combine(target, "top.txt")));
            Assert.AreEqual("r1", File.ReadAllText(Path.Combine(target, "Scans", "Receipts", "r1.txt")));
            Assert.IsNull(service.FileSystem, "Exporting must not leave the decrypted file system loaded");
        }

        [TestMethod]
        public void ExportAll_NoContainer_ExportsNothing()
        {
            string target = _folder.Combine("export");

            Assert.AreEqual(0, CreateService().ExportAll(Password, target));
            Assert.IsFalse(Directory.Exists(target));
        }

        [TestMethod]
        public void ExportAll_WrongPassword_Throws()
        {
            CreateServiceWithFile("a.txt", Encoding.UTF8.GetBytes("a"));

            Assert.Throws<CryptographicException>(() => CreateService().ExportAll("WrongPassword1", _folder.Combine("export")));
        }

        [TestMethod]
        public void Delete_RemovesTheContainer()
        {
            FileStorageService service = CreateServiceWithFile("a.txt", Encoding.UTF8.GetBytes("a"));

            service.Delete();

            Assert.IsFalse(service.ContainerExists);
            Assert.IsNull(service.FileSystem);
        }
    }
}

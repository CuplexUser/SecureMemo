using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SecureMemo.FileStorageEvents;
using SecureMemo.FileStorageModels;

namespace UnitTests.DataModels
{
    [TestClass]
    public class FileStorageTests
    {
        private static byte[] Bytes(string text)
        {
            return Encoding.UTF8.GetBytes(text);
        }

        [TestMethod]
        public void ToContentThenFromContent_RoundTripsFoldersFilesAndContents()
        {
            StorageFileSystem original = StorageFileSystem.CreateNewFileSystem();
            int documentsId = original.CreateDirectory(original.GetRootDirectory(), "Documents");
            int fileId = original.AddFile(original.GetDirectory(documentsId), "notes.txt", Bytes("hello"));

            StorageFileSystem copy = StorageFileSystem.FromContent(original.ToContent());

            Assert.AreEqual("Documents", copy.GetDirectory(documentsId).DirectoryName);
            StorageFile file = copy.GetFiles(documentsId).Single();
            Assert.AreEqual("notes.txt", file.FileName);
            Assert.AreEqual(5, file.FileSize);
            CollectionAssert.AreEqual(Bytes("hello"), copy.ReadFile(fileId));
        }

        [TestMethod]
        public void FromContent_ContinuesNumberingWhereTheSavedFileSystemLeftOff()
        {
            StorageFileSystem original = StorageFileSystem.CreateNewFileSystem();
            int dirId = original.CreateDirectory(original.GetRootDirectory(), "A");
            int fileId = original.AddFile(original.GetRootDirectory(), "a.txt", Bytes("a"));

            StorageFileSystem copy = StorageFileSystem.FromContent(original.ToContent());

            Assert.AreNotEqual(dirId, copy.CreateDirectory(copy.GetRootDirectory(), "B"));
            Assert.AreNotEqual(fileId, copy.AddFile(copy.GetRootDirectory(), "b.txt", Bytes("b")));
        }

        [TestMethod]
        public void FromContent_WithoutRootDirectory_RecreatesIt()
        {
            StorageFileSystem fileSystem = StorageFileSystem.FromContent(new StorageFileContent());

            Assert.AreEqual(StorageFileSystem.RootDirectoryId, fileSystem.GetRootDirectory().Id);
        }

        [TestMethod]
        public void LoadedFileSystem_CanStillRenameDirectories()
        {
            // Regression test: the constructor used when loading never created the directory-name
            // validator, so renaming a folder after opening a saved file system threw.
            StorageFileSystem original = StorageFileSystem.CreateNewFileSystem();
            int documentsId = original.CreateDirectory(original.GetRootDirectory(), "Documents");

            StorageFileSystem loaded = StorageFileSystem.FromContent(original.ToContent());

            Assert.IsTrue(loaded.RenameDirectory(documentsId, "Papers"));
        }

        [TestMethod]
        public void GetDirectories_OfRoot_DoesNotIncludeTheRootItself()
        {
            // Regression test: the root's ParentId is its own id (0), so it was listed as its own
            // child - shown as an extra folder in the File Manager, and deleting that folder
            // recursed into itself until the process crashed.
            StorageFileSystem storageFileSystem = StorageFileSystem.CreateNewFileSystem();
            StorageDirectory root = storageFileSystem.GetRootDirectory();
            storageFileSystem.CreateDirectory(root, "Documents");

            List<StorageDirectory> children = storageFileSystem.GetDirectories(root.Id);

            CollectionAssert.AreEqual(new[] {"Documents"}, children.Select(d => d.DirectoryName).ToArray());
        }

        [TestMethod]
        public void GetDirectoriesAndFiles_AreSortedByName()
        {
            StorageFileSystem fs = StorageFileSystem.CreateNewFileSystem();
            StorageDirectory root = fs.GetRootDirectory();
            fs.CreateDirectory(root, "beta");
            fs.CreateDirectory(root, "Alpha");
            fs.AddFile(root, "z.txt", Bytes(""));
            fs.AddFile(root, "A.txt", Bytes(""));

            CollectionAssert.AreEqual(new[] {"Alpha", "beta"}, fs.GetDirectories(root.Id).Select(d => d.DirectoryName).ToArray());
            CollectionAssert.AreEqual(new[] {"A.txt", "z.txt"}, fs.GetFiles(root.Id).Select(f => f.FileName).ToArray());
        }

        [TestMethod]
        public void DeleteDirectory_RemovesChildDirectoriesAndFiles()
        {
            // Regression test: DeleteDirectory used to remove only the directory itself, silently
            // orphaning its child directories/files (never visible again, but never freed either).
            StorageFileSystem storageFileSystem = StorageFileSystem.CreateNewFileSystem();
            StorageDirectory root = storageFileSystem.GetRootDirectory();
            int parentId = storageFileSystem.CreateDirectory(root, "Parent");
            StorageDirectory parent = storageFileSystem.GetDirectory(parentId);
            int childId = storageFileSystem.CreateDirectory(parent, "Child");
            StorageDirectory child = storageFileSystem.GetDirectory(childId);
            int parentFileId = storageFileSystem.AddFile(parent, "in-parent.txt", Bytes("p"));
            storageFileSystem.AddFile(child, "in-child.txt", Bytes("c"));

            bool deleted = storageFileSystem.DeleteDirectory(parentId);

            Assert.IsTrue(deleted);
            Assert.IsNull(storageFileSystem.GetDirectory(parentId));
            Assert.IsNull(storageFileSystem.GetDirectory(childId));
            Assert.AreEqual(0, storageFileSystem.GetFiles(parentId).Count);
            Assert.AreEqual(0, storageFileSystem.GetFiles(childId).Count);
            Assert.IsNull(storageFileSystem.GetFile(parentFileId));
            Assert.AreEqual(0, storageFileSystem.ToContent().FileData.Count, "Deleted files' contents must not stay in the container");
        }

        [TestMethod]
        public void DeleteDirectory_RootOrUnknownId_ReturnsFalse()
        {
            StorageFileSystem fs = StorageFileSystem.CreateNewFileSystem();

            Assert.IsFalse(fs.DeleteDirectory(StorageFileSystem.RootDirectoryId));
            Assert.IsFalse(fs.DeleteDirectory(42));
            Assert.IsNotNull(fs.GetRootDirectory());
        }

        [TestMethod]
        public void CreateDirectory_InvalidNameOrUnknownParent_Throws()
        {
            StorageFileSystem fs = StorageFileSystem.CreateNewFileSystem();

            Assert.ThrowsExactly<ArgumentException>(() => fs.CreateDirectory(fs.GetRootDirectory(), "bad/name"));
            Assert.ThrowsExactly<ArgumentException>(() => fs.CreateDirectory(new StorageDirectory {Id = 99}, "Fine"));
        }

        [TestMethod]
        public void DirectoryChanges_RaiseEvents()
        {
            StorageFileSystem storageFileSystem = StorageFileSystem.CreateNewFileSystem();
            var events = new List<(StorageFileSystemEventTypes Type, int Id)>();
            storageFileSystem.DirectoryStructureChanged += (_, e) => events.Add((e.DirectoryEventType, e.DirectoryId));

            int id = storageFileSystem.CreateDirectory(storageFileSystem.GetRootDirectory(), "New");
            storageFileSystem.RenameDirectory(id, "Renamed");
            storageFileSystem.DeleteDirectory(id);

            CollectionAssert.AreEqual(new[]
            {
                (StorageFileSystemEventTypes.Created, id),
                (StorageFileSystemEventTypes.Renamed, id),
                (StorageFileSystemEventTypes.Deleted, id)
            }, events);
        }

        [TestMethod]
        public void FileChanges_RaiseEvents()
        {
            StorageFileSystem fs = StorageFileSystem.CreateNewFileSystem();
            var events = new List<(StorageFileSystemEventTypes Type, int Id)>();
            fs.FileStructureChanged += (_, e) => events.Add((e.FileEvent, e.FileId));

            int fileId = fs.AddFile(fs.GetRootDirectory(), "a.txt", Bytes("a"));
            fs.DeleteFile(fileId);

            CollectionAssert.AreEqual(new[] {(StorageFileSystemEventTypes.Created, fileId), (StorageFileSystemEventTypes.Deleted, fileId)}, events);
        }

        [TestMethod]
        public void RenameDirectory_InvalidNameRootOrUnknownId_IsRejected()
        {
            StorageFileSystem storageFileSystem = StorageFileSystem.CreateNewFileSystem();
            int id = storageFileSystem.CreateDirectory(storageFileSystem.GetRootDirectory(), "Docs");

            Assert.IsFalse(storageFileSystem.RenameDirectory(id, "bad/name"));
            Assert.IsFalse(storageFileSystem.RenameDirectory(99, "Fine"));
            Assert.IsFalse(storageFileSystem.RenameDirectory(StorageFileSystem.RootDirectoryId, "Fine"));
            Assert.AreEqual("Docs", storageFileSystem.GetDirectory(id).DirectoryName);
        }

        [TestMethod]
        [DataRow("Documents", true)]
        [DataRow("my_folder-2.old", true)]
        [DataRow("", false)]
        [DataRow(null, false)]
        [DataRow("with space", false)]
        [DataRow("a\\b", false)]
        public void IsValidDirectoryName(string name, bool expected)
        {
            Assert.AreEqual(expected, StorageFileSystem.CreateNewFileSystem().IsValidDirectoryName(name));
        }

        [TestMethod]
        [DataRow("report final.pdf", true)]
        [DataRow("no-extension", true)]
        [DataRow("", false)]
        [DataRow(" ", false)]
        [DataRow("..", false)]
        [DataRow("..\\escape.txt", false)]
        [DataRow("dir/file.txt", false)]
        [DataRow("what?.txt", false)]
        public void IsValidFileName(string name, bool expected)
        {
            Assert.AreEqual(expected, StorageFileSystem.IsValidFileName(name));
        }

        [TestMethod]
        public void AddFile_StoresACopyOfTheContent()
        {
            StorageFileSystem fs = StorageFileSystem.CreateNewFileSystem();
            byte[] content = Bytes("original");

            int fileId = fs.AddFile(fs.GetRootDirectory(), "a.txt", content);
            content[0] = (byte) 'X';
            fs.ReadFile(fileId)[1] = (byte) 'Y';

            CollectionAssert.AreEqual(Bytes("original"), fs.ReadFile(fileId));
        }

        [TestMethod]
        public void AddFile_NameAlreadyTakenInFolder_GetsANumberedName()
        {
            StorageFileSystem fs = StorageFileSystem.CreateNewFileSystem();
            StorageDirectory root = fs.GetRootDirectory();
            int otherFolder = fs.CreateDirectory(root, "Other");

            fs.AddFile(root, "report.pdf", Bytes("1"));
            int second = fs.AddFile(root, "REPORT.pdf", Bytes("2"));
            int third = fs.AddFile(root, "report.pdf", Bytes("3"));
            int elsewhere = fs.AddFile(fs.GetDirectory(otherFolder), "report.pdf", Bytes("4"));

            Assert.AreEqual("REPORT (2).pdf", fs.GetFile(second).FileName);
            Assert.AreEqual("report (3).pdf", fs.GetFile(third).FileName);
            Assert.AreEqual("report.pdf", fs.GetFile(elsewhere).FileName);
        }

        [TestMethod]
        public void AddFile_EmptyFile_CanBeReadBack()
        {
            StorageFileSystem fs = StorageFileSystem.CreateNewFileSystem();
            int fileId = fs.AddFile(fs.GetRootDirectory(), "empty.txt", Array.Empty<byte>());

            Assert.AreEqual(0, fs.ReadFile(fileId).Length);
        }

        [TestMethod]
        public void AddFile_InvalidArguments_Throw()
        {
            StorageFileSystem fs = StorageFileSystem.CreateNewFileSystem();

            Assert.ThrowsExactly<ArgumentException>(() => fs.AddFile(fs.GetRootDirectory(), "..\\escape.txt", Bytes("x")));
            Assert.ThrowsExactly<ArgumentException>(() => fs.AddFile(new StorageDirectory {Id = 99}, "a.txt", Bytes("x")));
            Assert.ThrowsExactly<ArgumentNullException>(() => fs.AddFile(fs.GetRootDirectory(), "a.txt", null));
        }

        [TestMethod]
        public void ReadFile_UnknownId_Throws()
        {
            Assert.ThrowsExactly<FileNotFoundException>(() => StorageFileSystem.CreateNewFileSystem().ReadFile(5));
        }

        [TestMethod]
        public void DeleteFile_RemovesTheFileAndItsContent()
        {
            StorageFileSystem fs = StorageFileSystem.CreateNewFileSystem();
            int fileId = fs.AddFile(fs.GetRootDirectory(), "a.txt", Bytes("a"));

            Assert.IsTrue(fs.DeleteFile(fileId));
            Assert.IsFalse(fs.DeleteFile(fileId));
            Assert.IsNull(fs.GetFile(fileId));
            Assert.AreEqual(0, fs.ToContent().FileData.Count);
        }
    }
}

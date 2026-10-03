using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SecureMemo.Services;
using UnitTests.TestSupport;

namespace UnitTests.Services
{
    [TestClass]
    public class DecryptedFileCacheTests
    {
        private TempDirectory _root;

        [TestInitialize]
        public void Setup()
        {
            _root = new TempDirectory("sm_opencache_");
        }

        [TestCleanup]
        public void Cleanup()
        {
            DecryptedFileCache.RemoveAll(_root.Path);
            _root.Dispose();
        }

        [TestMethod]
        public void WriteFile_KeepsTheFileNameAndContentAndIsReadOnly()
        {
            var cache = new DecryptedFileCache(_root.Path);

            string path = cache.WriteFile("report.pdf", Encoding.UTF8.GetBytes("pdf bytes"));

            Assert.AreEqual("report.pdf", Path.GetFileName(path));
            Assert.AreEqual("pdf bytes", File.ReadAllText(path));
            Assert.IsTrue(File.GetAttributes(path).HasFlag(FileAttributes.ReadOnly));
            StringAssert.StartsWith(path, _root.Path);
        }

        [TestMethod]
        public void WriteFile_SameNameTwice_KeepsBothCopies()
        {
            var cache = new DecryptedFileCache(_root.Path);

            string first = cache.WriteFile("a.txt", Encoding.UTF8.GetBytes("1"));
            string second = cache.WriteFile("a.txt", Encoding.UTF8.GetBytes("2"));

            Assert.AreNotEqual(first, second);
            Assert.AreEqual("1", File.ReadAllText(first));
        }

        [TestMethod]
        public void WriteFile_NameWithDirectoryParts_StaysInsideTheCache()
        {
            var cache = new DecryptedFileCache(_root.Path);

            string path = cache.WriteFile("..\\..\\escape.txt", Encoding.UTF8.GetBytes("x"));

            StringAssert.StartsWith(Path.GetFullPath(path), _root.Path);
        }

        [TestMethod]
        public void Clear_DeletesOnlyThisSessionsCopies()
        {
            var thisSession = new DecryptedFileCache(_root.Path);
            var otherSession = new DecryptedFileCache(_root.Path);
            string mine = thisSession.WriteFile("mine.txt", Encoding.UTF8.GetBytes("m"));
            string theirs = otherSession.WriteFile("theirs.txt", Encoding.UTF8.GetBytes("t"));

            Assert.IsTrue(thisSession.Clear());

            Assert.IsFalse(File.Exists(mine));
            Assert.IsTrue(File.Exists(theirs));
        }

        [TestMethod]
        public void Clear_FileStillOpenElsewhere_IsLeftForRemoveAll()
        {
            var cache = new DecryptedFileCache(_root.Path);
            string path = cache.WriteFile("open.txt", Encoding.UTF8.GetBytes("x"));

            using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
                Assert.IsFalse(cache.Clear());

            Assert.IsTrue(DecryptedFileCache.RemoveAll(_root.Path));
            Assert.IsFalse(Directory.Exists(_root.Path));
        }

        [TestMethod]
        public void ClearOrRemoveAll_NothingWritten_Succeeds()
        {
            Assert.IsTrue(new DecryptedFileCache(_root.Path).Clear());
            Assert.IsTrue(DecryptedFileCache.RemoveAll(Path.Combine(_root.Path, "missing")));
        }
    }
}

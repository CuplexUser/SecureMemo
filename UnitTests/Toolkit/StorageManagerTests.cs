using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SecureMemo.DataModels;
using SecureMemo.Toolkit.Storage;
using SecureMemo.Toolkit.Storage.Models;
using UnitTests.TestSupport;

namespace UnitTests.Toolkit
{
    [TestClass]
    public class StorageManagerTests
    {
        [TestMethod]
        public void Deserialize_FileWrittenWithoutTheBlockHeader_IsStillRead()
        {
            // Single-threaded writes produce a plain LZMA stream without the multi-block header;
            // the multi-threaded reader the app uses must fall back to decoding that format.
            using var folder = new TempDirectory();
            string path = folder.Combine("legacy.dat");
            var collection = TabPageDataCollection.CreateNewPageDataCollection(2);
            collection.TabPageDictionary[1].TabPageText = "written single-threaded";

            Assert.IsTrue(new StorageManager(new StorageManagerSettings(false, 1, true, "Passw0rd")).SerializeObjectToFile(collection, path, null));
            var loaded = new StorageManager(new StorageManagerSettings(true, Environment.ProcessorCount, true, "Passw0rd")).DeserializeObjectFromFile<TabPageDataCollection>(path, null);

            Assert.AreEqual("written single-threaded", loaded.TabPageDictionary[1].TabPageText);
        }

        [TestMethod]
        public void Serialize_Null_Throws()
        {
            using var folder = new TempDirectory();

            Assert.ThrowsExactly<ArgumentException>(() => new StorageManager(new StorageManagerSettings(true, 1, true, "Passw0rd")).SerializeObjectToFile(null, folder.Combine("x.dat"), null));
        }

        [TestMethod]
        public void Constructor_NullSettings_Throws()
        {
            Assert.ThrowsExactly<ArgumentException>(() => new StorageManager(null));
        }
    }
}

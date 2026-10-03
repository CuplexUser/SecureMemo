using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProtoBuf;
using SecureMemo.DataModels;
using SecureMemo.Toolkit.Compression.SevenZip.Compress.LZMA;
using SecureMemo.Toolkit.Encryption;
using SecureMemo.Toolkit.Storage;
using SecureMemo.Toolkit.Storage.Models;
using UnitTests.TestSupport;

namespace UnitTests.Toolkit
{
    [TestClass]
    public class StorageManagerTests
    {
        [TestMethod]
        public void Deserialize_FileWithoutTheBlockHeader_IsStillRead()
        {
            // Older single-threaded writes stored one plain LZMA stream (encoder properties, the
            // uncompressed size, then the data) with no multi-block header in front.
            using var folder = new TempDirectory();
            string path = folder.Combine("legacy.dat");
            var collection = TabPageDataCollection.CreateNewPageDataCollection(2);
            collection.TabPageDictionary[1].TabPageText = "written single-threaded";

            var serialized = new MemoryStream();
            Serializer.Serialize(serialized, collection);
            serialized.Position = 0;
            var compressed = new MemoryStream();
            var encoder = new Encoder();
            encoder.WriteCoderProperties(compressed);
            compressed.Write(BitConverter.GetBytes(serialized.Length), 0, 8);
            encoder.Code(serialized, compressed, serialized.Length, -1, null);
            Assert.IsTrue(new EncryptionManager().EncryptAndSaveFile(path, compressed, "Passw0rd"));

            var loaded = new StorageManager(new StorageManagerSettings(Environment.ProcessorCount, "Passw0rd")).DeserializeObjectFromFile<TabPageDataCollection>(path);

            Assert.AreEqual("written single-threaded", loaded.TabPageDictionary[1].TabPageText);
        }

        [TestMethod]
        public void Serialize_NullOrNonDataContractObject_Throws()
        {
            using var folder = new TempDirectory();
            var storageManager = new StorageManager(new StorageManagerSettings(1, "Passw0rd"));

            Assert.ThrowsExactly<ArgumentException>(() => storageManager.SerializeObjectToFile(null, folder.Combine("x.dat")));
            Assert.ThrowsExactly<ArgumentException>(() => storageManager.SerializeObjectToFile("not a data contract", folder.Combine("x.dat")));
        }

        [TestMethod]
        public void Constructor_NullSettings_Throws()
        {
            Assert.ThrowsExactly<ArgumentException>(() => new StorageManager(null));
        }
    }
}

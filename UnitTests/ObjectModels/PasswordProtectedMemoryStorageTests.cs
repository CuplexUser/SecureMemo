using Microsoft.VisualStudio.TestTools.UnitTesting;
using SecureMemo.Toolkit.Storage.Memory;

namespace UnitTests.ObjectModels
{
    [TestClass]
    public class PasswordProtectedMemoryStorageTests
    {
        [TestMethod]
        public void SetThenGet_ReturnsSameValue()
        {
            using var storage = new PasswordStorage();
            storage.Set("SecureMemo", "correct-horse-battery-staple");

            Assert.AreEqual("correct-horse-battery-staple", storage.Get("SecureMemo"));
        }

        [TestMethod]
        public void Get_UnknownKey_ReturnsNull()
        {
            using var storage = new PasswordStorage();

            Assert.IsNull(storage.Get("NeverSet"));
        }

        [TestMethod]
        public void Set_NullOrEmptyValue_RemovesKey()
        {
            using var storage = new PasswordStorage();
            storage.Set("SecureMemo", "some-password");
            storage.Set("SecureMemo", null);

            Assert.IsNull(storage.Get("SecureMemo"));
        }

        [TestMethod]
        public void PurgeMemory_ClearsAllStoredValues()
        {
            using var storage = new PasswordStorage();
            storage.Set("SecureMemo", "db-password");
            storage.Set("SharedFolderPassword", "shared-password");

            storage.PurgeMemory();

            Assert.IsNull(storage.Get("SecureMemo"));
            Assert.IsNull(storage.Get("SharedFolderPassword"));
        }
    }
}

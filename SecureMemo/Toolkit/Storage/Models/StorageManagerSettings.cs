using System.Security;

namespace SecureMemo.Toolkit.Storage.Models
{
    [SecurityCritical]
    public sealed class StorageManagerSettings
    {
        [SecuritySafeCritical]
        private string _password;

        public StorageManagerSettings(int numberOfThreads, string password)
        {
            NumberOfThreads = numberOfThreads;
            _password = password;
        }

        /// <summary>
        ///     How many 2 MB blocks are compressed or decompressed in parallel.
        /// </summary>
        public int NumberOfThreads { get; }

        public void SetPassword(string password)
        {
            _password = password;
        }

        [SecuritySafeCritical]
        public string GetPassword()
        {
            return _password;
        }
    }
}

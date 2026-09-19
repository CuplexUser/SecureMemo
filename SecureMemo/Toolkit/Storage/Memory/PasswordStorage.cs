using System;
using System.Collections.Generic;
using System.Text;

namespace SecureMemo.Toolkit.Storage.Memory
{
    public sealed class PasswordStorage : IDisposable
    {
        private readonly Dictionary<string, byte[]> _encodedDataDictionary;

        public PasswordStorage()
        {
            _encodedDataDictionary = new Dictionary<string, byte[]>();
        }

        public void Set(string key, string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                _encodedDataDictionary.Remove(key);
                return;
            }

            _encodedDataDictionary[key] = Encoding.UTF8.GetBytes(password);
        }

        public string Get(string key)
        {
            return _encodedDataDictionary.TryGetValue(key, out byte[] buffer) ? Encoding.UTF8.GetString(buffer) : null;
        }

        public void PurgeMemory()
        {
            foreach (var buffer in _encodedDataDictionary.Values)
                Array.Clear(buffer, 0, buffer.Length);

            _encodedDataDictionary.Clear();
        }

        public void Dispose()
        {
            PurgeMemory();
        }
    }
}

using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Serilog;

namespace SecureMemo.Toolkit.Encryption
{
    public class EncryptionManager
    {
        private const int MaxBufferSize = 33554432; //32 Mb

        private static readonly byte[] SALT =
        {
            0x16, 0x81, 0x38, 0x37, 0x1d, 0x3e, 0x97, 0x2, 0x4d, 0x72, 0x69, 0xaa, 0x99, 0x92, 0x64, 0x3d, 0xa0, 0x10, 0x65, 0x2, 0xef, 0x4c, 0x72, 0xb9, 0xbe, 0x75,
            0x9, 0xee, 0x6e, 0x9a, 0x9b, 0x12
        };

        public bool EncryptAndSaveFile(string filePath, MemoryStream ms, string passwordString)
        {
            FileStream fs = null;

            try
            {
                if (string.IsNullOrEmpty(passwordString))
                    throw new Exception("Password can not be null or empty");

                if (File.Exists(filePath))
                    File.Delete(filePath);

                fs = File.Create(filePath);

                using (Aes aesAlg = CreateAes(passwordString))
                {
                    // Create a encrypt transform
                    ICryptoTransform encrypt = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                    // Create the streams used for encryption.
                    int bufferSize = (int) Math.Min(MaxBufferSize, ms.Length);
                    var buffer = new byte[bufferSize];
                    ms.Position = 0;

                    using (var csEncrypt = new CryptoStream(fs, encrypt, CryptoStreamMode.Write))
                    {
                        int bytesRead;
                        while ((bytesRead = ms.Read(buffer, 0, buffer.Length)) > 0)
                            csEncrypt.Write(buffer, 0, bytesRead);

                        csEncrypt.FlushFinalBlock();
                        fs.Flush();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in EncryptionManager.EncryptAndSaveFile");
                return false;
            }
            finally
            {
                fs?.Close();
            }

            return true;
        }

        public MemoryStream DecryptFileToMemoryStream(string filePath, string passwordString)
        {
            var ms = new MemoryStream();
            FileStream fs = null;
            try
            {
                if (string.IsNullOrEmpty(passwordString))
                    throw new Exception("Password can not be null or empty");

                fs = File.OpenRead(filePath);
                fs.Position = 0;

                using (Aes aesAlg = CreateAes(passwordString))
                {
                    // Create a decrytor to perform the stream transform.
                    ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                    // Create the streams used for decryption.
                    int bufferSize = Math.Min(MaxBufferSize, (int) fs.Length);
                    var plainTextBytes = new byte[bufferSize];

                    using (var csDecrypt = new CryptoStream(fs, decryptor, CryptoStreamMode.Read))
                    {
                        int decryptedByteCount;
                        while ((decryptedByteCount = csDecrypt.Read(plainTextBytes, 0, plainTextBytes.Length)) > 0)
                            ms.Write(plainTextBytes, 0, decryptedByteCount);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in EncryptionManager.DecryptFileToMemoryStream");
                return null;
            }
            finally
            {
                fs?.Close();
            }

            return ms;
        }

        private static Aes CreateAes(string passwordString)
        {
            Aes aesAlg = Aes.Create();
            Debug.Assert(aesAlg != null, nameof(aesAlg) + " != null");
            aesAlg.BlockSize = 128;
            aesAlg.KeySize = 256;
            aesAlg.Padding = PaddingMode.PKCS7;
            aesAlg.Mode = CipherMode.CBC;

            // Derived in one 48-byte pull (key = first 32 bytes, IV = next 16) to exactly
            // reproduce the old stateful GetBytes(32)+GetBytes(16) sequence from a single
            // Rfc2898DeriveBytes instance; SHA1 matches that constructor's implicit
            // default. Verified byte-for-byte identical to the old API before switching.
            byte[] keyMaterial = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(passwordString), SALT, 1000, HashAlgorithmName.SHA1, 48);
            aesAlg.Key = keyMaterial[..32];
            aesAlg.IV = keyMaterial[32..48];

            return aesAlg;
        }
    }
}

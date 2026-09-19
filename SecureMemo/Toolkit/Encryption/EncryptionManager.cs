using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
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

        public async Task<bool> EncryptAndSaveFileAsync(string filePath, MemoryStream ms, string passwordString, CryptoProgress progress)
        {
            bool result = await Task.Run(() => EncryptAndSaveFile(filePath, ms, passwordString, progress));
            return result;
        }

        public bool EncryptAndSaveFile(string filePath, MemoryStream ms, string passwordString, CryptoProgress progress)
        {
            FileStream fs = null;

            try
            {
                CryptoProgressHandler progressHandler = null;

                if (string.IsNullOrEmpty(passwordString))
                    throw new Exception("Password can not be null or empty");

                if (File.Exists(filePath))
                    File.Delete(filePath);

                fs = File.Create(filePath);

                if (progress != null)
                {
                    progressHandler = new CryptoProgressHandler { EncodedBytes = 0, TotalBytes = ms.Length, Text = "Starting ecryption" };
                    progress.Report(progressHandler);
                }

                using (Aes aesAlg = Aes.Create())
                {
                    var rfc2898DeriveBytes = new Rfc2898DeriveBytes(passwordString, SALT, 1000);
                    Debug.Assert(aesAlg != null, nameof(aesAlg) + " != null");
                    aesAlg.BlockSize = 128;
                    aesAlg.KeySize = 256;
                    aesAlg.Padding = PaddingMode.PKCS7;
                    aesAlg.Mode = CipherMode.CBC;

                    aesAlg.Key = rfc2898DeriveBytes.GetBytes(32);
                    aesAlg.IV = rfc2898DeriveBytes.GetBytes(16);

                    // Create a encrypt transform
                    ICryptoTransform encrypt = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                    // Create the streams used for encryption.
                    int bufferSize = (int)Math.Min(MaxBufferSize, ms.Length);
                    var buffer = new byte[bufferSize];
                    ms.Position = 0;

                    using (var csEncrypt = new CryptoStream(fs, encrypt, CryptoStreamMode.Write))
                    {
                        int bytesRead;
                        while ((bytesRead = ms.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            csEncrypt.Write(buffer, 0, bytesRead);
                            if (progressHandler == null) continue;
                            progressHandler.EncodedBytes += bytesRead;
                            progress.Report(progressHandler);
                        }

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
                progress?.Report(new CryptoProgressHandler { EncodedBytes = ms.Length, TotalBytes = ms.Length, Text = "Encryption completed" });
            }

            return true;
        }

        public async Task<MemoryStream> DecryptFileToMemoryStreamAsync(string filePath, string passwordString, CryptoProgress progress)
        {
            MemoryStream result = await Task.Run(() => DecryptFileToMemoryStream(filePath, passwordString, progress));
            return result;
        }

        public MemoryStream DecryptFileToMemoryStream(string filePath, string passwordString, CryptoProgress progress)
        {
            var ms = new MemoryStream();
            FileStream fs = null;
            try
            {
                CryptoProgressHandler progressHandler = null;
                if (string.IsNullOrEmpty(passwordString))
                    throw new Exception("Password can not be null or empty");

                fs = File.OpenRead(filePath);
                fs.Position = 0;


                if (progress != null)
                {
                    progressHandler = new CryptoProgressHandler { EncodedBytes = 0, TotalBytes = fs.Length, Text = "Starting decryption" };
                    progress.Report(progressHandler);
                }

                // Create an AesCryptoServiceProvider object
                using (Aes aesAlg = Aes.Create())
                {
                    var rfc2898DeriveBytes = new Rfc2898DeriveBytes(passwordString, SALT, 1000);
                    aesAlg.BlockSize = 128;
                    aesAlg.KeySize = 256;
                    aesAlg.Padding = PaddingMode.PKCS7;
                    aesAlg.Mode = CipherMode.CBC;

                    aesAlg.Key = rfc2898DeriveBytes.GetBytes(32);
                    aesAlg.IV = rfc2898DeriveBytes.GetBytes(16);

                    // Create a decrytor to perform the stream transform.
                    ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                    // Create the streams used for decryption.
                    int bufferSize = Math.Min(MaxBufferSize, (int)fs.Length);
                    var plainTextBytes = new byte[bufferSize];

                    using (var csDecrypt = new CryptoStream(fs, decryptor, CryptoStreamMode.Read))
                    {
                        int decryptedByteCount;
                        while ((decryptedByteCount = csDecrypt.Read(plainTextBytes, 0, plainTextBytes.Length)) > 0)
                        {
                            ms.Write(plainTextBytes, 0, decryptedByteCount);
                            if (progressHandler == null) continue;
                            progressHandler.EncodedBytes += decryptedByteCount;
                            progress.Report(progressHandler);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in EncryptionManager.EncryptAndSaveFile");
                return null;
            }
            finally
            {
                fs?.Close();

                progress?.Report(new CryptoProgressHandler { EncodedBytes = ms.Length, TotalBytes = ms.Length, Text = "Decryption completed" });
            }

            return ms;
        }
    }
}

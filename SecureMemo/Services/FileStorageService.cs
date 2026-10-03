using System;
using System.IO;
using System.Security.Cryptography;
using SecureMemo.FileStorageModels;
using SecureMemo.Toolkit.Storage;
using SecureMemo.Toolkit.Storage.Models;

namespace SecureMemo.Services
{
    /// <summary>
    ///     Loads and saves the File Manager's single encrypted container file, encrypted with the
    ///     memo database password.
    /// </summary>
    public class FileStorageService : ServiceBase
    {
        public const string ContainerFileName = "FileStorage.dat";

        /// <summary>
        ///     Files larger than this are refused; the whole container is held in memory and
        ///     recompressed on every save.
        /// </summary>
        public const long MaxFileSize = 64 * 1024 * 1024;

        private readonly string _containerFilePath;

        public FileStorageService(string containerFilePath)
        {
            _containerFilePath = containerFilePath;
        }

        /// <summary>
        ///     The loaded file system, or null when nothing is loaded.
        /// </summary>
        public StorageFileSystem FileSystem { get; private set; }

        public bool ContainerExists => File.Exists(_containerFilePath);

        /// <summary>
        ///     Loads the container, or starts an empty file system if none has been saved yet.
        /// </summary>
        /// <exception cref="CryptographicException">The container could not be decrypted with <paramref name="password" />.</exception>
        public void Load(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new InvalidOperationException("A password is required to open the file storage");

            FileSystem = ContainerExists ? StorageFileSystem.FromContent(ReadContainer(password)) : StorageFileSystem.CreateNewFileSystem();
        }

        /// <summary>
        ///     Encrypts and writes the loaded file system. The previous container is only replaced
        ///     once the new one has been written completely.
        /// </summary>
        public void Save(string password)
        {
            if (FileSystem == null)
                throw new InvalidOperationException("No file system is loaded");

            WriteContainer(FileSystem.ToContent(), password);
        }

        /// <summary>
        ///     Drops the decrypted file system from memory.
        /// </summary>
        public void Unload()
        {
            FileSystem = null;
        }

        /// <summary>
        ///     Re-encrypts an existing container with a new password.
        /// </summary>
        public void ChangePassword(string currentPassword, string newPassword)
        {
            if (!ContainerExists)
                return;

            WriteContainer(ReadContainer(currentPassword), newPassword);
        }

        /// <summary>
        ///     Writes decrypted copies of every stored file into <paramref name="targetFolder" />,
        ///     recreating the folder structure. The folder is created if needed.
        /// </summary>
        /// <returns>The number of files written.</returns>
        public int ExportAll(string password, string targetFolder)
        {
            if (!ContainerExists)
                return 0;

            StorageFileSystem fileSystem = StorageFileSystem.FromContent(ReadContainer(password));
            return ExportDirectory(fileSystem, StorageFileSystem.RootDirectoryId, targetFolder);
        }

        private static int ExportDirectory(StorageFileSystem fileSystem, int directoryId, string targetFolder)
        {
            Directory.CreateDirectory(targetFolder);

            int fileCount = 0;
            foreach (StorageFile file in fileSystem.GetFiles(directoryId))
            {
                File.WriteAllBytes(Path.Combine(targetFolder, file.FileName), fileSystem.ReadFile(file.Id));
                fileCount++;
            }

            foreach (StorageDirectory directory in fileSystem.GetDirectories(directoryId))
                fileCount += ExportDirectory(fileSystem, directory.Id, Path.Combine(targetFolder, directory.DirectoryName));

            return fileCount;
        }

        /// <summary>
        ///     Permanently removes the container and everything stored in it.
        /// </summary>
        public void Delete()
        {
            FileSystem = null;
            if (ContainerExists)
                File.Delete(_containerFilePath);
        }

        private StorageFileContent ReadContainer(string password)
        {
            try
            {
                var storageManager = new StorageManager(new StorageManagerSettings(true, Environment.ProcessorCount, true, password));
                return storageManager.DeserializeObjectFromFile<StorageFileContent>(_containerFilePath, null)
                       ?? throw new CryptographicException("The file storage is empty");
            }
            catch (CryptographicException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // A wrong password usually surfaces as a decryption error, but can also produce
                // bytes that only fail later, while decompressing or deserializing.
                throw new CryptographicException("The file storage could not be decrypted with the current password", ex);
            }
        }

        private void WriteContainer(StorageFileContent content, string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new InvalidOperationException("A password is required to save the file storage");

            string tempFilePath = _containerFilePath + ".tmp";
            var storageManager = new StorageManager(new StorageManagerSettings(true, Environment.ProcessorCount, true, password));
            if (!storageManager.SerializeObjectToFile(content, tempFilePath, null))
            {
                File.Delete(tempFilePath);
                throw new IOException("The file storage could not be saved");
            }

            File.Move(tempFilePath, _containerFilePath, true);
        }
    }
}

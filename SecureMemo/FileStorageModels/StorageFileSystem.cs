using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SecureMemo.FileStorageEvents;

namespace SecureMemo.FileStorageModels
{
    /// <summary>
    ///     The in-memory folder tree and file contents shown by the File Manager. Persisting it
    ///     (encrypted) is up to <see cref="Services.FileStorageService" />.
    /// </summary>
    public class StorageFileSystem
    {
        public const int RootDirectoryId = 0;
        private const string RootDirectoryName = "Files";
        private static readonly Regex ValidDirNameRegex = new Regex(@"^[\w\._-]+$");

        private readonly Dictionary<int, StorageDirectory> _directories;
        private readonly Dictionary<int, StorageFile> _files;
        private readonly Dictionary<int, byte[]> _fileData;
        private int _nextDirectoryId;
        private int _nextFileId;

        private StorageFileSystem(StorageFileContent content)
        {
            _directories = (content.Directories ?? new List<StorageDirectory>()).ToDictionary(d => d.Id);
            _files = (content.Files ?? new List<StorageFile>()).ToDictionary(f => f.Id);
            _fileData = new Dictionary<int, byte[]>(content.FileData ?? new Dictionary<int, byte[]>());
            _nextDirectoryId = content.NextDirectoryId;
            _nextFileId = content.NextFileId;

            if (!_directories.ContainsKey(RootDirectoryId))
            {
                _directories.Add(RootDirectoryId, new StorageDirectory {Id = RootDirectoryId, ParentId = RootDirectoryId, DirectoryName = RootDirectoryName, CreateDate = DateTime.Now, ModifiedDate = DateTime.Now});
                _nextDirectoryId = Math.Max(_nextDirectoryId, RootDirectoryId + 1);
            }
        }

        public event StorageFileEventHandler FileStructureChanged;
        public event StorageDirectoryEventHandler DirectoryStructureChanged;

        public static StorageFileSystem CreateNewFileSystem()
        {
            return new StorageFileSystem(new StorageFileContent());
        }

        public static StorageFileSystem FromContent(StorageFileContent content)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            return new StorageFileSystem(content);
        }

        public StorageFileContent ToContent()
        {
            return new StorageFileContent
            {
                Directories = _directories.Values.ToList(),
                Files = _files.Values.ToList(),
                FileData = new Dictionary<int, byte[]>(_fileData),
                NextDirectoryId = _nextDirectoryId,
                NextFileId = _nextFileId
            };
        }

        #region Directories

        public int CreateDirectory(StorageDirectory parentDirectory, string directoryName)
        {
            if (parentDirectory == null || !_directories.ContainsKey(parentDirectory.Id))
                throw new ArgumentException("The parent directory does not exist", nameof(parentDirectory));

            if (!IsValidDirectoryName(directoryName))
                throw new ArgumentException("Invalid directory name: " + directoryName, nameof(directoryName));

            var storageDirectory = new StorageDirectory
            {
                Id = _nextDirectoryId++,
                ParentId = parentDirectory.Id,
                DirectoryName = directoryName,
                CreateDate = DateTime.Now,
                ModifiedDate = DateTime.Now
            };

            _directories.Add(storageDirectory.Id, storageDirectory);

            DirectoryStructureChanged?.Invoke(this,
                new StorageDirectorySystemEventArgs {DirectoryEventType = StorageFileSystemEventTypes.Created, DirectoryId = storageDirectory.Id, ParentDirectoryId = storageDirectory.ParentId});

            return storageDirectory.Id;
        }

        public bool DeleteDirectory(int directoryId)
        {
            if (directoryId == RootDirectoryId || !_directories.TryGetValue(directoryId, out StorageDirectory storageDirectory))
                return false;

            // Without cascading, child directories/files would stay in the container forever,
            // orphaned and invisible in the UI but never actually removed.
            foreach (StorageDirectory childDirectory in GetDirectories(directoryId))
                DeleteDirectory(childDirectory.Id);

            foreach (StorageFile file in GetFiles(directoryId))
            {
                _files.Remove(file.Id);
                _fileData.Remove(file.Id);
            }

            _directories.Remove(directoryId);

            DirectoryStructureChanged?.Invoke(this,
                new StorageDirectorySystemEventArgs {DirectoryEventType = StorageFileSystemEventTypes.Deleted, DirectoryId = directoryId, ParentDirectoryId = storageDirectory.ParentId});

            return true;
        }

        public bool RenameDirectory(int directoryId, string newDirectoryName)
        {
            if (!IsValidDirectoryName(newDirectoryName) || directoryId == RootDirectoryId)
                return false;

            if (!_directories.TryGetValue(directoryId, out StorageDirectory storageDirectory))
                return false;

            storageDirectory.DirectoryName = newDirectoryName;
            storageDirectory.ModifiedDate = DateTime.Now;

            DirectoryStructureChanged?.Invoke(this,
                new StorageDirectorySystemEventArgs {DirectoryEventType = StorageFileSystemEventTypes.Renamed, DirectoryId = directoryId, ParentDirectoryId = storageDirectory.ParentId});

            return true;
        }

        public bool IsValidDirectoryName(string directoryName)
        {
            if (string.IsNullOrEmpty(directoryName))
                return false;

            return ValidDirNameRegex.IsMatch(directoryName);
        }

        public StorageDirectory GetRootDirectory()
        {
            return _directories[RootDirectoryId];
        }

        public StorageDirectory GetDirectory(int directoryId)
        {
            return _directories.GetValueOrDefault(directoryId);
        }

        public List<StorageDirectory> GetDirectories(int parentDirectoryId)
        {
            // The root is its own parent, so it must not be listed as its own child.
            return _directories.Values.Where(d => d.ParentId == parentDirectoryId && d.Id != parentDirectoryId).OrderBy(d => d.DirectoryName, StringComparer.CurrentCultureIgnoreCase).ToList();
        }

        #endregion

        #region Files

        /// <summary>
        ///     Stores a copy of <paramref name="content" /> in <paramref name="parentDirectory" />. A name
        ///     that is already taken in that directory gets a " (2)", " (3)", ... suffix.
        /// </summary>
        /// <returns>The new file's id.</returns>
        public int AddFile(StorageDirectory parentDirectory, string fileName, byte[] content)
        {
            if (parentDirectory == null || !_directories.ContainsKey(parentDirectory.Id))
                throw new ArgumentException("The parent directory does not exist", nameof(parentDirectory));

            if (!IsValidFileName(fileName))
                throw new ArgumentException("Invalid file name: " + fileName, nameof(fileName));

            if (content == null)
                throw new ArgumentNullException(nameof(content));

            var storageFile = new StorageFile
            {
                Id = _nextFileId++,
                DirectoryId = parentDirectory.Id,
                FileName = GetUniqueFileName(parentDirectory.Id, fileName),
                FileSize = content.Length,
                CreateDate = DateTime.Now,
                ModifiedDate = DateTime.Now
            };

            _files.Add(storageFile.Id, storageFile);
            _fileData.Add(storageFile.Id, (byte[]) content.Clone());

            FileStructureChanged?.Invoke(this, new StorageFileSystemEventArgs {DirectoryId = storageFile.DirectoryId, FileId = storageFile.Id, FileEvent = StorageFileSystemEventTypes.Created});

            return storageFile.Id;
        }

        /// <summary>
        ///     Returns a copy of the stored file's contents.
        /// </summary>
        public byte[] ReadFile(int fileId)
        {
            if (!_fileData.TryGetValue(fileId, out byte[] data))
                throw new FileNotFoundException("No stored file with id " + fileId);

            // protobuf-net reads an empty byte array back as null.
            return data == null ? Array.Empty<byte>() : (byte[]) data.Clone();
        }

        public bool DeleteFile(int fileId)
        {
            if (!_files.Remove(fileId, out StorageFile storageFile))
                return false;

            _fileData.Remove(fileId);
            FileStructureChanged?.Invoke(this, new StorageFileSystemEventArgs {DirectoryId = storageFile.DirectoryId, FileId = fileId, FileEvent = StorageFileSystemEventTypes.Deleted});

            return true;
        }

        public StorageFile GetFile(int fileId)
        {
            return _files.GetValueOrDefault(fileId);
        }

        public List<StorageFile> GetFiles(int directoryId)
        {
            return _files.Values.Where(f => f.DirectoryId == directoryId).OrderBy(f => f.FileName, StringComparer.CurrentCultureIgnoreCase).ToList();
        }

        /// <summary>
        ///     A plain file name: not empty, no directory parts and no characters Windows rejects.
        /// </summary>
        public static bool IsValidFileName(string fileName)
        {
            return !string.IsNullOrWhiteSpace(fileName) && fileName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 && fileName != "." && fileName != "..";
        }

        private string GetUniqueFileName(int directoryId, string fileName)
        {
            var existingNames = new HashSet<string>(GetFiles(directoryId).Select(f => f.FileName), StringComparer.OrdinalIgnoreCase);
            if (!existingNames.Contains(fileName))
                return fileName;

            string baseName = Path.GetFileNameWithoutExtension(fileName);
            string extension = Path.GetExtension(fileName);
            for (int i = 2;; i++)
            {
                string candidate = $"{baseName} ({i}){extension}";
                if (!existingNames.Contains(candidate))
                    return candidate;
            }
        }

        #endregion
    }
}

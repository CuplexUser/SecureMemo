using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SecureMemo.FileStorageModels
{
    /// <summary>
    ///     Everything persisted in the encrypted file storage container: the folder and file index
    ///     plus the contents of every stored file.
    /// </summary>
    [DataContract(Name = "StorageFileContent")]
    public class StorageFileContent
    {
        public StorageFileContent()
        {
            Directories = new List<StorageDirectory>();
            Files = new List<StorageFile>();
            FileData = new Dictionary<int, byte[]>();
        }

        [DataMember(Name = "Directories", Order = 1)]
        public List<StorageDirectory> Directories { get; set; }

        [DataMember(Name = "Files", Order = 2)]
        public List<StorageFile> Files { get; set; }

        /// <summary>
        ///     File contents keyed by <see cref="StorageFile.Id" />.
        /// </summary>
        [DataMember(Name = "FileData", Order = 3)]
        public Dictionary<int, byte[]> FileData { get; set; }

        [DataMember(Name = "NextDirectoryId", Order = 4)]
        public int NextDirectoryId { get; set; }

        [DataMember(Name = "NextFileId", Order = 5)]
        public int NextFileId { get; set; }
    }
}

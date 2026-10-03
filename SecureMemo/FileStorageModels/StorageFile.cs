using System;
using System.Runtime.Serialization;

namespace SecureMemo.FileStorageModels
{
    [DataContract(Name = "StorageFile")]
    public class StorageFile
    {
        [DataMember(Name = "Id", Order = 1)]
        public int Id { get; set; }

        [DataMember(Name = "DirectoryId", Order = 2)]
        public int DirectoryId { get; set; }

        [DataMember(Name = "FileName", Order = 3)]
        public string FileName { get; set; }

        [DataMember(Name = "FileSize", Order = 4)]
        public long FileSize { get; set; }

        [DataMember(Name = "CreateDate", Order = 5)]
        public DateTime CreateDate { get; set; }

        [DataMember(Name = "ModifiedDate", Order = 6)]
        public DateTime ModifiedDate { get; set; }
    }
}

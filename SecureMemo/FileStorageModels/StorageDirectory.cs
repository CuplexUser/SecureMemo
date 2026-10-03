using System;
using System.Runtime.Serialization;

namespace SecureMemo.FileStorageModels
{
    [DataContract(Name = "StorageDirectory")]
    public class StorageDirectory
    {
        [DataMember(Name = "Id", Order = 1)]
        public int Id { get; set; }

        /// <summary>
        ///     The parent directory's id. The root directory is its own parent (both are 0).
        /// </summary>
        [DataMember(Name = "ParentId", Order = 2)]
        public int ParentId { get; set; }

        [DataMember(Name = "DirectoryName", Order = 3)]
        public string DirectoryName { get; set; }

        [DataMember(Name = "CreateDate", Order = 4)]
        public DateTime CreateDate { get; set; }

        [DataMember(Name = "ModifiedDate", Order = 5)]
        public DateTime ModifiedDate { get; set; }
    }
}

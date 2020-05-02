using SecureMemo.FileStorageModels;

namespace SecureMemo.Services
{
    public class FileStorageService : ServiceBase
    {
        private StorageFileSystem _storageFileSystem;

        public StorageFileSystem FileSystem => _storageFileSystem;

        public FileStorageService()
        {
            CreateEmptyFileSystem();
        }

        public FileStorageService(StorageFileSystem storageFileSystem)
        {
            _storageFileSystem = storageFileSystem;
        }


        public void Save(string databaseFilePath)
        {
            _storageFileSystem.SaveToFile(databaseFilePath);
        }

        public void Load(string databaseFilePath)
        {
            _storageFileSystem = StorageFileSystem.LoadFileSystem(databaseFilePath);
        }

        private void CreateEmptyFileSystem()
        {
            _storageFileSystem = StorageFileSystem.CreateNewFileSystem();
        }
    }
}
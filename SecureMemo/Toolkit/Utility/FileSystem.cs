using System.IO;

namespace SecureMemo.Toolkit.Utility
{
    public static class FileSystem
    {
        public static bool IsValidDirectory(string path)
        {
            return !string.IsNullOrWhiteSpace(path) && Directory.Exists(path);
        }
    }
}

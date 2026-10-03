using System;
using System.IO;
using Serilog;

namespace SecureMemo.Services
{
    /// <summary>
    ///     Temporary decrypted copies of stored files, written so they can be opened in their default
    ///     application. Copies are read-only (they are not saved back into the file storage) and are
    ///     removed by <see cref="Clear" />; anything left behind by a crash is removed at the next start
    ///     by <see cref="RemoveAll" />.
    /// </summary>
    public sealed class DecryptedFileCache
    {
        public static readonly string DefaultRootPath = Path.Combine(Path.GetTempPath(), "SecureMemo.OpenFiles");

        private readonly string _sessionPath;
        private int _fileCounter;

        public DecryptedFileCache(string rootPath)
        {
            _sessionPath = Path.Combine(rootPath, Guid.NewGuid().ToString("N"));
        }

        /// <summary>
        ///     Writes a read-only copy of a file and returns its path. Each copy gets its own folder so
        ///     the original file name (and so its default application) is kept.
        /// </summary>
        public string WriteFile(string fileName, byte[] content)
        {
            string folder = Path.Combine(_sessionPath, (++_fileCounter).ToString());
            Directory.CreateDirectory(folder);

            string filePath = Path.Combine(folder, Path.GetFileName(fileName));
            File.WriteAllBytes(filePath, content);
            File.SetAttributes(filePath, FileAttributes.ReadOnly);

            return filePath;
        }

        /// <summary>
        ///     Deletes this session's copies. Files still open in another application are left for
        ///     <see cref="RemoveAll" /> to clean up later.
        /// </summary>
        /// <returns>True if everything was deleted.</returns>
        public bool Clear()
        {
            return DeleteDirectory(_sessionPath);
        }

        /// <summary>
        ///     Deletes every cached copy, including ones from earlier sessions.
        /// </summary>
        public static bool RemoveAll(string rootPath)
        {
            return DeleteDirectory(rootPath);
        }

        private static bool DeleteDirectory(string path)
        {
            if (!Directory.Exists(path))
                return true;

            bool allDeleted = true;
            foreach (string file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                try
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    Log.Warning(ex, "Could not delete decrypted copy {File}", file);
                    allDeleted = false;
                }
            }

            if (!allDeleted)
                return false;

            try
            {
                Directory.Delete(path, true);
                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                Log.Warning(ex, "Could not delete decrypted copies folder {Path}", path);
                return false;
            }
        }
    }
}

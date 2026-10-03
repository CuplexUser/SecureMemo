using System;
using System.IO;

namespace UnitTests.TestSupport
{
    /// <summary>
    ///     A uniquely named directory under the system temp folder that is deleted on dispose.
    /// </summary>
    public sealed class TempDirectory : IDisposable
    {
        public TempDirectory(string prefix = "sm_test_")
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), prefix + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public string Combine(string fileName)
        {
            return System.IO.Path.Combine(Path, fileName);
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, true);
        }
    }
}

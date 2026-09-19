using System.IO;

namespace SecureMemo.Toolkit.Compression.SevenZip
{
    public interface IWriteCoderProperties
    {
        void WriteCoderProperties(Stream outStream);
    }
}
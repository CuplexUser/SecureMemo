using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Threading.Tasks;
using SecureMemo.Toolkit.Compression.SevenZip.Compress.LZMA;
using SecureMemo.Toolkit.Encryption;
using SecureMemo.Toolkit.Storage.Models;
using ProtoBuf;
using Serilog;

namespace SecureMemo.Toolkit.Storage
{
    /// <summary>
    ///     Saves [DataContract] objects as protobuf, LZMA-compressed in parallel 2 MB blocks and then
    ///     AES-encrypted, and reads them back.
    /// </summary>
    public class StorageManager
    {
        private const int BlockSize = 0x200000;
        private static readonly object FileLock = new object();
        private readonly StorageManagerSettings _settings;

        public StorageManager(StorageManagerSettings settings)
        {
            _settings = settings ?? throw new ArgumentException("StorageManagerSettings was null");
        }

        public bool SerializeObjectToFile(object obj, string path)
        {
            if (obj == null)
                throw new ArgumentException("serializableObject is not serializable");

            var encryptionManager = new EncryptionManager();
            MemoryStream ms = SerializeAndCompressObjectToMemoryStream(obj);

            lock (FileLock)
            {
                return encryptionManager.EncryptAndSaveFile(path, ms, _settings.GetPassword());
            }
        }

        public T DeserializeObjectFromFile<T>(string path)
        {
            var encryptionManager = new EncryptionManager();
            Stream input = null;
            var output = new MemoryStream();
            try
            {
                input = encryptionManager.DecryptFileToMemoryStream(path, _settings.GetPassword());
                input.Position = 0;

                if (CompressionFileHeader.VerifyFileHeader(input))
                    DeflateDataMultithreaded(input, output);
                else
                    // Plain single LZMA stream, as written without the multi-block header.
                    DeflateData(input, output, input.Length).RunSynchronously();

                output.Flush();
                output.Position = 0;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in StorageManager.DeserializeObjectFromFile()");
                throw new CryptographicUnexpectedOperationException("DeSerializeAndDecompressObjectFromEncryptedFile");
            }
            finally
            {
                input?.Close();
            }

            return Serializer.Deserialize<T>(output);
        }

        private MemoryStream SerializeAndCompressObjectToMemoryStream(object obj)
        {
            var msInput = new MemoryStream();
            var msOutput = new MemoryStream();

            if (!Attribute.GetCustomAttributes(obj.GetType()).OfType<DataContractAttribute>().Any())
                throw new ArgumentException("input object is not serializable as a DataContract", nameof(obj));

            Serializer.NonGeneric.Serialize(msInput, obj);
            msInput.Position = 0;
            CompressDataMultithreaded(msInput, msOutput);

            GC.Collect();
            return msOutput;
        }

        private void CompressDataMultithreaded(Stream input, Stream output)
        {
            var compressionFileHeader = new CompressionFileHeader(input.Length, BlockSize);

            int sizeOfHeader = compressionFileHeader.FileHeaderSize;
            output.Position = sizeOfHeader;

            long bytesLeft = input.Length;

            var tasks = new Task[_settings.NumberOfThreads];
            var outMemoryStreams = new MemoryStream[_settings.NumberOfThreads];
            var inputBlockSizeArray = new int[_settings.NumberOfThreads];

            input.Position = 0;
            while (bytesLeft > 0)
            {
                int taskCount = 0;
                for (int i = 0; i < tasks.Length; i++)
                {
                    int encodeSize = Math.Min(BlockSize, (int) bytesLeft);

                    if (encodeSize <= 0)
                        break;

                    taskCount++;
                    var buffer = new byte[encodeSize];
                    int bytesRead = input.Read(buffer, 0, buffer.Length);
                    bytesLeft -= bytesRead;

                    if (bytesRead == 0)
                        break;

                    var inputStream = new MemoryStream(buffer);
                    var outStream = new MemoryStream();
                    outMemoryStreams[i] = outStream;
                    inputBlockSizeArray[i] = bytesRead;
                    tasks[i] = new Task(() => { CompressData(inputStream, outStream, bytesRead); });
                    tasks[i].Start();
                }

                Task.WaitAll(tasks.Take(taskCount).ToArray());

                for (int i = 0; i < taskCount; i++)
                {
                    var compressionBlock = new CompressionBlock();
                    var outBytes = outMemoryStreams[i].ToArray();
                    outMemoryStreams[i] = null;
                    compressionBlock.CompressedBlockSize = outBytes.Length;
                    compressionBlock.UncompressedBlockSize = inputBlockSizeArray[i];
                    compressionBlock.StartPosition = output.Position;
                    compressionBlock.EndPosition = output.Position + outBytes.Length;

                    output.Write(outBytes, 0, outBytes.Length);

                    compressionFileHeader.CompressedDataBlocks.Add(compressionBlock);
                }
            }

            // Write file header
            output.Position = 0;
            var headerBytes = compressionFileHeader.ToBytes();
            output.Write(headerBytes, 0, headerBytes.Length);
        }

        private void DeflateDataMultithreaded(Stream inputDataStream, Stream outputStream)
        {
            CompressionFileHeader compressionFileHeader = CompressionFileHeader.DecodeHeader(inputDataStream);
            inputDataStream.Position = compressionFileHeader.FileHeaderSize;

            int currentBlock = 0;
            var decoderTasks = new Task[_settings.NumberOfThreads];
            var outputMemoryStreams = new MemoryStream[_settings.NumberOfThreads];

            while (currentBlock < compressionFileHeader.NumberOfBlocks)
            {
                int taskCount = 0;
                for (int i = 0; i < _settings.NumberOfThreads; i++)
                {
                    CompressionBlock dataBlock = compressionFileHeader.CompressedDataBlocks[currentBlock];
                    outputMemoryStreams[i] = new MemoryStream();
                    var buffer = new byte[dataBlock.CompressedBlockSize];
                    inputDataStream.ReadExactly(buffer, 0, buffer.Length);
                    var inputStream = new MemoryStream(buffer) {Position = 0};
                    decoderTasks[i] = DeflateData(inputStream, outputMemoryStreams[i], dataBlock.CompressedBlockSize);
                    decoderTasks[i].Start();
                    currentBlock++;
                    taskCount++;

                    if (currentBlock == compressionFileHeader.NumberOfBlocks)
                        break;
                }

                Task.WaitAll(decoderTasks.Take(taskCount).ToArray());

                for (int i = 0; i < taskCount; i++)
                {
                    var buffer = outputMemoryStreams[i].ToArray();
                    outputStream.Write(buffer, 0, buffer.Length);
                }
            }
        }

        private static void CompressData(Stream inputStream, Stream outStream, long inputSize)
        {
            var coder = new Encoder();

            // Write the encoder properties
            coder.WriteCoderProperties(outStream);

            // Write the decompressed file size.
            outStream.Write(BitConverter.GetBytes(inputSize), 0, 8);

            coder.Code(inputStream, outStream, inputSize, -1, null);
        }

        private static Task DeflateData(Stream inputStream, Stream outStream, long compressedSize)
        {
            return new Task(() =>
            {
                var decoder = new Decoder();

                // Read the decoder properties
                var properties = new byte[5];
                inputStream.ReadExactly(properties, 0, 5);

                // Read in the decompress block size.
                var fileLengthBytes = new byte[8];
                inputStream.ReadExactly(fileLengthBytes, 0, 8);
                long blockSize = BitConverter.ToInt64(fileLengthBytes, 0);

                decoder.SetDecoderProperties(properties);

                decoder.Code(inputStream, outStream, compressedSize, blockSize, null);
            });
        }
    }
}

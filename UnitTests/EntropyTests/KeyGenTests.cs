using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SecureMemo.Managers;
using UnitTests.Config;

namespace UnitTests.EntropyTests
{
    [TestClass]
    public class KeyGenTests
    {
        
        private ILifetimeScope scope;
        [TestMethod]
        public void TestBase64KeyRandomness()
        {
            // Generate 1 MB of data
            CryptoManager cryptoManager = new CryptoManager();
            StringBuilder sb = new StringBuilder();
            TestAppBuildConfig buildConfig = scope.Resolve<TestAppBuildConfig>();
            string filePath = Path.Combine(buildConfig.TestDataOutputPath, "RandomSequence.txt");
            const int outputSize = 1048576;

            var cancelToken = new CancellationToken(false);
            const int maxTaskCount = 8;

            List<Task> tasks = new List<Task>();
            int dataLeftToGenerate = outputSize;
            int dataPerThread = outputSize / maxTaskCount;
            int dataWritten = 0;

            Stopwatch stopwatch = Stopwatch.StartNew();
            // CreateFileStream
            FileStream fs = null;
            const int bufferSize = 262144;
            try
            {
                fs = File.Create(filePath);
                StreamWriter streamWriter = new StreamWriter(fs, Encoding.ASCII, bufferSize);

                Task mainTask = Task.Factory.StartNew(() =>
                {
                    while (tasks.Count <= maxTaskCount && dataLeftToGenerate > 0)
                    {
                        AsyncState state = new AsyncState();
                        state.SetCryptoManager(cryptoManager: ref cryptoManager);
                        state.LengthToGenerate = dataPerThread;
                        dataLeftToGenerate -= state.LengthToGenerate;
                        Task t = Task.Factory.StartNew(GenerateRandomData, state, cancelToken);
                        t.ContinueWith(async (antecedent) =>
                        {
                            if (antecedent.AsyncState is AsyncState completedState && dataWritten <= outputSize)
                            {
                                await streamWriter.WriteAsync(completedState.GeneratedData).ConfigureAwait(true);
                                dataWritten += completedState.GeneratedData.Length;
                                await streamWriter.FlushAsync().ConfigureAwait(true);
                                await fs.FlushAsync(cancelToken);
                            }
                        }, cancelToken);
                        tasks.Add(t);
                    }

                    Task.WaitAny(tasks.ToArray(), cancelToken);
                    Task.WaitAll(tasks.ToArray());
                }, cancelToken);

                mainTask.Wait(cancelToken);
              
                streamWriter.Close();
                fs.Flush(true);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                fs?.Close();
            }

            // check for a newly generated file that fits the specifications
            Assert.IsTrue(File.Exists(filePath), "No output file was generated!");

            stopwatch.Stop();
            FileInfo fileInfo = new FileInfo(filePath);

            // Output size is only required to be atlest 1 million bytes long for testing randomness so if the target file is of by 2 bytes makes no difference.
            Assert.IsTrue(fileInfo.Length >= outputSize && fileInfo.Length <= outputSize + 512, "The outputfile did not have the correct size");
            Assert.IsTrue(fileInfo.LastWriteTime < DateTime.Now.AddSeconds(stopwatch.Elapsed.Seconds + 10), "The file was not written during this test");
        }

        

        [TestMethod]
        public void TestBinaryKeyRandomness()
        {
            // Generate 1 MB of data
            CryptoManager cryptoManager = new CryptoManager();
            StringBuilder sb = new StringBuilder();
            TestAppBuildConfig buildConfig = scope.Resolve<TestAppBuildConfig>();
            string filePath = Path.Combine(buildConfig.TestDataOutputPath, "RandomSequence.dat");
            const int outputSize = 1048576;

            var cancelToken = new CancellationToken(false);
            const int maxTaskCount = 8;

            List<Task> tasks = new List<Task>();
            int dataLeftToGenerate = outputSize;
            int dataPerThread = outputSize / maxTaskCount;
            int dataWritten = 0;

            Stopwatch stopwatch = Stopwatch.StartNew();
            // CreateFileStream
            FileStream fs = null;
            const int bufferSize = 65536;
            try
            {
                fs = File.Create(filePath);

                Task mainTask = Task.Factory.StartNew(() =>
                {
                    while (tasks.Count <= maxTaskCount && dataLeftToGenerate > 0)
                    {
                        AsyncState state = new AsyncState();
                        state.SetCryptoManager(cryptoManager: ref cryptoManager);
                        state.LengthToGenerate = dataPerThread;
                        dataLeftToGenerate -= state.LengthToGenerate;
                        Task t = Task.Factory.StartNew(GenerateRandomBinaryData, state, cancelToken);
                        t.ContinueWith(async (antecedent) =>
                        {
                            if (antecedent.AsyncState is AsyncState completedState && dataWritten <= outputSize)
                            {
                                await fs.WriteAsync(completedState.GeneratedBinaryData,0,completedState.GeneratedBinaryData.Length, cancelToken).ConfigureAwait(true);
                                await fs.FlushAsync(cancelToken);
                                dataWritten += completedState.GeneratedData.Length;
                            }
                        }, cancelToken);
                        tasks.Add(t);
                    }

                    Task.WaitAny(tasks.ToArray(), cancelToken);
                    Task.WaitAll(tasks.ToArray());
                }, cancelToken);

                mainTask.Wait(cancelToken);

                fs.Flush(true);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                fs?.Close();
            }

            // check for a newly generated file that fits the specifications
            Assert.IsTrue(File.Exists(filePath), "No output file was generated!");

            stopwatch.Stop();
            FileInfo fileInfo = new FileInfo(filePath);

            // Output size is only required to be atlest 1 million bytes long for testing randomness so if the target file is of by 2 bytes makes no difference.
            Assert.IsTrue(fileInfo.Length >= outputSize && fileInfo.Length <= outputSize + 512, "The outputfile did not have the correct size");
            Assert.IsTrue(fileInfo.LastWriteTime < DateTime.Now.AddSeconds(stopwatch.Elapsed.Seconds + 10), "The file was not written during this test");
        }

        internal class AsyncState
        {
            private CryptoManager _cryptoManager;
            public int LengthToGenerate { get; set; }
            public string GeneratedData { get; set; }
            public byte[] GeneratedBinaryData { get; set; }


            public ref CryptoManager GetInstance()
            {
                return ref _cryptoManager;
            }

            public void SetCryptoManager(ref CryptoManager cryptoManager)
            {
                _cryptoManager = cryptoManager;
                GeneratedData = "";
            }

        }

        private void GenerateRandomBinaryData(object obj)
        {
            if (obj is AsyncState state)
            {
                MemoryStream ms =new MemoryStream();
                while (ms.Length < state.LengthToGenerate)
                {
                    byte[] buffer = state.GetInstance().GenerateBinaryKey();
                    ms.Write(buffer, 0, buffer.Length);
                }

                state.GeneratedBinaryData = ms.ToArray().Take(state.LengthToGenerate).ToArray();
                ms.Dispose();
            }
        }

        private void GenerateRandomData(object obj)
        {
            if (obj is AsyncState state)
            {
                StringBuilder sb = new StringBuilder();
                while (sb.Length < state.LengthToGenerate)
                {
                    sb.Append(state.GetInstance().GenerateKey());
                }

                state.GeneratedData = sb.ToString();
                sb.Clear();
            }
        }



        [TestInitialize]
        public void TestClassInit()
        {
            TestSystemInit.SetupRuntimeEnvironment();
            scope = TestSystemInit.Scope;
        }
    }
}

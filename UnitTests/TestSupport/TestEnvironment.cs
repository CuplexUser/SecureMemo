using System;
using System.IO;
using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SecureMemo.Configuration;
using SecureMemo.Toolkit.Configuration;
using Serilog;

namespace UnitTests.TestSupport
{
    /// <summary>
    ///     Builds the application's real Autofac container once per test run, pointed at a temporary
    ///     user data folder so tests never touch the real settings, database or log files.
    /// </summary>
    [TestClass]
    public class TestEnvironment
    {
        public static string UserDataPath { get; private set; }

        public static IContainer Container { get; private set; }

        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            UserDataPath = Path.Combine(Path.GetTempPath(), "sm_userdata_" + Guid.NewGuid().ToString("N")) + "\\";
            Directory.CreateDirectory(UserDataPath);
            ApplicationBuildConfig.SetOverrideUserDataPath(UserDataPath);

            // Built once: LoggingModule replaces the global Serilog logger (and opens a log file)
            // every time a container is created.
            Container = AutofacConfig.CreateContainer();
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            Container?.Dispose();
            Log.CloseAndFlush();

            try
            {
                Directory.Delete(UserDataPath, true);
            }
            catch (IOException)
            {
                // Best effort; a lingering file handle shouldn't fail the run.
            }
        }
    }
}

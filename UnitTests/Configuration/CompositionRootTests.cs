using System.IO;
using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SecureMemo;
using SecureMemo.DataModels;
using SecureMemo.Managers;
using SecureMemo.Services;
using SecureMemo.TextSearchModels;
using SecureMemo.Toolkit.Configuration;
using SecureMemo.Toolkit.Storage.Memory;
using UnitTests.TestSupport;

namespace UnitTests.Configuration
{
    [TestClass]
    public class CompositionRootTests
    {
        [TestMethod]
        public void Container_ResolvesEveryServiceTheFormsDependOn()
        {
            using var scope = TestEnvironment.Container.BeginLifetimeScope();

            Assert.IsNotNull(scope.Resolve<AppSettingsService>());
            Assert.IsNotNull(scope.Resolve<MemoStorageService>());
            Assert.IsNotNull(scope.Resolve<FileStorageService>());
            Assert.IsNotNull(scope.Resolve<PasswordStorage>());
            Assert.IsNotNull(scope.Resolve<MainFormLogicManager>());
            Assert.IsNotNull(scope.Resolve<TabSearchEngine>());
        }

        [TestMethod]
        public void Container_RegistersTheFormsResolvedAtRuntime()
        {
            // FormMain and the dialogs it opens via ILifetimeScope.Resolve; constructing them here
            // would need a UI thread, so only the registrations are checked.
            Assert.IsTrue(TestEnvironment.Container.IsRegistered<FormMain>());
            Assert.IsTrue(TestEnvironment.Container.IsRegistered<FormSettings>());
            Assert.IsTrue(TestEnvironment.Container.IsRegistered<FormRestoreBackup>());
            Assert.IsTrue(TestEnvironment.Container.IsRegistered<FormFileManager>());
            Assert.IsTrue(TestEnvironment.Container.IsRegistered<FormTabEdit>());
        }

        [TestMethod]
        public void Container_SharesOneLogicManagerAndPasswordStorage()
        {
            using var scope1 = TestEnvironment.Container.BeginLifetimeScope();
            using var scope2 = TestEnvironment.Container.BeginLifetimeScope();

            Assert.AreSame(scope1.Resolve<MainFormLogicManager>(), scope2.Resolve<MainFormLogicManager>());
            Assert.AreSame(scope1.Resolve<PasswordStorage>(), scope2.Resolve<PasswordStorage>());
        }

        [TestMethod]
        public void TabPageDataCollection_ResolvesToLogicManagersLiveCollection()
        {
            // TabSearchEngine must search whatever database is currently open, not the one that
            // happened to be open when the container was built.
            using var scope = TestEnvironment.Container.BeginLifetimeScope();
            var logicManager = scope.Resolve<MainFormLogicManager>();
            logicManager.ResetToDefaultDatabase();

            Assert.AreSame(logicManager.GetActiveTabPageDataCollection(), scope.Resolve<TabPageDataCollection>());
        }

        [TestMethod]
        public void UserDataPath_Override_IsUsedForSettingsAndLogFiles()
        {
            Assert.AreEqual(TestEnvironment.UserDataPath, ApplicationBuildConfig.UserDataPath);

            string logFilePath = ApplicationBuildConfig.ApplicationLogFilePath();
            Assert.AreEqual(TestEnvironment.UserDataPath, Path.GetDirectoryName(logFilePath) + "\\");
            StringAssert.EndsWith(logFilePath, ".log");
        }
    }
}

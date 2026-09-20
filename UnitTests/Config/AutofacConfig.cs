using System.IO;
using System.Reflection;
using Autofac;
using SecureMemo.Toolkit.ConfigHelper;
using SecureMemo.Toolkit.Storage.Memory;
using SecureMemo.DataModels;
using SecureMemo.Managers;
using SecureMemo.Services;
using SecureMemo.TextSearchModels;
using SecureMemo.Utility;

namespace UnitTests.Config
{
    public static class AutofacConfig
    {
        public static IContainer CreateContainer()
        {
            TestAppBuildConfig testAppBuildConfig = new TestAppBuildConfig();

            string settingsFolderPath = testAppBuildConfig.UserDataPath;

            string iniConfigFilePath = Path.Combine(testAppBuildConfig.UserDataPath, "ApplicationSettings.ini");
            var appSettings = new AppSettingsService(ConfigHelper.GetDefaultSettings(), new IniConfigFileManager(), iniConfigFilePath);
            var memoStorageService = new MemoStorageService(appSettings, settingsFolderPath);
            var passwordStorageMgr = new PasswordStorage();

            // Create autofac container
            var builder = new ContainerBuilder();
            builder.RegisterInstance(testAppBuildConfig).As<TestAppBuildConfig>().SingleInstance();
            builder.RegisterInstance(appSettings).As<AppSettingsService>().SingleInstance();
            builder.RegisterInstance(memoStorageService).As<MemoStorageService>().SingleInstance();
            builder.RegisterInstance(new FileStorageService()).As<FileStorageService>().SingleInstance();
            builder.RegisterInstance(passwordStorageMgr).As<PasswordStorage>().SingleInstance();

            builder.RegisterAssemblyModules(Assembly.GetExecutingAssembly());


            builder.RegisterType<MainFormLogicManager>().AsSelf().SingleInstance();

            builder.Register(context => context.Resolve<MainFormLogicManager>().GetActiveTabPageDataCollection())
                .As<TabPageDataCollection>()
                .InstancePerDependency();

            // Register instantiation of Search engine
            builder.RegisterType<TabSearchEngine>().AsSelf().InstancePerLifetimeScope();


            
            var container = builder.Build();

            return container;
        }
    }
}

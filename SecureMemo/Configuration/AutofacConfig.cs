using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Autofac;
using AutoMapper;
using SecureMemo.Toolkit.ConfigHelper;
using SecureMemo.Toolkit.Configuration;
using SecureMemo.Toolkit.Storage.Memory;
using SecureMemo.DataModels;
using SecureMemo.Managers;
using SecureMemo.Services;
using SecureMemo.TextSearchModels;
using SecureMemo.Utility;

namespace SecureMemo.Configuration
{
    /// <summary>
    ///     Only Referenced from Program.cs on application startup
    /// </summary>
    public static class AutofacConfig
    {
        /// <summary>
        ///     Creates the Autofac container.
        /// </summary>
        /// <returns></returns>
        public static IContainer CreateContainer()
        {
            string settingsFolderPath = ApplicationBuildConfig.UserDataPath;
            string iniConfigFilePath = Path.Combine(ApplicationBuildConfig.UserDataPath, "ApplicationSettings.ini");
            var appSettings = new AppSettingsService(ConfigHelper.GetDefaultSettings(), new IniConfigFileManager(), iniConfigFilePath);
            var memoStorageService = new MemoStorageService(appSettings, settingsFolderPath);
            var passwordStorageMgr = new PasswordStorage();

            // Create autofac container
            var builder = new ContainerBuilder();
            builder.RegisterInstance(appSettings).As<AppSettingsService>().SingleInstance();
            builder.RegisterInstance(memoStorageService).As<MemoStorageService>().SingleInstance();
            builder.RegisterInstance(new FileStorageService()).As<FileStorageService>().SingleInstance();
            builder.RegisterInstance(passwordStorageMgr).As<PasswordStorage>().SingleInstance();


            builder.RegisterAssemblyModules(Assembly.GetExecutingAssembly());


            builder.RegisterType<MainFormLogicManager>().AsSelf().SingleInstance();

            // TabSearchEngine needs the currently-active TabPageDataCollection; resolving it via
            // MainFormLogicManager (rather than a fixed instance) ensures a freshly-resolved
            // TabSearchEngine always sees the current database, not whichever one was open first.
            builder.Register(context => context.Resolve<MainFormLogicManager>().GetActiveTabPageDataCollection())
                .As<TabPageDataCollection>()
                .InstancePerDependency();

            // Register instantiation of Search engine
            builder.RegisterType<TabSearchEngine>().AsSelf().InstancePerLifetimeScope();

            // Register Automapper and configure the mapping bindings 
            builder.Register(context => context.Resolve<MapperConfiguration>()
                    .CreateMapper())
                .As<IMapper>()
                .AutoActivate()
                .SingleInstance();

            builder.Register(Configure)
                .AutoActivate()
                .AsSelf()
                .AsImplementedInterfaces()
                .SingleInstance();

            var container = builder.Build();

            return container;
        }


        private static MapperConfiguration Configure(IComponentContext context)
        {
            var configuration = new MapperConfiguration(cfg =>
            {
                var innerContext = context.Resolve<IComponentContext>();
                cfg.ConstructServicesUsing(innerContext.Resolve);

                foreach (var profile in context.Resolve<IEnumerable<Profile>>()) cfg.AddProfile(profile);
            });

            return configuration;
        }
    }
}
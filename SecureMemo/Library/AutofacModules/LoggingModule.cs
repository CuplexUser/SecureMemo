﻿using Autofac;
using AutofacSerilogIntegration;
using GeneralToolkitLib.Configuration;
using Serilog;
using Serilog.Events;

namespace SecureMemo.Library.AutofacModules
{
    public class LoggingModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var logLevel = LogEventLevel.Debug;
            if (!ApplicationBuildConfig.DebugMode) logLevel = LogEventLevel.Warning;

            Log.Logger = new LoggerConfiguration()
                .WriteTo.RollingFile(ApplicationBuildConfig.ApplicationLogFilePath(true),
                    fileSizeLimitBytes: 1048576,
                    retainedFileCountLimit: 31,
                    restrictedToMinimumLevel: logLevel,
                    buffered: false,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.ff} [{Level}] {Message}{NewLine}{Exception}{Data}")
                .Enrich.FromLogContext()
                .MinimumLevel.Is(logLevel)
                .CreateLogger();

            builder.RegisterLogger();
        }
    }
}
using Autofac;
using AutofacSerilogIntegration;
using SecureMemo.Toolkit.Configuration;
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
                .WriteTo.File(ApplicationBuildConfig.ApplicationLogFilePath(),
                    fileSizeLimitBytes: 1048576,
                    retainedFileCountLimit: 31,
                    rollOnFileSizeLimit: true,
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
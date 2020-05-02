using Autofac;
using GeneralToolkitLib.Storage.Memory;

namespace SecureMemo.Library.AutofacModules
{
    public class SecureMemoMudule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<PasswordStorage>().AsSelf().SingleInstance();
            builder.RegisterType<FormMain>();
            builder.RegisterType<FormSettings>();
            builder.RegisterType<FormRestoreBackup>();
            builder.RegisterType<FormFileManager>();
            builder.RegisterType<FormTabEdit>();
        }
    }
}

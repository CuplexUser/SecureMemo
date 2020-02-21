using System;
using System.Reflection;

namespace SecureMemo.Utility
{
    internal static class ConfigSpecificSettings
    {
        public static string GetSettingsFolderPath(bool appendBackslash)
        {
#if DEBUG
            return Environment.CurrentDirectory + "\\" + Assembly.GetExecutingAssembly().GetName().Name + (appendBackslash ? "\\" : "");
#else
            return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\" + Assembly.GetExecutingAssembly().GetName().Name + (appendBackslash ? "\\" : "");
#endif
        }
    }
}
using System;
using System.IO;
using System.Reflection;

namespace SecureMemo.Toolkit.Configuration
{
    public static class ApplicationBuildConfig
    {
        private static string _userDataPath;

        public static string ApplicationLogFilePath()
        {
            string logFilename = Assembly.GetCallingAssembly().GetName().Name;
            logFilename += $"{DateTime.Today:yyyy-MM-dd}.log";

            return Path.Combine(UserDataPath, logFilename);
        }

        public static string UserDataPath => _userDataPath ?? (_userDataPath = GetUserDataPath());

#if DEBUG
        public static bool DebugMode => true;
#else
        public static bool DebugMode => false;
#endif

        private static string GetUserDataPath()
        {
            if (DebugMode)
            {
                return GetAssemblyPath(Assembly.GetExecutingAssembly().Location);
            }

            return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\" + Assembly.GetEntryAssembly()?.GetName().Name.Replace(" ", "") + "\\";
        }

        /// <summary>
        /// Sets the override user data path. Used for tests to run properly.
        /// </summary>
        public static void SetOverrideUserDataPath(string path)
        {
            _userDataPath = path;
        }

        private static string GetAssemblyPath(string fullAssemblyPath)
        {
            if (fullAssemblyPath != null)
            {
                int lastSlash = fullAssemblyPath.LastIndexOf('\\');
                if (lastSlash > 0)
                    return fullAssemblyPath.Substring(0, lastSlash + 1);
            }
            return null;
        }
    }
}

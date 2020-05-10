using System.Drawing;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using GeneralToolkitLib.Converters;
using SecureMemo.DataModels;

namespace SecureMemo.Utility
{
    public static class ConfigHelper
    {
        private const int EmptyTabPages = 3;
        private const int WinWidth = 400;
        private const int WinHeight = 300;
        private const bool AllwaysOnTop = false;

        private static string CreateApplicationSalt()
        {
            var randomBytes = new byte[512];
            using (RandomNumberGenerator random = RandomNumberGenerator.Create())
            {
                random.GetBytes(randomBytes);
                return GeneralConverters.ByteArrayToHexString(randomBytes);
            }
        }

        public static SecureMemoAppSettings GetDefaultSettings()
        {
            var appSettings = new SecureMemoAppSettings {AlwaysOnTop = AllwaysOnTop, ApplicationSaltValue = CreateApplicationSalt(), DefaultEmptyTabPages = EmptyTabPages};
            var fontSettings = new SecureMemoFontSettings {FontFamilyName = "Arial", FontSize = 12f, Style = FontStyle.Regular};
            fontSettings.FontFamilyName = fontSettings.FontFamily.GetName(0);
            appSettings.FontSettings = fontSettings;
            appSettings.MainWindowHeight = WinHeight;
            appSettings.MainWindowWith = WinWidth;
            appSettings.PasswordDerivedString = null;

            return appSettings;
        }
        
        public static string AssemblyTitle
        {
            get
            {
                var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
                if (attributes.Length > 0)
                {
                    var titleAttribute = (AssemblyTitleAttribute)attributes[0];
                    if (titleAttribute.Title != "")
                        return titleAttribute.Title;
                }
                return Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().CodeBase);
            }
        }
    }
}
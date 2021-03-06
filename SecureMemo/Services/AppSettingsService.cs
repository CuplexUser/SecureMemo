﻿using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using GeneralToolkitLib.ConfigHelper;
using SecureMemo.DataModels;
using SecureMemo.Utility;
using Serilog;

namespace SecureMemo.Services
{
    public class AppSettingsService : ServiceBase
    {
        private readonly SecureMemoAppSettings _defaultAppSettings;
        private readonly IniConfigFileManager _iniConfigFileManager;
        private readonly string _iniConfigFilePath;

        public AppSettingsService(SecureMemoAppSettings defaultAppSettings, IniConfigFileManager iniConfigFileManager, string iniConfigFilePath)
        {
            _defaultAppSettings = defaultAppSettings;
            _iniConfigFileManager = iniConfigFileManager;
            _iniConfigFilePath = iniConfigFilePath;
            Settings = ConfigHelper.GetDefaultSettings();
            CreateAppDataDirectoryIfDirNotFound(ConfigSpecificSettings.GetSettingsFolderPath(false));
        }

        public SecureMemoAppSettings Settings { get; }


        private void CreateAppDataDirectoryIfDirNotFound(string settingsFolderPath)
        {
            try
            {
                if (!Directory.Exists(settingsFolderPath))
                    Directory.CreateDirectory(settingsFolderPath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error when creating app data folder");
            }
        }

        public void LoadSettings()
        {
            try
            {
                if (!File.Exists(_iniConfigFilePath))
                {
                    SaveSettings();
                    return;
                }

                if (!_iniConfigFileManager.LoadConfigFile(_iniConfigFilePath))
                    throw new Exception("Unable to load application settings");

                int screenWidth = Screen.PrimaryScreen.WorkingArea.Width;
                int screenHeight = Screen.PrimaryScreen.WorkingArea.Height;

                IniConfigFileSection generalConfigFileSection = _iniConfigFileManager.ConfigurationData.ConfigSections["General"];
                Settings.DefaultEmptyTabPages = int.Parse(generalConfigFileSection.ConfigItems["DefaultEmptyTabPages"]);
                Settings.ApplicationSaltValue = generalConfigFileSection.ConfigItems["ApplicationSaltValue"];
                Settings.PasswordDerivedString = generalConfigFileSection.ConfigItems["PasswordDerivedString"];
                Settings.AlwaysOnTop = bool.Parse(generalConfigFileSection.ConfigItems["AlwaysOnTop"]);

                if (int.TryParse(generalConfigFileSection.ConfigItems["MainWindowWith"], out var winSize))
                    Settings.MainWindowWith = winSize;

                if (int.TryParse(generalConfigFileSection.ConfigItems["MainWindowHeight"], out winSize))
                    Settings.MainWindowHeight = winSize;

                if (Settings.MainWindowWith < _defaultAppSettings.MainWindowWith)
                    Settings.MainWindowWith = _defaultAppSettings.MainWindowWith;

                if (Settings.MainWindowWith > screenWidth)
                    Settings.MainWindowWith = screenWidth;

                if (Settings.MainWindowHeight < _defaultAppSettings.MainWindowHeight)
                    Settings.MainWindowHeight = _defaultAppSettings.MainWindowHeight;

                if (Settings.MainWindowHeight > screenHeight)
                    Settings.MainWindowHeight = screenHeight;

                if (Settings.ApplicationSaltValue == null || Settings.ApplicationSaltValue.Length != 1024)
                    throw new Exception("ApplicationSaltValue Length must be 1024 characters");

                // Shared Database folder
                bool useSharedSyncFolder = false;
                if (generalConfigFileSection.ConfigItems["UseSharedSyncFolder"] != null)
                    bool.TryParse(generalConfigFileSection.ConfigItems["UseSharedSyncFolder"], out useSharedSyncFolder);

                Settings.UseSharedSyncFolder = useSharedSyncFolder;
                Settings.SyncFolderPath = generalConfigFileSection.ConfigItems["SyncFolderPath"];

                // Font settings
                IniConfigFileSection value;
                if (_iniConfigFileManager.ConfigurationData.ConfigSections.TryGetValue("FontSettings", out value))
                {
                    IniConfigFileSection fontConfigFileSection = value;
                    var fontSettings = new SecureMemoFontSettings
                    {
                        FontSize = Convert.ToSingle(fontConfigFileSection.ConfigItems["FontSize"]),
                        FontFamilyName = fontConfigFileSection.ConfigItems["FontFamilyName"],
                        Style = (FontStyle) Enum.Parse(typeof(FontStyle), fontConfigFileSection.ConfigItems["Style"])
                    };
                    //fontSettings.FontFamily = new Font(fontConfigFileSection.ConfigItems["FontFamily"], fontSettings.FontSize, fontSettings.Style).FontFamily;

                    Settings.FontSettings = fontSettings;
                }
                else
                {
                    Settings.FontSettings = _defaultAppSettings.FontSettings;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load application settings");
                throw;
            }
        }

        public void SaveSettings()
        {
            try
            {
                if (!_iniConfigFileManager.ConfigurationData.ConfigSections.ContainsKey("General"))
                    _iniConfigFileManager.ConfigurationData.ConfigSections.Add("General", new IniConfigFileSection());

                IniConfigFileSection generalConfigFileSection = _iniConfigFileManager.ConfigurationData.ConfigSections["General"];
                generalConfigFileSection.ConfigItems["DefaultEmptyTabPages"] = Settings.DefaultEmptyTabPages.ToString();
                generalConfigFileSection.ConfigItems["ApplicationSaltValue"] = Settings.ApplicationSaltValue;
                generalConfigFileSection.ConfigItems["PasswordDerivedString"] = Settings.PasswordDerivedString;
                generalConfigFileSection.ConfigItems["AlwaysOnTop"] = Settings.AlwaysOnTop ? "True" : "False";
                generalConfigFileSection.ConfigItems["MainWindowWith"] = Settings.MainWindowWith.ToString();
                generalConfigFileSection.ConfigItems["MainWindowHeight"] = Settings.MainWindowHeight.ToString();
                //Todo handle window states

                // Shared Database folder
                generalConfigFileSection.ConfigItems["UseSharedSyncFolder"] = Settings.UseSharedSyncFolder ? "True" : "False";
                generalConfigFileSection.ConfigItems["SyncFolderPath"] = Settings.SyncFolderPath;

                if (!_iniConfigFileManager.ConfigurationData.ConfigSections.ContainsKey("FontSettings"))
                    _iniConfigFileManager.ConfigurationData.ConfigSections.Add("FontSettings", new IniConfigFileSection());

                IniConfigFileSection fontConfigFileSection = _iniConfigFileManager.ConfigurationData.ConfigSections["FontSettings"];
                fontConfigFileSection.ConfigItems["FontFamily"] = Settings.FontSettings.FontFamily.Name;
                fontConfigFileSection.ConfigItems["FontFamilyName"] = Settings.FontSettings.FontFamilyName;
                fontConfigFileSection.ConfigItems["Style"] = Settings.FontSettings.Style.ToString();
                fontConfigFileSection.ConfigItems["FontSize"] = Settings.FontSettings.FontSize.ToString(CultureInfo.CurrentCulture);

                _iniConfigFileManager.SaveConfigFile(_iniConfigFilePath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in AppSettings SaveDatabase");
            }
        }
    }
}
﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GeneralToolkitLib.Storage;
using GeneralToolkitLib.Storage.Models;
using SecureMemo.DataModels;
using SecureMemo.Storage;
using Serilog;

namespace SecureMemo.Services
{
    public class MemoStorageService : ServiceBase
    {
        private const string DatabaseFileName = "MemoDatabase.dat";
        private const string ConfSaltVal = "l73hgwiHLwscWqHQUT7vwJSTX58K0XWZlecm77NbzmqbsF60LOEeftqSdeSvL6cB";
        private const string ConfSaltVal2 = "BxV0CQsjr6f7MbiXTqdpHN4bjyhqUX9Yd79zA2vRZLGPQj0qdTGlTwBFiK7eiFqc";
        private readonly AppSettingsService _appSettingsService;
        private readonly string _databaseFilePath;

        public MemoStorageService(AppSettingsService appSettingsService, string databaseFilePath)
        {
            _appSettingsService = appSettingsService;
            _databaseFilePath = databaseFilePath;
        }

        public bool FoundDatabaseErrors { get; private set; }

        private string GetFullPathToDatabaseFile()
        {
            return _databaseFilePath + "\\" + DatabaseFileName;
        }

        private string GetFullPathToSharedDatabaseFile()
        {
            return _appSettingsService.Settings.SyncFolderPath + "\\" + DatabaseFileName;
        }

        private string GetFullPathToSharedDecryptedConfigFile()
        {
            return _appSettingsService.Settings.SyncFolderPath + "\\" + "ApplicationSettings.dat";
        }

        public bool DatabaseExists()
        {
            return File.Exists(GetFullPathToDatabaseFile());
        }

        public TabPageDataCollection LoadTabPageCollection(string password)
        {
            TabPageDataCollection tabPageDataCollection = null;
            try
            {
                var settings = new StorageManagerSettings(true, Environment.ProcessorCount, true, password);
                var storageManager = new StorageManager(settings);
                tabPageDataCollection = storageManager.DeserializeObjectFromFile<TabPageDataCollection>(GetFullPathToDatabaseFile(), null);

                PageDataCollectionManager collectionManager = new PageDataCollectionManager(tabPageDataCollection);
                FoundDatabaseErrors = !collectionManager.ValidateDataCollectionIntegrity();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading database");
            }

            return tabPageDataCollection;
        }

        public bool SaveTabPageCollection(TabPageDataCollection tabPageDataCollection, string password)
        {
            bool success = false;
            try
            {
                var settings = new StorageManagerSettings(true, Environment.ProcessorCount, true, password);
                var storageManager = new StorageManager(settings);
                success = storageManager.SerializeObjectToFile(tabPageDataCollection, GetFullPathToDatabaseFile(), null);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error saving database");
            }


            FoundDatabaseErrors = false;
            return success;
        }

        public bool SaveTabPageCollectionToSharedFolder(TabPageDataCollection tabPageDataCollection, string password)
        {
            bool success = false;
            try
            {
                var settings = new StorageManagerSettings(true, Environment.ProcessorCount, true, password);
                var storageManager = new StorageManager(settings);

                string encodedConfigFilePath = GetFullPathToSharedDatabaseFile();
                if (File.Exists(encodedConfigFilePath))
                    File.Delete(encodedConfigFilePath);

                settings.Password = ConfSaltVal + settings.Password + ConfSaltVal2;
                File.Copy(GetFullPathToDatabaseFile(), encodedConfigFilePath);
                success = storageManager.SerializeObjectToFile(_appSettingsService.Settings, GetFullPathToSharedDecryptedConfigFile(), null);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error saving database");
            }

            FoundDatabaseErrors = false;
            return success;
        }

        public IEnumerable<BackupFileInfo> GetBackupFiles()
        {
            string backupPath = _databaseFilePath + "\\backup\\";
            var dirInfo = new DirectoryInfo(backupPath);

            if (!dirInfo.Exists)
                return null;

            var backupFiles = dirInfo.GetFiles("*MemoDatabase.dat");
            return backupFiles.Select(backupFile => new BackupFileInfo {Name = backupFile.Name, CreatedDate = backupFile.CreationTime, FullName = backupFile.FullName}).ToList();
        }

        public void MakeBackup()
        {
            if (!Directory.Exists(_databaseFilePath))
                throw new Exception("No database found, create a new database first.");

            string backupPath = _databaseFilePath + "\\backup\\";

            if (!Directory.Exists(backupPath))
                Directory.CreateDirectory(backupPath);

            File.Copy(GetFullPathToDatabaseFile(), backupPath + DateTime.Now.ToString("yyyyMMdd_HH.mm.ss_") + DatabaseFileName);
        }

        public void RestoreBackup(BackupFileInfo backupFileInfo)
        {
            if (!File.Exists(backupFileInfo.FullName))
                throw new Exception("Backupfile with name: " + backupFileInfo.Name + " does not exist!");

            string pathToDatabaseFile = GetFullPathToDatabaseFile();

            if (File.Exists(pathToDatabaseFile))
                File.Delete(pathToDatabaseFile);

            File.Copy(backupFileInfo.FullName, pathToDatabaseFile);
            File.Delete(backupFileInfo.FullName);
        }

        public RestoreSyncDataResult RestoreBackupFromSyncFolder(string password)
        {
            var restoreSyncDataResult = new RestoreSyncDataResult();
            string dbFilePath = GetFullPathToDatabaseFile();
            try
            {
                if (!File.Exists(GetFullPathToSharedDatabaseFile()))
                {
                    restoreSyncDataResult.ErrorCode = RestoreSyncDataErrorCodes.MemoDatabaseFileNotFound;
                }
                else if (!File.Exists(GetFullPathToSharedDecryptedConfigFile()))
                {
                    restoreSyncDataResult.ErrorCode = restoreSyncDataResult.ErrorCode | RestoreSyncDataErrorCodes.ApplicationSettingsFileNotFound;
                }
                else
                {
                    var settings = new StorageManagerSettings(true, Environment.ProcessorCount, true, ConfSaltVal + password + ConfSaltVal2);
                    var storageManager = new StorageManager(settings);

                    var secureMemoAppSettings = storageManager.DeserializeObjectFromFile<SecureMemoAppSettings>(GetFullPathToSharedDecryptedConfigFile(), null);

                    if (string.IsNullOrWhiteSpace(secureMemoAppSettings.ApplicationSaltValue) || string.IsNullOrWhiteSpace(secureMemoAppSettings.PasswordDerivedString))
                    {
                        restoreSyncDataResult.ErrorCode = restoreSyncDataResult.ErrorCode | RestoreSyncDataErrorCodes.ApplicationSettingsFileParseError;
                        restoreSyncDataResult.ErrorText = "Invalid password";
                    }
                    else
                    {
                        var s = _appSettingsService.Settings;
                        s.ApplicationSaltValue = secureMemoAppSettings.ApplicationSaltValue;
                        s.PasswordDerivedString = secureMemoAppSettings.PasswordDerivedString;
                        s.FontSettings = secureMemoAppSettings.FontSettings;
                        s.UseSharedSyncFolder = secureMemoAppSettings.UseSharedSyncFolder;
                        s.DefaultEmptyTabPages = secureMemoAppSettings.DefaultEmptyTabPages;
                        s.MainWindowHeight = secureMemoAppSettings.MainWindowHeight;
                        s.MainWindowWith = secureMemoAppSettings.MainWindowWith;
                        _appSettingsService.SaveSettings();
                        _appSettingsService.LoadSettings();

                        if (File.Exists(dbFilePath))
                            File.Delete(dbFilePath);

                        File.Copy(GetFullPathToSharedDatabaseFile(), dbFilePath);
                        restoreSyncDataResult.Successful = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error when calling RestoreBackupFromSyncFolder() - {Message}", ex.Message);
                restoreSyncDataResult.ErrorText = ex.Message;
            }

            return restoreSyncDataResult;
        }
    }
}
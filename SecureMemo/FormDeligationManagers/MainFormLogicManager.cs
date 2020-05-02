using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using Autofac;
using GeneralToolkitLib.Storage.Memory;
using SecureMemo.DataModels;
using SecureMemo.Services;

namespace SecureMemo.FormDeligationManagers
{
    public class MainFormLogicManager : ManagerBase
    {
        private readonly MemoStorageService _memoStorageService;
        private readonly FileStorageService _fileStorageService;
        private readonly PasswordStorage _passwordStorage;
        private TabPageDataCollection _tabPageDataCollection;
        private readonly AppSettingsService _appSettingsService;
        private readonly ILifetimeScope _scope;
        private object _lockObject = new object();
        private bool? _existingDatabase;

        public MainFormLogicManager(MemoStorageService memoStorageService, FileStorageService fileStorageService, PasswordStorage passwordStorage, ILifetimeScope scope, AppSettingsService appSettingsService)
        {
            _memoStorageService = memoStorageService;
            _fileStorageService = fileStorageService;
            _scope = scope;
            _appSettingsService = appSettingsService;
            _passwordStorage = passwordStorage;
            _tabPageDataCollection = TabPageDataCollection.CreateNewPageDataCollection(_appSettingsService.Settings.DefaultEmptyTabPages);
        }

        public int PageCount
        {
            get => _tabPageDataCollection.TabPageDictionary.Count;
        }

        public int ActivePageIndex
        {
            get => _tabPageDataCollection.ActiveTabIndex;
        }


        public bool HasExistingDatabase
        {
            get
            {
                _existingDatabase ??= _memoStorageService.DatabaseExists();
                return _existingDatabase.Value;
            }
        }



        [SecuritySafeCritical]
        public void CreateNewDatabase()
        {
            string password = _passwordStorage.Get("SecureMemo");
            _tabPageDataCollection = TabPageDataCollection.CreateNewPageDataCollection(_appSettingsService.Settings.DefaultEmptyTabPages);
            _memoStorageService.SaveTabPageCollection(_tabPageDataCollection, password);

        }

        public void CreateBackup()
        {
            _memoStorageService.MakeBackup();
        }

        public bool UpdateTabPageLabel(int index, string tabLabel)
        {
            if (index >= 0 && index < _tabPageDataCollection.TabPageDictionary.Count)
            {
                _tabPageDataCollection.TabPageDictionary[index].TabPageLabel = tabLabel;
                return true;
            }

            return false;
        }

        public void SetActivePageIndex(int pageIndex)
        {
            if (pageIndex < 0 || pageIndex >= _tabPageDataCollection.TabPageDictionary.Count)
            {
                throw new ArgumentException("PageIndex was out of range", nameof(pageIndex));
            }
            _tabPageDataCollection.ActiveTabIndex = pageIndex;
        }

        public string GetActiveTabText()
        {
            int pageIndex = _tabPageDataCollection.ActiveTabIndex;
            return _tabPageDataCollection.TabPageDictionary[pageIndex].TabPageText;
        }

        public void SetTabPageText(int pageIndex, string tabPageText)
        {
            _tabPageDataCollection.TabPageDictionary[pageIndex].TabPageText = tabPageText;

        }

        public void SaveNew()
        {

        }

        public void SaveDatabase()
        {
            string password = _passwordStorage.Get("SecureMemo");
            _memoStorageService.SaveTabPageCollection(_tabPageDataCollection, password);
        }

        [SecuritySafeCritical]
        public bool SaveToSharedFolder()
        {
            string password = _passwordStorage.Get("SharedFolderPassword");
            if (string.IsNullOrEmpty(password))
            {
                throw new InvalidOperationException("SaveToSharedFolder requires a password being set");
            }

            bool result = _memoStorageService.SaveTabPageCollectionToSharedFolder(_tabPageDataCollection, password);

            _passwordStorage.Set("SharedFolderPassword", null);
            return result;
        }

        public RestoreSyncDataResult RestoreBackupFromSyncFolder()
        {
            string password = _passwordStorage.Get("RestoreDatabaseFromSync");
            if (string.IsNullOrEmpty(password))
            {
                throw new InvalidOperationException("RestoreBackupFromSyncFolder requires a password being set");
            }

            var result = _memoStorageService.RestoreBackupFromSyncFolder(password);
            _passwordStorage.Set("RestoreDatabaseFromSync", null);

            return result;
        }

        public void ResetToDefaultDatabase()
        {
            _tabPageDataCollection.TabPageDictionary.Clear();
            _tabPageDataCollection.ActiveTabIndex = 0;
            _tabPageDataCollection = null;
            GC.Collect();
            _tabPageDataCollection = TabPageDataCollection.CreateNewPageDataCollection(_appSettingsService.Settings.DefaultEmptyTabPages);
        }

        public string GetTabPageText(int pageIndex)
        {
            if (pageIndex < 0 || pageIndex >= _tabPageDataCollection.TabPageDictionary.Count)
            {
                throw new ArgumentException($"Range exception when calling GetTabPageText. TabPageCount = {PageCount}", nameof(pageIndex));
            }

            return _tabPageDataCollection.TabPageDictionary[pageIndex].TabPageText;
        }

        public string GetTabPageLabel(int pageIndex)
        {
            if (pageIndex < 0 || pageIndex >= _tabPageDataCollection.TabPageDictionary.Count)
            {
                throw new ArgumentException($"Range exception when calling GetTabPageText. TabPageCount = {PageCount}", nameof(pageIndex));
            }

            return _tabPageDataCollection.TabPageDictionary[pageIndex].TabPageLabel;
        }

        public void AppendNewTabPage()
        {
            var page = new TabPageData();
            page.GenerateUniqueIdIfNoneExists();
            page.PageIndex = PageCount;
            page.TabPageLabel = $"Page {PageCount}";
            _tabPageDataCollection.TabPageDictionary.Add(page.PageIndex, page);
            _tabPageDataCollection.ActiveTabIndex = page.PageIndex;
        }

        public List<TabPageData> GetTabPageDataCollection()
        {
            return _tabPageDataCollection.TabPageDictionary.Values.ToList();
        }

        public void SetActiveTabPageText(string text)
        {
            _tabPageDataCollection.TabPageDictionary[_tabPageDataCollection.ActiveTabIndex].TabPageText = text;
        }

        public void SetTabPageLabel(int tabPageIndex, string tabPageLabel)
        {
            _tabPageDataCollection.TabPageDictionary[tabPageIndex].TabPageLabel = tabPageLabel;
        }

        public async Task<bool> RemoveTabPageAsync(int tabIndex)
        {
            if (!_tabPageDataCollection.TabPageDictionary.ContainsKey(tabIndex))
            {
                return false;
            }

            var tabToRemove = _tabPageDataCollection.TabPageDictionary[tabIndex];
            int removedPageIndex = tabToRemove.PageIndex;

            // Removing Item
            _tabPageDataCollection.TabPageDictionary.Remove(tabIndex);

            // Reindexing
            await ReindexTabPageColectionAfterRemovalAsync().ConfigureAwait(true);


            //_tabPageDataCollection.ActiveTabIndex should only be modified if the last page was removed.

            if (_tabPageDataCollection.ActiveTabIndex >= _tabPageDataCollection.TabPageDictionary.Count)
            {
                _tabPageDataCollection.ActiveTabIndex = _tabPageDataCollection.TabPageDictionary.Count - 1;
            }

            return true;

        }

        private async Task ReindexTabPageColectionAfterRemovalAsync()
        {
            TabPageData[] tabPageArray = null;
            lock (_lockObject)
            {
                tabPageArray = _tabPageDataCollection.TabPageDictionary.Values.OrderBy(x => x.PageIndex).ToArray();
                _tabPageDataCollection.TabPageDictionary.Clear();
            }

            if (tabPageArray.Length == 0)
            {
                return;
            }

            void Function()
            {
                const int lowIndex = 0;
                int maxIndex = _tabPageDataCollection.TabPageDictionary.Count - 1;


                Parallel.For(lowIndex, maxIndex, (i, state) => { tabPageArray[i].PageIndex = i; });
                tabPageArray.AsParallel().ForAll(data => { _tabPageDataCollection.TabPageDictionary.Add(data.PageIndex, data); });


                if (_tabPageDataCollection.ActiveTabIndex > 0 && _tabPageDataCollection.ActiveTabIndex > maxIndex)
                {
                    _tabPageDataCollection.ActiveTabIndex = maxIndex;
                }
            }

            await Task.Factory.StartNew(Function);

        }

        private void ReindexTabPageColectionAfterRemoval()
        {
            TabPageData[] tabPageArray = null;
            lock (_lockObject)
            {
                tabPageArray = _tabPageDataCollection.TabPageDictionary.Values.OrderBy(x => x.PageIndex).ToArray();
                _tabPageDataCollection.TabPageDictionary.Clear();
            }

            if (tabPageArray.Length == 0)
            {
                return;
            }


            const int lowIndex = 0;
            int maxIndex = _tabPageDataCollection.TabPageDictionary.Count - 1;

            for (int i = lowIndex; i < maxIndex; i++)
            {
                tabPageArray[i].PageIndex = i;
                var data = tabPageArray[i];
                _tabPageDataCollection.TabPageDictionary.Add(data.PageIndex, data);
            }

            if (_tabPageDataCollection.ActiveTabIndex > 0 && _tabPageDataCollection.ActiveTabIndex > maxIndex)
            {
                _tabPageDataCollection.ActiveTabIndex = maxIndex;
            }
        }

        [SecurityCritical]
        public bool OpenDatabase()
        {
            string password = _passwordStorage.Get("SecureMemo");
            var tabPageDataCollection = _memoStorageService.LoadTabPageCollection(password);
            if (tabPageDataCollection?.TabPageDictionary == null)
            {
                return false;
            }

            if (_memoStorageService.FoundDatabaseErrors)
            {
                ReindexTabPageColectionAfterRemoval();
                SaveDatabase();
                return true;
            }
            return true;
        }
    }
}
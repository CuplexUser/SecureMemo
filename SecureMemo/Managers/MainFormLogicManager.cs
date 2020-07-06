using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using GeneralToolkitLib.Storage.Memory;
using SecureMemo.DataModels;
using SecureMemo.Delegates;
using SecureMemo.EventHandlers;
using SecureMemo.Services;
using Serilog;

namespace SecureMemo.Managers
{
    /// <summary>
    /// </summary>
    /// <seealso cref="SecureMemo.Managers.ManagerBase" />
    public class MainFormLogicManager : ManagerBase
    {
        /// <summary>
        ///     The application settings service
        /// </summary>
        private readonly AppSettingsService _appSettingsService;

        /// <summary>
        ///     The file storage service
        /// </summary>
        private readonly FileStorageService _fileStorageService;

        /// <summary>
        ///     The memo storage service
        /// </summary>
        private readonly MemoStorageService _memoStorageService;

        /// <summary>
        ///     The password storage
        /// </summary>
        private readonly PasswordStorage _passwordStorage;

        /// <summary>
        ///     The scope
        /// </summary>
        private readonly ILifetimeScope _scope;

        /// <summary>
        ///     The existing database
        /// </summary>
        private bool? _existingDatabase;

        /// <summary>
        ///     The lock object
        /// </summary>
        private readonly object _lockObject = new object();


        /// <summary>
        ///     The tab page data collection
        /// </summary>
        private TabPageDataCollection _tabPageDataCollection;

        private bool PageDataChanged;

        private readonly CancellationToken reIndexCancellationToken = new CancellationToken(false);
        private bool TabPageStructureChanged;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MainFormLogicManager" /> class.
        /// </summary>
        /// <param name="memoStorageService">The memo storage service.</param>
        /// <param name="fileStorageService">The file storage service.</param>
        /// <param name="passwordStorage">The password storage.</param>
        /// <param name="scope">The scope.</param>
        /// <param name="appSettingsService">The application settings service.</param>
        public MainFormLogicManager(MemoStorageService memoStorageService, FileStorageService fileStorageService, PasswordStorage passwordStorage, ILifetimeScope scope, AppSettingsService appSettingsService)
        {
            _memoStorageService = memoStorageService;
            _fileStorageService = fileStorageService;
            _scope = scope;
            _appSettingsService = appSettingsService;
            _passwordStorage = passwordStorage;
            _tabPageDataCollection = TabPageDataCollection.CreateNewPageDataCollection(_appSettingsService.Settings.DefaultEmptyTabPages);
        }

        public bool IsModified
        {
            get => TabPageStructureChanged || PageDataChanged;
            private set
            {
                TabPageStructureChanged = value;
                PageDataChanged = value;
            }
        }

        /// <summary>
        ///     Gets the page count.
        /// </summary>
        /// <value>
        ///     The page count.
        /// </value>
        public int PageCount => _tabPageDataCollection.TabPageDictionary.Count;

        /// <summary>
        ///     Gets the index of the active page.
        /// </summary>
        /// <value>
        ///     The index of the active page.
        /// </value>
        public int ActivePageIndex => _tabPageDataCollection.ActiveTabIndex;

        //TabPageCollectionEventHandler (TabPageCollectionEventArgs eventArgs ) eaaaa;


        /// <summary>
        ///     Gets or sets a value indicating whether this instance has existing database.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this instance has existing database; otherwise, <c>false</c>.
        /// </value>
        public bool HasExistingDatabase
        {
            get
            {
                _existingDatabase ??= _memoStorageService.DatabaseExists();
                return _existingDatabase.Value;
            }
        }

        public event EventDeliagtes.TabPageCollectionChanged OnTabPageCollectionChange;

        public event EventDeliagtes.ActivatePageIndexChanged OnActivePageIndexChange;


        [SecuritySafeCritical]
        public void CreateNewDatabase()
        {
            string password = _passwordStorage.Get("SecureMemo");
            _tabPageDataCollection = TabPageDataCollection.CreateNewPageDataCollection(_appSettingsService.Settings.DefaultEmptyTabPages);
            _memoStorageService.SaveTabPageCollection(_tabPageDataCollection, password);
            OnTabPageCollectionChange?.Invoke(this, new TabPageCollectionEventArgs(TabPageCollectionStateChange.NewDatabaseCreated));
            PageDataChanged = true;
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
                OnTabPageCollectionChange?.Invoke(this, new TabPageCollectionEventArgs(TabPageCollectionStateChange.PageLabelChanged));
                return true;
            }

            return false;
        }

        public void SetActivePageIndex(int pageIndex)
        {
            if (pageIndex < 0 || pageIndex >= _tabPageDataCollection.TabPageDictionary.Count) throw new ArgumentException("PageIndex was out of range", nameof(pageIndex));

            OnActivePageIndexChange?.Invoke(this, new ActivatePageIndexChangedArgs(_tabPageDataCollection.ActiveTabIndex, pageIndex));
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
            PageDataChanged = true;
        }

        public void SaveNew()
        {
        }

        public void SaveDatabase()
        {
            string password = _passwordStorage.Get("SecureMemo");
            _memoStorageService.SaveTabPageCollection(_tabPageDataCollection, password);
            PageDataChanged = false;
        }

        [SecuritySafeCritical]
        public bool SaveToSharedFolder()
        {
            string password = _passwordStorage.Get("SharedFolderPassword");
            if (string.IsNullOrEmpty(password)) throw new InvalidOperationException("SaveToSharedFolder requires a password being set");

            bool result = _memoStorageService.SaveTabPageCollectionToSharedFolder(_tabPageDataCollection, password);

            _passwordStorage.Set("SharedFolderPassword", null);
            return result;
        }

        public RestoreSyncDataResult RestoreBackupFromSyncFolder()
        {
            string password = _passwordStorage.Get("RestoreDatabaseFromSync");
            if (string.IsNullOrEmpty(password)) throw new InvalidOperationException("RestoreBackupFromSyncFolder requires a password being set");

            var result = _memoStorageService.RestoreBackupFromSyncFolder(password);
            _passwordStorage.Set("RestoreDatabaseFromSync", null);
            PageDataChanged = false;

            return result;
        }

        public void ResetToDefaultDatabase()
        {
            _tabPageDataCollection.TabPageDictionary.Clear();
            _tabPageDataCollection.ActiveTabIndex = 0;
            _tabPageDataCollection = null;
            GC.Collect();
            _tabPageDataCollection = TabPageDataCollection.CreateNewPageDataCollection(_appSettingsService.Settings.DefaultEmptyTabPages);
            PageDataChanged = true;
        }

        /// <summary>
        ///     Gets the tab page text.
        /// </summary>
        /// <param name="pageIndex">Index of the page.</param>
        /// <returns></returns>
        public string GetTabPageText(int pageIndex)
        {
            if (pageIndex < 0 || pageIndex >= _tabPageDataCollection.TabPageDictionary.Count) throw new ArgumentException($"Range exception when calling GetTabPageText. TabPageCount = {PageCount}", nameof(pageIndex));

            return _tabPageDataCollection.TabPageDictionary[pageIndex].TabPageText;
        }

        /// <summary>
        ///     Gets the tab page label.
        /// </summary>
        /// <param name="pageIndex">Index of the page.</param>
        /// <returns></returns>
        public string GetTabPageLabel(int pageIndex)
        {
            if (pageIndex < 0 || pageIndex >= _tabPageDataCollection.TabPageDictionary.Count) throw new ArgumentException($"Range exception when calling GetTabPageText. TabPageCount = {PageCount}", nameof(pageIndex));

            if (!_tabPageDataCollection.TabPageDictionary.ContainsKey(pageIndex))
                throw new IndexOutOfRangeException($"An atempt to get PageIndex: {pageIndex} from the TabPageDataSource was made and an error occured because the TabPageDataSource was not properly indexed. " +
                                                   "Meaning a synchronization error occured in order to fasilitate this errorstate.");

            return _tabPageDataCollection.TabPageDictionary[pageIndex].TabPageLabel;
        }

        /// <summary>
        ///     Appends the new tab page.
        /// </summary>
        public void AppendNewTabPage()
        {
            var page = new TabPageData();
            page.GenerateUniqueIdIfNoneExists();
            page.PageIndex = PageCount;
            page.TabPageLabel = $"Page {PageCount}";
            _tabPageDataCollection.TabPageDictionary.Add(page.PageIndex, page);
            _tabPageDataCollection.ActiveTabIndex = page.PageIndex;
            TabPageStructureChanged = true;

            OnTabPageCollectionChange?.Invoke(this, new TabPageCollectionEventArgs(TabPageCollectionStateChange.PageAdded));
        }

        /// <summary>
        ///     Gets the tab page data collection.
        /// </summary>
        /// <returns></returns>
        public List<TabPageData> GetTabPageDataCollection()
        {
            return _tabPageDataCollection.TabPageDictionary.Values.ToList();
        }

        /// <summary>
        ///     Sets the active tab page text.
        /// </summary>
        /// <param name="text">The text.</param>
        public void SetActiveTabPageText(string text)
        {
            _tabPageDataCollection.TabPageDictionary[_tabPageDataCollection.ActiveTabIndex].TabPageText = text;
            PageDataChanged = true;
        }

        /// <summary>
        ///     Sets the tab page label.
        /// </summary>
        /// <param name="tabPageIndex">Index of the tab page.</param>
        /// <param name="tabPageLabel">The tab page label.</param>
        public void SetTabPageLabel(int tabPageIndex, string tabPageLabel)
        {
            _tabPageDataCollection.TabPageDictionary[tabPageIndex].TabPageLabel = tabPageLabel;
            PageDataChanged = true;
        }

        /// <summary>
        ///     Removes the tab page asynchronous.
        /// </summary>
        /// <param name="tabIndex">Index of the tab.</param>
        /// <returns></returns>
        public async Task<bool> RemoveTabPageAsync(int tabIndex)
        {
            if (!_tabPageDataCollection.TabPageDictionary.ContainsKey(tabIndex)) return false;

            var tabToRemove = _tabPageDataCollection.TabPageDictionary[tabIndex];
            int removedPageIndex = tabToRemove.PageIndex;

            // Removing Item
            _tabPageDataCollection.TabPageDictionary.Remove(removedPageIndex);

            // State
            TabPageStructureChanged = true;

            // Reindexing
            bool reindexState = await ReindexTabPageCollectionAfterRemovalAsync().ConfigureAwait(true);


            //_tabPageDataCollection.ActiveTabIndex should only be modified if the last page was removed.

            if (_tabPageDataCollection.ActiveTabIndex >= _tabPageDataCollection.TabPageDictionary.Count) _tabPageDataCollection.ActiveTabIndex = _tabPageDataCollection.TabPageDictionary.Count - 1;

            OnTabPageCollectionChange?.Invoke(this, new TabPageCollectionEventArgs(TabPageCollectionStateChange.PageRemoved));


            return reindexState;
        }

        /// <summary>
        ///     Re-indexes the tab page collection after removal asynchronous.
        /// </summary>
        /// <returns>
        ///     Return value is
        /// </returns>
        private async Task<bool> ReindexTabPageCollectionAfterRemovalAsync()
        {
            TabPageData[] tabPageArray = null;

            lock (_lockObject)
            {
                tabPageArray = _tabPageDataCollection.TabPageDictionary.Values.Distinct().OrderBy(x => x.PageIndex).ToArray();
                _tabPageDataCollection.TabPageDictionary.Clear();
            }

            if (tabPageArray.Length == 0) return false;

            bool concurrentState = Monitor.TryEnter(_tabPageDataCollection, TimeSpan.FromSeconds(5));
            if (!concurrentState) return false;

            concurrentState = false;
            Monitor.Enter(_tabPageDataCollection, ref concurrentState);
            if (!concurrentState || !Monitor.IsEntered(_tabPageDataCollection))
            {
                Monitor.Pulse(_tabPageDataCollection);
                return false;
            }

            Task<bool> reindexTask = null;

            try
            {
                reindexTask = Task.Factory.StartNew(() =>
                {
                    const int lowIndex = 0;
                    int maxIndex = _tabPageDataCollection.TabPageDictionary.Count - 1;

                    Parallel.For(lowIndex, maxIndex, (i, state) =>
                    {
                        tabPageArray[i].PageIndex = i;
                        _tabPageDataCollection.TabPageDictionary.Add(i, tabPageArray[i]);
                    });

                    if (_tabPageDataCollection.ActiveTabIndex > 0 && _tabPageDataCollection.ActiveTabIndex > maxIndex) _tabPageDataCollection.ActiveTabIndex = maxIndex;

                    // Validation
                    List<int> indexList = _tabPageDataCollection.TabPageDictionary.Keys.ToList();
                    if (indexList.Count == 0) return true;

                    int min = indexList.Min();
                    int max = indexList.Max();

                    return IsContinuousSeries(min, max, indexList);
                }, reIndexCancellationToken);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception thrown in ReindexTabPageCollectionAfterRemovalAsync");
            }
            finally
            {
                Monitor.Exit(_tabPageDataCollection);
            }

            Monitor.Pulse(_tabPageDataCollection);
            return reindexTask != null && await reindexTask;
        }

        /// <summary>
        ///     Determines whether [is continous series] [the specified minimum].
        /// </summary>
        /// <param name="min">The minimum.</param>
        /// <param name="max">The maximum.</param>
        /// <param name="noninteruptedSeries">The noninterupted series.</param>
        /// <returns>
        ///     <c>true</c> if [is continuous series] [the specified minimum]; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsContinuousSeries(int min, int max, IEnumerable<int> noninteruptedSeries)
        {
            IEnumerable<int> series = noninteruptedSeries as int[] ?? noninteruptedSeries.ToArray();
            if (series.Count() - 1 != max - min) return false;

            var query = from s in series
                group series by s
                into sg
                select sg.Count();

            IEnumerable<int> distinctValues = query as int[] ?? query.ToArray();
            if (distinctValues.Count() != series.Count()) return false;

            return !distinctValues.Where(s => s > 1).Select(x => x).Any();
        }

        /// <summary>
        ///     Re-indexes the tab page collection after removal.
        /// </summary>
        /// <returns></returns>
        private bool ReindexTabPageCollectionAfterRemoval()
        {
            TabPageData[] tabPageArray;
            lock (_lockObject)
            {
                tabPageArray = _tabPageDataCollection.TabPageDictionary.Values.OrderBy(x => x.PageIndex).ToArray();
                _tabPageDataCollection.TabPageDictionary.Clear();
            }

            if (tabPageArray.Length == 0) return false;

            TabPageStructureChanged = true;
            const int lowIndex = 0;
            int maxIndex = _tabPageDataCollection.TabPageDictionary.Count - 1;

            for (int i = lowIndex; i < maxIndex; i++)
            {
                tabPageArray[i].PageIndex = i;
                var data = tabPageArray[i];
                _tabPageDataCollection.TabPageDictionary.Add(data.PageIndex, data);
            }

            if (_tabPageDataCollection.ActiveTabIndex > 0 && _tabPageDataCollection.ActiveTabIndex > maxIndex) _tabPageDataCollection.ActiveTabIndex = maxIndex;

            return true;
        }

        /// <summary>
        ///     Opens the database.
        /// </summary>
        /// <returns></returns>
        [SecurityCritical]
        public bool OpenDatabase()
        {
            string password = _passwordStorage.Get("SecureMemo");
            lock (_lockObject)
            {
                var tabPageDataCollection = _memoStorageService.LoadTabPageCollection(password);
                if (tabPageDataCollection?.TabPageDictionary == null) return false;
            }

            if (!_memoStorageService.FoundDatabaseErrors) return true;

            SaveDatabase();
            IsModified = false;

            return true;
        }
    }
}
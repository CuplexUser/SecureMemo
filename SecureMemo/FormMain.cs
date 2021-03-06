﻿using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Windows.Forms;
using Autofac;
using GeneralToolkitLib.Converters;
using GeneralToolkitLib.Encryption.License;
using GeneralToolkitLib.Encryption.License.StaticData;
using GeneralToolkitLib.Storage.Memory;
using SecureMemo.DataModels;
using SecureMemo.Delegates;
using SecureMemo.EventHandlers;
using SecureMemo.Forms;
using SecureMemo.InputForms;
using SecureMemo.Managers;
using SecureMemo.Properties;
using SecureMemo.Services;
using SecureMemo.TextSearchModels;
using SecureMemo.UserControls;
using SecureMemo.Utility;
using Serilog;

//using SecureMemo.EventHandlers.TabPageCollectionStateChange;

namespace SecureMemo
{
    public partial class FormMain : Form
    {
        private const string PwdKey = "SecureMemo";
        private const string LicenseFilename = "licence.txt";
        private readonly ApplicationState _applicationState;
        private readonly AppSettingsService _appSettingsService;
        private readonly LicenseService _licenseService;
        private readonly MainFormLogicManager _logicManager;
        private readonly PasswordStorage _passwordStorage;
        private readonly ILifetimeScope _scope;
        private TabDropData _dropData;
        private FormFind _formFind;
        private bool _isResizingWindow;
        private int _tabPageClickIndex = -1;
        private TabSearchEngine _tabSearchEngine;


        public FormMain(AppSettingsService appSettingsService, ILifetimeScope scope, MainFormLogicManager logicManager, PasswordStorage passwordStorage)
        {
            if (DesignMode)
                return;

            _appSettingsService = appSettingsService;

            _scope = scope;
            _logicManager = logicManager;
            _passwordStorage = passwordStorage;

            _applicationState = new ApplicationState();

            _licenseService = LicenseService.Instance;
            InitializeComponent();

            logicManager.OnTabPageCollectionChange += LogicManager_OnTabPageCollectionChange;
            logicManager.OnActivePageIndexChange += LogicManager_OnActivePageIndexChange;
        }

        protected bool IsDataModelChanged => _applicationState.TabIndexChanged || _applicationState.TabTextDataChanged || _applicationState.TabPageAddOrRemove;

        private void LogicManager_OnTabPageCollectionChange(object sender, TabPageCollectionEventArgs eventArgs)
        {
            const TabPageCollectionStateChange state = TabPageCollectionStateChange.NewDatabaseCreated | TabPageCollectionStateChange.PageShiftedPosition | TabPageCollectionStateChange.PageAdded | TabPageCollectionStateChange.PageRemoved;

            if ((eventArgs.ActiveChange | state) > 0)
            {
                Invoke(new EventDeliagtes.InvokeUiThreadUpdate(InitializeTabControls));
                Invoke(new EventDeliagtes.InvokeUiThreadUpdate(UpdateApplicationState));
            }
        }

        private void LogicManager_OnActivePageIndexChange(object sender, ActivatePageIndexChangedArgs args)
        {
            _tabPageClickIndex = args.CurrentIndex;
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            try
            {
                InitializeTabControls();
                _appSettingsService.LoadSettings();
                InitFormSettings();
                LoadLicenseFile();
                _licenseService.Init(SerialNumbersSettings.ProtectedApp.SecureMemo);

                Text = ConfigHelper.AssemblyTitle + " - v" + Assembly.GetExecutingAssembly().GetName().Version;
                UpdateApplicationState();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, Resources.FormMain__Error_loading_application_settings, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void frmMain_Resize(object sender, EventArgs e)
        {
            if (!_isResizingWindow)
            {
                _appSettingsService.Settings.MainWindowWith = ClientRectangle.Width;
                _appSettingsService.Settings.MainWindowHeight = ClientRectangle.Height;
            }
        }

        private void tabPageControl_TabTextDataChanged(object sender, EventArgs e)
        {
            if (_applicationState.Initializing)
                return;

            _logicManager.UpdateTabPageLabel(tabControlNotepad.SelectedIndex, GetTextInTabControl(tabControlNotepad.SelectedIndex));
            _applicationState.TabTextDataChanged = true;
            UpdateApplicationState();
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if ((e.CloseReason == CloseReason.UserClosing || e.CloseReason == CloseReason.FormOwnerClosing) && !PromptExit())
            {
                e.Cancel = true;
                return;
            }

            _appSettingsService.SaveSettings();
        }

        private void tabControlNotepad_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_applicationState.Initializing)
                return;

            _applicationState.TabIndexChanged = true;
            _logicManager.SetActivePageIndex(tabControlNotepad.SelectedIndex);

            UpdateApplicationState();
        }

        private void UpdateTabControls()
        {
            //if (_applicationState.TabPageAddOrRemove)
            //{
            //    if (pageRemoved)
            //    {
            //        using (var uiTabEnum = tabControlNotepad.TabPages.Cast<TabPage>().GetEnumerator())
            //        {
            //            while (uiTabEnum.MoveNext())
            //            {
            //                if (uiTabEnum.Current != null)
            //                {
            //                    int tabIndex = uiTabEnum.Current.TabIndex;
            //                    if (!_tabPageDataCollection.TabPageDictionary.ContainsKey(tabIndex))
            //                    {
            //                        var tabPageToRemove = tabControlNotepad.TabPages[tabIndex];
            //                        tabControlNotepad.TabPages.Remove(tabPageToRemove);

            //                        foreach (Control control in tabPageToRemove.Controls)
            //                        {
            //                            control.Dispose();
            //                        }

            //                        tabPageToRemove.Dispose();

            //                        break;
            //                    }
            //                }
            //            }
            //        }
            //    }
            //    else if (pageAdded)
            //    {
            //        int tabPageIndexAdded = _tabPageDataCollection.TabPageDictionary.Max(x => x.Key);


            //        TabPageData tabPageData = _tabPageDataCollection.TabPageDictionary[tabPageIndexAdded];
            //        var tabPageControl = new MemoTabPageControl("MemoTabPageControl", tabPageIndexAdded) { Dock = DockStyle.Fill };
            //        var tabPage = new TabPage(tabPageData.TabPageLabel);
            //        tabPageControl.TabTextDataChanged += tabPageControl_TabTextDataChanged;

            //        tabPage.Controls.Add(tabPageControl);

            //        if (ControlHelper.GetChildControlByName(tabPageControl, tabPageControl.TabPageControlTextboxName) is RichTextBox richTextBox)
            //        {
            //            SecureMemoFontSettings fontSettings = _appSettingsService.Settings.FontSettings;
            //            richTextBox.Font = new Font(fontSettings.FontFamily, fontSettings.FontSize, fontSettings.Style);
            //            richTextBox.Text = tabPageData.TabPageText;
            //            richTextBox.ContextMenuStrip = contextMenuTextArea;
            //            richTextBox.SelectionChanged += RichTextBox_SelectionChanged;
            //        }

            //        tabControlNotepad.TabPages.Add(tabPage);

            //    }

            //    _applicationState.TabPageAddOrRemove = false;
            //    _applicationState.Initializing = false;
            //}
        }

        [SecurityCritical]
        private void SaveToSharedFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!_appSettingsService.Settings.UseSharedSyncFolder)
            {
                MessageBox.Show(Resources.FormMain_saveToSharedFolderMenuDisabled, Resources.FormMain__Could_not_save, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(_appSettingsService.Settings.SyncFolderPath) || !Directory.Exists(_appSettingsService.Settings.SyncFolderPath))
            {
                MessageBox.Show(Resources.FormMain_saveToSharedFoldrMenuInvalidPath, Resources.FormMain__Could_not_save, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }


            var formSetPassword = new FormSetPassword();
            if (formSetPassword.ShowDialog(this) != DialogResult.OK)
            {
                formSetPassword.Dispose();
                return;
            }


            string password = formSetPassword.VerifiedPassword;
            _passwordStorage.Set("SharedFolderPassword", password);
            formSetPassword.Dispose();

            bool result = _logicManager.SaveToSharedFolder();

            if (result)
                MessageBox.Show("Database successfully saved to sync folder", Resources.FormMain__Database_Saved, MessageBoxButtons.OK, MessageBoxIcon.Information);
            else
                MessageBox.Show("Failed to save database to sync folder path", Resources.FormMain__Could_not_save, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        [SecurityCritical]
        private void RestoreDatabaseFromSyncPathMenuItem_Click(object sender, EventArgs e)
        {
            if (_applicationState.DatabaseExists &&
                MessageBox.Show("Are you sure that you want to replace the existing database?\nIt is recommended that you perform a backup before replacing the existing database!",
                    "Replace database from sync folder",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            var formGetPassword = new FormGetPassword {UsePasswordValidation = false};
            if (formGetPassword.ShowDialog(this) != DialogResult.OK)
            {
                formGetPassword.Dispose();
                return;
            }


            string password = formGetPassword.PasswordString;
            _passwordStorage.Set("RestoreDatabaseFromSync", password);
            formGetPassword.Dispose();

            _applicationState.DatabaseLoaded = false;
            _appSettingsService.LoadSettings();
            InitFormSettings();

            RestoreSyncDataResult result = _logicManager.RestoreBackupFromSyncFolder();
            if (result.Successful)
            {
                MessageBox.Show("Database and app settings restored from sync folder.", "Restore complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _applicationState.DatabaseExists = true;
                UpdateApplicationState();
            }
            else
            {
                MessageBox.Show("Could not restore sync folder content: " + result.ErrorText, "Error restoring data", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CheckFormUpdatesMenuItem_Click(object sender, EventArgs e)
        {
            var applicationUpdate = new ApplicationUpdate();
            applicationUpdate.ShowDialog(this);
        }

        private void tabControlNotepad_DoubleClick(object sender, EventArgs e)
        {
            if (sender is TabControl tabControl)
            {
            }
        }

        private void tabControlNotepad_DragDrop(object sender, DragEventArgs e)
        {
            var tabDropData = e.Data.GetData(typeof(TabDropData)) as TabDropData;

            if (tabDropData == null)
                return;

            InitializeTabControls();
        }

        private void tabControlNotepad_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(TabDropData)))
                e.Effect = e.AllowedEffect;
        }

        private void tabControlNotepad_DragOver(object sender, DragEventArgs e)
        {
            var p = new Point(e.X, e.Y);
            p = tabControlNotepad.PointToClient(p);

            if (!(e.Data.GetData(typeof(TabDropData)) is TabDropData tabDropData))
                return;

            int sourceIndex = tabDropData.SourceIndex;


            // TODO Update GUI tabs to give the impression that the dragged over TabPages are changing.
            //for (int i = 0; i < tabControlNotepad.TabCount; i++)
            //{
            //    if (sourceIndex == i)
            //        continue;

            //    Rectangle tabRectangle = tabControlNotepad.GetTabRect(i);
            //    if (!tabRectangle.Contains(p)) continue;
            //    SwapTabs(sourceIndex, i);
            //    tabDropData.SourceIndex = i;
            //    e.Data.SetData(typeof(TabDropData), tabDropData);
            //    break;
            //}
        }

        private void tabControlNotepad_MouseDown(object sender, MouseEventArgs e)
        {
            if (tabControlNotepad.SelectedIndex < 0)
                return;

            int i = tabControlNotepad.SelectedIndex;
            _dropData = new TabDropData {SourceIndex = i, DoingDragDrop = false, InitialPosition = e.Location};
        }

        private void tabControlNotepad_MouseUp(object sender, MouseEventArgs e)
        {
            _dropData = null;
        }

        private void tabControlNotepad_MouseMove(object sender, MouseEventArgs e)
        {
            if (_dropData == null || e.Button != MouseButtons.Left) return;
            if (_dropData.DoingDragDrop || !_dropData.CheckValidHorizontalDistance(e.X)) return;
            _dropData.DoingDragDrop = true;
            DoDragDrop(_dropData, DragDropEffects.Move);
        }

        private void tabControlNotepad_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;
            var p = new Point(e.X, e.Y);
            _tabPageClickIndex = -1;

            for (int i = 0; i < tabControlNotepad.TabCount; i++)
            {
                Rectangle tabRectangle = tabControlNotepad.GetTabRect(i);
                if (!tabRectangle.Contains(p)) continue;
                _tabPageClickIndex = i;
                break;
            }

            contextMenuEditTabPage.Show(tabControlNotepad, e.Location);
        }

        private void RenameTabToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_tabPageClickIndex < 0 || _tabPageClickIndex >= tabControlNotepad.TabCount)
            {
                Log.Warning("Rename tab page could not find selected index: {_tabPageClickIndex}", _tabPageClickIndex);
                return;
            }


            int tabPageIndex = _tabPageClickIndex;

            var renameTabControl = new RenameTabPageControl {TabPageName = _logicManager.GetTabPageLabel(tabPageIndex)};
            var renameTabForm = FormFactory.CreateFormFromUserControl(renameTabControl);

            if (renameTabForm.ShowDialog(this) == DialogResult.OK)
            {
                string tabPageName = renameTabControl.TabPageName;
                _logicManager.SetTabPageLabel(tabPageIndex, tabPageName);
                tabControlNotepad.TabPages[tabPageIndex].Text = tabPageName;
                _applicationState.TabTextDataChanged = true;
            }

            renameTabControl.Dispose();
            renameTabForm.Dispose();
        }

        private async void deleteTabToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_tabPageClickIndex < 0 || _tabPageClickIndex >= tabControlNotepad.TabCount)
            {
                Log.Warning("Delete tab page could not find selected index: {_tabPageClickIndex}", _tabPageClickIndex);
                return;
            }

            if (_logicManager.PageCount == 1)
            {
                MessageBox.Show(this, "You must have atleast one tab page active", "Could not delete tab page", MessageBoxButtons.OK);
                return;
            }


            int tabIndex = tabControlNotepad.SelectedIndex;


            if (MessageBox.Show(this, $"Are you sure you want to delete this tab page with label: '{_logicManager.GetTabPageLabel(tabIndex)}' ?", "Confirm delete", MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                bool result = await _logicManager.RemoveTabPageAsync(tabIndex).ConfigureAwait(true);

                if (result)
                {
                    _applicationState.TabPageAddOrRemove = true;
                    UpdateTabControls();
                }
            }
        }

        private void closeCurrentDbMenuItem_Click(object sender, EventArgs e)
        {
            if (_applicationState.DatabaseLoaded && _applicationState.DatabaseExists)
            {
                if (IsDataModelChanged)
                {
                    if (MessageBox.Show("Are you sure you want to save and close the current open database?", "SaveDatabase and close", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK) return;

                    _logicManager.SaveDatabase();
                }

                if (CloseActiveDatabase())
                {
                    GC.Collect();
                    return;
                }
            }

            Log.Warning("CLose db was called without any loaded database.");
            MessageBox.Show("No database was loaded or found.", "Failed to close database", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        //private void SwapTabs(int sourceIndex, int destinationIndex)
        //{
        //    TabPageData tabData = _tabPageDataCollection.TabPageDictionary[sourceIndex];
        //    _tabPageDataCollection.TabPageDictionary[sourceIndex] = _tabPageDataCollection.TabPageDictionary[destinationIndex];
        //    _tabPageDataCollection.TabPageDictionary[destinationIndex] = tabData;

        //    _tabPageDataCollection.TabPageDictionary[sourceIndex].PageIndex = sourceIndex;
        //    _tabPageDataCollection.TabPageDictionary[destinationIndex].PageIndex = destinationIndex;

        //    _tabPageDataCollection.ActiveTabIndex = destinationIndex;
        //}

        private class ApplicationState
        {
            public bool DatabaseLoaded { get; set; }
            public bool DatabaseExists { get; set; }
            public bool TabTextDataChanged { get; set; }
            public bool TabIndexChanged { get; set; }
            public bool TabPageAddOrRemove { get; set; }
            public bool UniqueIdMissingFromExistingTabPage { get; set; }
            public bool Initializing { get; set; }
            public bool FontSettingsChanged { get; set; }
        }

        private class TabDropData
        {
            private const int MinDistance = 10;
            public int SourceIndex { get; set; }
            public Point InitialPosition { get; set; }
            public bool DoingDragDrop { get; set; }

            public bool CheckValidHorizontalDistance(int x)
            {
                return x < InitialPosition.X - MinDistance || x > InitialPosition.X + MinDistance;
            }
        }

        #region Private Methods

        private bool CloseActiveDatabase()
        {
            if (_applicationState.DatabaseLoaded && _applicationState.DatabaseExists)
            {
                _logicManager.ResetToDefaultDatabase();
                _applicationState.DatabaseLoaded = false;
                _applicationState.TabIndexChanged = false;
                _applicationState.TabPageAddOrRemove = false;
                _applicationState.TabTextDataChanged = false;
                InitializeTabControls();
                UpdateApplicationState();

                return true;
            }

            return false;
        }

        private void InitializeTabControls()
        {
            _applicationState.Initializing = true;
            if (!_logicManager.HasExistingDatabase)
            {
                _applicationState.DatabaseLoaded = false;
                _applicationState.DatabaseExists = false;
            }
            else
            {
                _applicationState.DatabaseExists = true;
            }

            // Dispose current Tab controls
            foreach (TabPage tabPage in tabControlNotepad.TabPages)
            {
                foreach (Control tabPageControl in tabPage.Controls) tabPageControl.Dispose();
                tabPage.Dispose();
            }

            tabControlNotepad.TabPages.Clear();


            for (int index = 0; index < _logicManager.PageCount; index++)
            {
                var tabPageControl = new MemoTabPageControl("MemoTabPageControl", index) {Dock = DockStyle.Fill};
                var tabPage = new TabPage(_logicManager.GetTabPageLabel(index));
                tabPageControl.TabTextDataChanged += tabPageControl_TabTextDataChanged;

                tabPage.Controls.Add(tabPageControl);

                if (ControlHelper.GetChildControlByName(tabPageControl, tabPageControl.TabPageControlTextboxName) is RichTextBox richTextBox)
                {
                    SecureMemoFontSettings fontSettings = _appSettingsService.Settings.FontSettings;
                    richTextBox.Font = new Font(fontSettings.FontFamily, fontSettings.FontSize, fontSettings.Style);
                    richTextBox.Text = _logicManager.GetTabPageText(index);
                    richTextBox.ContextMenuStrip = contextMenuTextArea;
                    richTextBox.SelectionChanged += RichTextBox_SelectionChanged;
                }

                tabControlNotepad.TabPages.Add(tabPage);
            }

            tabControlNotepad.SelectedIndex = _logicManager.ActivePageIndex;
            _applicationState.Initializing = false;
            _applicationState.FontSettingsChanged = false;
            UpdateApplicationState();
        }

        private void RichTextBox_SelectionChanged(object sender, EventArgs e)
        {
            var richTextBox = (RichTextBox) sender;

            if (richTextBox == null) return;
            if (_tabSearchEngine != null && !_tabSearchEngine.SelectionSetByCode)
                _tabSearchEngine.ResetSearchState(_logicManager.ActivePageIndex, richTextBox.SelectionStart, richTextBox.SelectionLength);
        }


        private string GetTextInTabControl(int tabIndex)
        {
            TabPage tabPage = tabControlNotepad.TabPages[tabIndex];
            if (tabPage.Controls[0] is MemoTabPageControl memoTabPageControl)
            {
                var richTextBox = ControlHelper.GetChildControlByName(memoTabPageControl, memoTabPageControl.TabPageControlTextboxName) as RichTextBox;

                return richTextBox?.Text;
            }

            return null;
        }

        private void UpdateApplicationState()
        {
            openDatabaseToolStripMenuItem.Enabled = _applicationState.DatabaseExists;
            saveToSharedFolderToolStripMenuItem.Enabled = _applicationState.DatabaseLoaded;

            if (_applicationState.DatabaseLoaded)
            {
                saveToolStripMenuItem.Enabled = IsDataModelChanged;
                closeCurrentDbMenuItem.Enabled = _applicationState.DatabaseExists && _applicationState.DatabaseLoaded;

                if (_applicationState.UniqueIdMissingFromExistingTabPage)
                {
                    _logicManager.SaveDatabase();
                    ;
                    _applicationState.UniqueIdMissingFromExistingTabPage = false;
                }
            }
            else
            {
                closeCurrentDbMenuItem.Enabled = false;
                saveToolStripMenuItem.Enabled = false;
            }


            tabsToolStripMenuItem.Enabled = _applicationState.DatabaseLoaded;
            changePasswordToolStripMenuItem.Enabled = _applicationState.DatabaseLoaded;

            BackupDatabasetoolStripMenuItem.Enabled = _applicationState.DatabaseExists;
            RestoreDatabasetoolStripMenuItem.Enabled = _applicationState.DatabaseExists;

            if (TopMost != _appSettingsService.Settings.AlwaysOnTop)
                TopMost = _appSettingsService.Settings.AlwaysOnTop;

            RestoreDatabaseFromSyncPathMenuItem.Enabled = _appSettingsService.Settings.UseSharedSyncFolder && !string.IsNullOrWhiteSpace(_appSettingsService.Settings.SyncFolderPath);
        }

        private void InitFormSettings()
        {
            _isResizingWindow = true;
            var screenSize = Screen.PrimaryScreen.Bounds;
            TopMost = _appSettingsService.Settings.AlwaysOnTop;
            Width = GetSafeParameter(_appSettingsService.Settings.MainWindowWith, MinimumSize.Width, screenSize.Width);
            Height = GetSafeParameter(_appSettingsService.Settings.MainWindowHeight, MinimumSize.Height, screenSize.Height);

            // Center form
            Left = screenSize.Width / 2 - Width / 2;
            Top = screenSize.Height / 2 - Height / 2;
            _isResizingWindow = false;
        }

        private int GetSafeParameter(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;

            return value;
        }

        private bool PromptExit()
        {
            if (!_applicationState.DatabaseLoaded || !_applicationState.TabTextDataChanged) return true;
            return MessageBox.Show(this, "Are you sure you want to exit without saving?", "Exit without save?", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK;
        }

        private void LoadLicenseFile()
        {
            try
            {
                if (File.Exists(LicenseFilename))
                    _licenseService.LoadLicenseFromFile(LicenseFilename);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, Resources.FormMain__ErrorText);
            }
        }

        private Point GetCenterLocationForChildForm(Control childForm)
        {
            int x = Width / 2 - childForm.Width / 2 + Left;
            int y = Height / 2 - childForm.Height / 2 + Top;
            Rectangle activeScreenArea = Screen.GetBounds(new Point(x, y));

            if (y + childForm.Height > activeScreenArea.Bottom)
                y = Top - childForm.Height - 5;

            if (x + childForm.Width > activeScreenArea.Right)
                x = activeScreenArea.Right - childForm.Width - 5;

            return new Point(x, y);
        }

        #endregion

        #region Menu stip methods

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!PromptExit()) return;
            _appSettingsService.SaveSettings();
            Application.Exit();
        }

        private void createNewDatabaseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (
                MessageBox.Show(this, "Do you want to create a new Memo database? Doing so will overwrite any existing stored database under this account.", "Create new database?",
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Question) !=
                DialogResult.OK)
                return;

            var frmSetPassword = new FormSetPassword {Text = "Choose password"};
            if (frmSetPassword.ShowDialog(this) != DialogResult.OK)
                return;

            string password = frmSetPassword.VerifiedPassword;

            if (string.IsNullOrEmpty(password))
            {
                MessageBox.Show(this, "Password can not be empty!", Resources.FormMain__ErrorText, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _passwordStorage.Set(PwdKey, password);
            _logicManager.CreateNewDatabase();


            _appSettingsService.Settings.PasswordDerivedString =
                GeneralConverters.GeneratePasswordDerivedString(_appSettingsService.Settings.ApplicationSaltValue + password + _appSettingsService.Settings.ApplicationSaltValue);

            _appSettingsService.SaveSettings();
            InitializeTabControls();
            _applicationState.DatabaseLoaded = true;
            _applicationState.DatabaseExists = true;
            UpdateApplicationState();
        }

        [SecurityCritical]
        private void openDatabaseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var formGetPassword = new FormGetPassword())

            {
                formGetPassword.PasswordSalt = _appSettingsService.Settings.ApplicationSaltValue;
                formGetPassword.PasswordDerivedString = _appSettingsService.Settings.PasswordDerivedString;


                if (formGetPassword.ShowDialog(this) != DialogResult.OK)
                    return;

                string password = formGetPassword.PasswordString;
                _passwordStorage.Set(PwdKey, password);
            }


            try
            {
                bool result = _logicManager.OpenDatabase();
                //tabPageCollection = _memoStorageService.LoadTabPageCollection(password);

                if (_logicManager.HasExistingDatabase)
                {
                    _logicManager.SaveDatabase();
                    Log.Warning("FoundDatabaseErrors Saving new recreated database");
                }

                // Make sure that every tabPageData has a unique Id
                //bool uniqueIdCreated = tabPageCollection.TabPageDictionary.Values.Aggregate(false, (current, tabPageData) => current | tabPageData.GenerateUniqueIdIfNoneExists());

                //if (uniqueIdCreated)
                //    _applicationState.UniqueIdMissingFromExistingTabPage = true;

                //if (tabPageCollection.TabPageDictionary.Count == 0)
                //    tabPageCollection = TabPageDataCollection.CreateNewPageDataCollection(_appSettingsService.Settings.DefaultEmptyTabPages);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Unable to load database, please verify that you entered the correct password. " + ex.Message, "Failed to load database", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            InitializeTabControls();
            _applicationState.DatabaseLoaded = true;
            _applicationState.TabIndexChanged = false;
            _applicationState.TabPageAddOrRemove = false;
            _applicationState.TabTextDataChanged = false;
            UpdateApplicationState();
        }


        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _logicManager.SaveDatabase();
        }

        private void changePasswordToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var formSetPassword = new FormSetPassword {Text = "Choose a new password"};
            if (formSetPassword.ShowDialog(this) != DialogResult.OK)
            {
                formSetPassword.Dispose();
                return;
            }

            string password = formSetPassword.VerifiedPassword;
            formSetPassword.Dispose();
            _passwordStorage.Set(PwdKey, password);
            _logicManager.SaveDatabase();

            _appSettingsService.Settings.PasswordDerivedString = GeneralConverters.GeneratePasswordDerivedString(_appSettingsService.Settings.ApplicationSaltValue + password + _appSettingsService.Settings.ApplicationSaltValue);

            _appSettingsService.SaveSettings();
            UpdateApplicationState();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var aboutForm = new FormAbout();
            aboutForm.ShowDialog(this);
        }

        private void appendNewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _logicManager.AppendNewTabPage();
            _applicationState.TabPageAddOrRemove = true;
            UpdateTabControls();
        }

        private void tabWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var scope = _scope.BeginLifetimeScope())
            {
                var formTabEdit = scope.Resolve<FormTabEdit>();
                if (formTabEdit.ShowDialog(this) != DialogResult.OK) return;

                if (formTabEdit.TabDataChanged)
                    _applicationState.TabPageAddOrRemove = true;
            }


            InitializeTabControls();
            ;
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var frmSettings = _scope.Resolve<FormSettings>();
            if (frmSettings.ShowDialog(this) == DialogResult.OK)
                if (_appSettingsService.Settings.FontSettings.HasChangedSinceLoaded)
                {
                    _applicationState.FontSettingsChanged = true;

                    InitializeTabControls();
                    _appSettingsService.Settings.FontSettings.HasChangedSinceLoaded = false;
                    _appSettingsService.SaveSettings();
                }
        }

        private void BackupDatabaseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                _logicManager.CreateBackup();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, Resources.FormMain_Failed_to_backup_database, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            MessageBox.Show(this, Resources.FormMain_Backup_completed_successfully, Resources.FormMain_Failed_to_backup_database, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void RestoreDatabaseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var frmSelectBackup = _scope.Resolve<FormRestoreBackup>();
            frmSelectBackup.ShowDialog(this);
        }

        private void fileManagerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var frmFileManager = _scope.Resolve<FormFileManager>();
            frmFileManager.ShowDialog(this);
        }

        private void findToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Set Tab page object reference to the search Service
            if (_formFind != null && !_formFind.IsDisposed)
            {
                _formFind.Show();
                _formFind.Location = GetCenterLocationForChildForm(_formFind);
                _formFind.Activate();
                return;
            }

            _formFind = new FormFind();
            _formFind.OnSearch += _formFind_OnSearch;
            _formFind.OnFormClose += _formFind_OnFormClose;
            _formFind.Show(this);
            _formFind.Location = GetCenterLocationForChildForm(_formFind);
        }

        private void _formFind_OnFormClose(object sender, EventArgs e)
        {
            if (_formFind == null)
                return;

            _formFind.OnSearch -= _formFind_OnSearch;
            _formFind.OnFormClose -= _formFind_OnFormClose;

            _formFind.Dispose();
            _formFind = null;
            _tabSearchEngine = null;
        }

        private void _formFind_OnSearch(object sender, TextSearchEventArgs eventArgs)
        {
            _tabSearchEngine ??= _scope.Resolve<TabSearchEngine>();

            try
            {
                var searchProperties = new TextSearchProperties
                {
                    CaseSensitive = eventArgs.SearchProperties.CaseSensitive,
                    LoopSearch = eventArgs.SearchProperties.LoopSearch,
                    SearchAllTabs = eventArgs.SearchProperties.SearchAllTabs,
                    SearchDirection = eventArgs.SearchProperties.SearchDirection,
                    SearchText = eventArgs.SearchProperties.SearchText
                };

                TextSearchResult searchResult = _tabSearchEngine.GetTextSearchResult(searchProperties);

                if (searchResult.SearchTextFound)
                {
                    _tabSearchEngine.SelectionSetByCode = true;
                    if (searchResult.TabIndex != _logicManager.ActivePageIndex && searchProperties.SearchAllTabs)
                    {
                        _logicManager.SetActivePageIndex(searchResult.TabIndex);
                        tabControlNotepad.SelectedIndex = searchResult.TabIndex;
                    }

                    Focus();
                    RichTextBox textBox = GetRichTextBoxInActiveTab();
                    textBox.SelectionStart = searchResult.StartPos;
                    textBox.SelectionLength = searchResult.Length;
                    textBox.Focus();
                    _tabSearchEngine.SelectionSetByCode = false;
                }
                else
                {
                    MessageBox.Show(Resources.FormMain__Search_string_not_found, Resources.FormMain__Not_found, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    RichTextBox textBox = GetRichTextBoxInActiveTab();
                    _tabSearchEngine.ResetSearchState(tabControlNotepad.SelectedIndex, textBox.SelectionStart, textBox.SelectionLength);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unhandled exception when calling _formFind_OnSearch()");
                MessageBox.Show(ex.Message, Resources.FormMain__ErrorText, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RichTextBox richTextBox = GetRichTextBoxInActiveTab();

            if (richTextBox != null && richTextBox.CanUndo)
            {
                richTextBox.Undo();
                _logicManager.SetActiveTabPageText(richTextBox.Text);
            }
        }

        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RichTextBox richTextBox = GetRichTextBoxInActiveTab();

            if (!string.IsNullOrWhiteSpace(richTextBox?.SelectedText))
            {
                richTextBox.Cut();
                _logicManager.SetTabPageText(_logicManager.ActivePageIndex, richTextBox.Text);
            }
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RichTextBox richTextBox = GetRichTextBoxInActiveTab();
            if (!string.IsNullOrWhiteSpace(richTextBox?.SelectedText))
                richTextBox.Copy();
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!Clipboard.ContainsText())
                return;

            RichTextBox richTextBox = GetRichTextBoxInActiveTab();

            if (richTextBox == null) return;
            richTextBox.Paste();
            _logicManager.SetActiveTabPageText(richTextBox.Text);
        }

        private RichTextBox GetRichTextBoxInActiveTab()
        {
            RichTextBox richTextBox = tabControlNotepad.SelectedTab?.GetChildControlByType(typeof(RichTextBox)).FirstOrDefault() as RichTextBox;

            return richTextBox;
        }

        #endregion
    }
}
using System;
using System.Drawing;
using System.Windows.Forms;
using GeneralToolkitLib.Utility;
using SecureMemo.DataModels;
using SecureMemo.Services;

namespace SecureMemo
{
    public partial class FormSettings : Form, IDisposable
    {
        private readonly SecureMemoFontSettings _fontSettings;
        private readonly AppSettingsService _appSettingsService;
        private Font _selectedFont;
        private bool fontSettingsChanged;

        public FormSettings(AppSettingsService appSettingsService)
        {
            _fontSettings = appSettingsService.Settings.FontSettings;
            _appSettingsService = appSettingsService;
            InitializeComponent();
        }

        private void frmSettings_Load(object sender, EventArgs e)
        {
            _selectedFont = new Font(_fontSettings.FontFamily, _fontSettings.FontSize);
            LoadFontSettings(_selectedFont);
            LoadGeneralSettings();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            //_fontSettings.FontFamily = _selectedFont.FontFamily;
            if (fontSettingsChanged)
            {
                _fontSettings.FontFamilyName = _selectedFont.FontFamily.Name;
                _fontSettings.FontSize = _selectedFont.Size;
                _fontSettings.Style = _selectedFont.Style;
                _fontSettings.HasChangedSinceLoaded = true;
                _fontSettings.FontFamilyUpdated();
            }
            else
            {
                _fontSettings.HasChangedSinceLoaded = false;
            }

            _appSettingsService.Settings.FontSettings = _fontSettings;
            _appSettingsService.Settings.AlwaysOnTop = chkAlwaysOntop.Checked;
            _appSettingsService.Settings.DefaultEmptyTabPages = Convert.ToInt32(numericUpDownTabPages.Value);

            if (chkSyncDatabase.Checked && !FileSystem.IsValidDirectory(txtSyncDatabaseDirectory.Text))
            {
                MessageBox.Show("The selected directory is invalid", "Invalid directory", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _appSettingsService.Settings.UseSharedSyncFolder = chkSyncDatabase.Checked;
            if (_appSettingsService.Settings.UseSharedSyncFolder)
                _appSettingsService.Settings.SyncFolderPath = txtSyncDatabaseDirectory.Text;


            _appSettingsService.SaveSettings();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void btnShowFontDialog_Click(object sender, EventArgs e)
        {
            try
            {
                fontDialog1.Font = _selectedFont;
                if (fontDialog1.ShowDialog(this) == DialogResult.OK)
                {
                    _selectedFont = fontDialog1.Font;
                    LoadFontSettings(_selectedFont);
                    fontSettingsChanged = true;
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, exception.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadGeneralSettings()
        {
            chkAlwaysOntop.Checked = _appSettingsService.Settings.AlwaysOnTop;
            numericUpDownTabPages.Value = _appSettingsService.Settings.DefaultEmptyTabPages;
            chkSyncDatabase.Checked = _appSettingsService.Settings.UseSharedSyncFolder;
            txtSyncDatabaseDirectory.Text = _appSettingsService.Settings.SyncFolderPath;
        }

        private void LoadFontSettings(Font font)
        {
            var displayFont = new Font(font.FontFamily, 12);
            txtFontFamily.Font = displayFont;
            txtFontSize.Font = displayFont;
            txtFontStyle.Font = displayFont;

            txtFontFamily.Text = font.FontFamily.Name;
            txtFontSize.Text = font.Size.ToString();
            txtFontStyle.Text = font.Style.ToString("F");
        }

        private void chkSyncDatabase_CheckedChanged(object sender, EventArgs e)
        {
            UpdateBrowseEnableState();
        }

        private void btnBrowseFolder_Click(object sender, EventArgs e)
        {
            if (folderBrowseForSyncDirectory.ShowDialog(this) == DialogResult.OK)
                txtSyncDatabaseDirectory.Text = folderBrowseForSyncDirectory.SelectedPath;
        }

        private void UpdateBrowseEnableState()
        {
            btnBrowseFolder.Enabled = chkSyncDatabase.Checked;
        }

        public new void Dispose()
        {
            _selectedFont.Dispose();
            _selectedFont = null;
            base.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
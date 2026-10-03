using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Windows.Forms;
using SecureMemo.FileStorageModels;
using SecureMemo.Services;
using SecureMemo.Toolkit.Storage.Memory;
using SecureMemo.Utility;
using Serilog;

namespace SecureMemo
{
    public partial class FormFileManager : Form
    {
        private const string PwdKey = "SecureMemo";
        private const int TreeIndentWidth = 16;
        private readonly DecryptedFileCache _decryptedFileCache;
        private readonly FileStorageService _fileStorageService;
        private readonly PasswordStorage _passwordStorage;

        public FormFileManager(FileStorageService fileStorageService, PasswordStorage passwordStorage)
        {
            _fileStorageService = fileStorageService;
            _passwordStorage = passwordStorage;
            _decryptedFileCache = new DecryptedFileCache(DecryptedFileCache.DefaultRootPath);
            InitializeComponent();
            treeViewFolders.DrawMode = TreeViewDrawMode.OwnerDrawAll;
            treeViewFolders.DrawNode += treeViewFolders_DrawNode;
        }

        private StorageFileSystem FileSystem => _fileStorageService.FileSystem;

        private StorageDirectory SelectedDirectory => treeViewFolders.SelectedNode?.Tag is int directoryId ? FileSystem.GetDirectory(directoryId) : FileSystem.GetRootDirectory();

        private List<StorageFile> SelectedFiles => listViewFiles.SelectedItems.Cast<ListViewItem>().Select(item => FileSystem.GetFile((int) item.Tag)).Where(f => f != null).ToList();

        #region Loading and saving

        private void FormFileManager_Load(object sender, EventArgs e)
        {
            try
            {
                _fileStorageService.Load(_passwordStorage.Get(PwdKey));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to open the file storage");
                string message = ex is CryptographicException
                    ? "The stored files could not be decrypted with the password of the open database."
                    : "The stored files could not be opened. " + ex.Message;

                MessageBox.Show(this, message, "File Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
                BeginInvoke(new Action(Close));
                return;
            }

            LoadFolderTree(StorageFileSystem.RootDirectoryId);
        }

        private void FormFileManager_FormClosed(object sender, FormClosedEventArgs e)
        {
            // Copies still open in another application are removed the next time the app starts.
            _decryptedFileCache.Clear();
            _fileStorageService.Unload();
        }

        /// <summary>
        ///     Every change is saved straight away, so nothing is lost if the app is closed.
        /// </summary>
        private bool SaveChanges()
        {
            Cursor = Cursors.WaitCursor;
            try
            {
                _fileStorageService.Save(_passwordStorage.Get(PwdKey));
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save the file storage");
                MessageBox.Show(this, "Your changes could not be saved. " + ex.Message, "File Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        #endregion

        #region Folder tree

        private void LoadFolderTree(int selectedDirectoryId)
        {
            treeViewFolders.BeginUpdate();
            treeViewFolders.Nodes.Clear();

            StorageDirectory root = FileSystem.GetRootDirectory();
            TreeNode rootNode = CreateDirectoryNode(root);
            treeViewFolders.Nodes.Add(rootNode);
            treeViewFolders.ExpandAll();

            TreeNode[] selected = treeViewFolders.Nodes.Find(selectedDirectoryId.ToString(), true);
            treeViewFolders.SelectedNode = selected.Length > 0 ? selected[0] : rootNode;
            treeViewFolders.EndUpdate();

            LoadFileList();
        }

        private TreeNode CreateDirectoryNode(StorageDirectory directory)
        {
            var node = new TreeNode(directory.DirectoryName) {Name = directory.Id.ToString(), Tag = directory.Id};
            foreach (StorageDirectory child in FileSystem.GetDirectories(directory.Id))
                node.Nodes.Add(CreateDirectoryNode(child));

            return node;
        }

        private void treeViewFolders_DrawNode(object sender, DrawTreeNodeEventArgs e)
        {
            if (e.Bounds.Width == 0 || e.Bounds.Height == 0)
                return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rowRectangle = new Rectangle(e.Bounds.X, e.Bounds.Y, treeViewFolders.ClientSize.Width - 1, e.Bounds.Height);

            if ((e.State & TreeNodeStates.Selected) > 0)
            {
                using Brush borderBrushOuter = new SolidBrush(Color.FromArgb(125, 162, 206));
                using Brush borderBrushInner = new SolidBrush(Color.FromArgb(235, 244, 253));
                using Brush fillBrush = new SolidBrush(Color.FromArgb(217, 232, 252));

                g.FillRoundedRectangle(borderBrushOuter, rowRectangle.X, rowRectangle.Y, rowRectangle.Width, rowRectangle.Height, 2);
                g.FillRoundedRectangle(borderBrushInner, Deflate(rowRectangle, 1), 2);
                g.FillRoundedRectangle(fillBrush, Deflate(rowRectangle, 3), 2);
            }
            else
            {
                g.FillRectangle(SystemBrushes.Window, rowRectangle);
            }

            int x = rowRectangle.X + 5 + e.Node.Level * TreeIndentWidth;
            Image image = FileSystemIcons.Images[0];
            g.DrawImage(image, x, rowRectangle.Y + 3);

            using Brush textBrush = new SolidBrush(Color.FromArgb(5, 5, 5));
            g.DrawString(e.Node.Text, Font, textBrush, x + image.Width + 4, rowRectangle.Y + rowRectangle.Height / 4);
        }

        private static Rectangle Deflate(Rectangle r, int pixels)
        {
            r.X = r.X + pixels;
            r.Y = r.Y + pixels;
            r.Height = r.Height - pixels * 2;
            r.Width = r.Width - pixels * 2;

            return r;
        }

        private void treeViewFolders_AfterSelect(object sender, TreeViewEventArgs e)
        {
            treeViewFolders.Refresh();
            LoadFileList();
        }

        private void treeViewFolders_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            // Make the context menu act on the folder that was right-clicked.
            if (e.Button == MouseButtons.Right)
                treeViewFolders.SelectedNode = e.Node;
        }

        private void contextMenuStripFolders_Opening(object sender, CancelEventArgs e)
        {
            bool isSubFolder = SelectedDirectory.Id != StorageFileSystem.RootDirectoryId;
            renameToolStripMenuItem.Visible = isSubFolder;
            deleteToolStripMenuItem.Visible = isSubFolder;
        }

        private void newFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StorageDirectory parent = SelectedDirectory;
            var existingNames = new HashSet<string>(FileSystem.GetDirectories(parent.Id).Select(d => d.DirectoryName), StringComparer.OrdinalIgnoreCase);
            string name = "NewFolder";
            for (int i = 2; existingNames.Contains(name); i++)
                name = "NewFolder_" + i;

            int directoryId = FileSystem.CreateDirectory(parent, name);
            SaveChanges();
            LoadFolderTree(directoryId);
            treeViewFolders.SelectedNode?.BeginEdit();
        }

        private void renameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            treeViewFolders.SelectedNode?.BeginEdit();
        }

        private void treeViewFolders_BeforeLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            if (e.Node.Tag is int directoryId && directoryId == StorageFileSystem.RootDirectoryId)
                e.CancelEdit = true;
        }

        private void treeViewFolders_AfterLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            // Label is null when the edit was cancelled or the text was left unchanged.
            if (e.Label == null)
                return;

            if (!FileSystem.IsValidDirectoryName(e.Label))
            {
                MessageBox.Show(this, "Folder names may only contain letters, digits, '.', '_' and '-'.", "Invalid folder name", MessageBoxButtons.OK, MessageBoxIcon.Error);
                e.CancelEdit = true;
                return;
            }

            FileSystem.RenameDirectory((int) e.Node.Tag, e.Label);
            SaveChanges();
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StorageDirectory directory = SelectedDirectory;
            if (directory.Id == StorageFileSystem.RootDirectoryId)
                return;

            if (MessageBox.Show(this, $"Delete the folder '{directory.DirectoryName}' and everything in it?", "Delete folder", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK)
                return;

            FileSystem.DeleteDirectory(directory.Id);
            SaveChanges();
            LoadFolderTree(directory.ParentId);
        }

        #endregion

        #region Files

        private void LoadFileList()
        {
            listViewFiles.BeginUpdate();
            listViewFiles.Items.Clear();
            foreach (StorageFile file in FileSystem.GetFiles(SelectedDirectory.Id))
            {
                var item = new ListViewItem(file.FileName, 1) {Tag = file.Id};
                item.SubItems.Add(FormatFileSize(file.FileSize));
                item.SubItems.Add(file.CreateDate.ToString("yyyy-MM-dd HH:mm"));
                listViewFiles.Items.Add(item);
            }

            listViewFiles.EndUpdate();
        }

        private static string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return bytes + " B";
            if (bytes < 1024 * 1024) return (bytes / 1024.0).ToString("0.#") + " KB";
            return (bytes / (1024.0 * 1024.0)).ToString("0.#") + " MB";
        }

        private void contextMenuStripFiles_Opening(object sender, CancelEventArgs e)
        {
            openFileContextMenuItem.Enabled = listViewFiles.SelectedItems.Count == 1;
            exportFilesContextMenuItem.Enabled = listViewFiles.SelectedItems.Count > 0;
            deleteFilesContextMenuItem.Enabled = listViewFiles.SelectedItems.Count > 0;
        }

        private void fileToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            exportToolStripMenuItem.Enabled = listViewFiles.SelectedItems.Count > 0;
        }

        private void addFiles_Click(object sender, EventArgs e)
        {
            using var openFileDialog = new OpenFileDialog {Multiselect = true, Title = "Add files to " + SelectedDirectory.DirectoryName};
            if (openFileDialog.ShowDialog(this) == DialogResult.OK)
                AddFiles(openFileDialog.FileNames);
        }

        private void listViewFiles_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void listViewFiles_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.FileDrop) is string[] paths)
                AddFiles(paths);
        }

        private void AddFiles(IEnumerable<string> paths)
        {
            StorageDirectory directory = SelectedDirectory;
            var skipped = new List<string>();
            int added = 0;

            Cursor = Cursors.WaitCursor;
            try
            {
                foreach (string path in paths)
                {
                    var fileInfo = new FileInfo(path);
                    if (!fileInfo.Exists)
                    {
                        skipped.Add(Path.GetFileName(path) + " (folders can't be added)");
                        continue;
                    }

                    if (fileInfo.Length > FileStorageService.MaxFileSize)
                    {
                        skipped.Add($"{fileInfo.Name} (larger than {FileStorageService.MaxFileSize / (1024 * 1024)} MB)");
                        continue;
                    }

                    try
                    {
                        FileSystem.AddFile(directory, fileInfo.Name, File.ReadAllBytes(fileInfo.FullName));
                        added++;
                    }
                    catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                    {
                        skipped.Add($"{fileInfo.Name} ({ex.Message})");
                    }
                }
            }
            finally
            {
                Cursor = Cursors.Default;
            }

            if (added > 0)
            {
                SaveChanges();
                LoadFileList();
            }

            if (skipped.Count > 0)
                MessageBox.Show(this, "These were not added:\n\n" + string.Join("\n", skipped), "Add files", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void listViewFiles_DoubleClick(object sender, EventArgs e)
        {
            OpenSelectedFile();
        }

        private void openFile_Click(object sender, EventArgs e)
        {
            OpenSelectedFile();
        }

        private void OpenSelectedFile()
        {
            if (listViewFiles.SelectedItems.Count != 1)
                return;

            StorageFile file = SelectedFiles.Single();
            try
            {
                string path = _decryptedFileCache.WriteFile(file.FileName, FileSystem.ReadFile(file.Id));
                Process.Start(new ProcessStartInfo(path) {UseShellExecute = true});
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to open stored file");
                MessageBox.Show(this, $"'{file.FileName}' could not be opened. {ex.Message}", "Open file", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void exportFiles_Click(object sender, EventArgs e)
        {
            List<StorageFile> files = SelectedFiles;
            if (files.Count == 0)
                return;

            try
            {
                if (files.Count == 1)
                    ExportSingleFile(files[0]);
                else
                    ExportFilesToFolder(files);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                MessageBox.Show(this, "Export failed. " + ex.Message, "Export", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportSingleFile(StorageFile file)
        {
            using var saveFileDialog = new SaveFileDialog {FileName = file.FileName, Title = "Export " + file.FileName};
            if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
                File.WriteAllBytes(saveFileDialog.FileName, FileSystem.ReadFile(file.Id));
        }

        private void ExportFilesToFolder(List<StorageFile> files)
        {
            using var folderBrowserDialog = new FolderBrowserDialog {Description = $"Export {files.Count} files to"};
            if (folderBrowserDialog.ShowDialog(this) != DialogResult.OK)
                return;

            string folder = folderBrowserDialog.SelectedPath;
            List<string> existing = files.Where(f => File.Exists(Path.Combine(folder, f.FileName))).Select(f => f.FileName).ToList();
            if (existing.Count > 0 &&
                MessageBox.Show(this, "These files already exist and will be replaced:\n\n" + string.Join("\n", existing), "Export", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK)
                return;

            foreach (StorageFile file in files)
                File.WriteAllBytes(Path.Combine(folder, file.FileName), FileSystem.ReadFile(file.Id));
        }

        private void deleteFiles_Click(object sender, EventArgs e)
        {
            DeleteSelectedFiles();
        }

        private void listViewFiles_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
                DeleteSelectedFiles();
            else if (e.KeyCode == Keys.Enter)
                OpenSelectedFile();
        }

        private void DeleteSelectedFiles()
        {
            List<StorageFile> files = SelectedFiles;
            if (files.Count == 0)
                return;

            string question = files.Count == 1 ? $"Delete '{files[0].FileName}'?" : $"Delete {files.Count} files?";
            if (MessageBox.Show(this, question, "Delete", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK)
                return;

            foreach (StorageFile file in files)
                FileSystem.DeleteFile(file.Id);

            SaveChanges();
            LoadFileList();
        }

        #endregion

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}

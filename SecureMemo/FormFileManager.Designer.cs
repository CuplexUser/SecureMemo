namespace SecureMemo
{
    partial class FormFileManager
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormFileManager));
            this.contextMenuStripFolders = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.newFolderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.renameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.deleteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.treeViewFolders = new System.Windows.Forms.TreeView();
            this.FileSystemIcons = new System.Windows.Forms.ImageList(this.components);
            this.listViewFiles = new System.Windows.Forms.ListView();
            this.columnHeaderName = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderSize = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderModified = new System.Windows.Forms.ColumnHeader();
            this.contextMenuStripFiles = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.addFilesContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openFileContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exportFilesContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.deleteFilesContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.menuStripFileManager = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addFilesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exportToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStripFolders.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.contextMenuStripFiles.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.menuStripFileManager.SuspendLayout();
            this.SuspendLayout();
            //
            // contextMenuStripFolders
            //
            this.contextMenuStripFolders.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newFolderToolStripMenuItem,
            this.renameToolStripMenuItem,
            this.deleteToolStripMenuItem});
            this.contextMenuStripFolders.Name = "contextMenuStrip1";
            this.contextMenuStripFolders.Size = new System.Drawing.Size(135, 70);
            this.contextMenuStripFolders.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenuStripFolders_Opening);
            //
            // newFolderToolStripMenuItem
            //
            this.newFolderToolStripMenuItem.Name = "newFolderToolStripMenuItem";
            this.newFolderToolStripMenuItem.Size = new System.Drawing.Size(134, 22);
            this.newFolderToolStripMenuItem.Text = "New Folder";
            this.newFolderToolStripMenuItem.Click += new System.EventHandler(this.newFolderToolStripMenuItem_Click);
            //
            // renameToolStripMenuItem
            //
            this.renameToolStripMenuItem.Name = "renameToolStripMenuItem";
            this.renameToolStripMenuItem.Size = new System.Drawing.Size(134, 22);
            this.renameToolStripMenuItem.Text = "Rename";
            this.renameToolStripMenuItem.Click += new System.EventHandler(this.renameToolStripMenuItem_Click);
            //
            // deleteToolStripMenuItem
            //
            this.deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
            this.deleteToolStripMenuItem.Size = new System.Drawing.Size(134, 22);
            this.deleteToolStripMenuItem.Text = "Delete";
            this.deleteToolStripMenuItem.Click += new System.EventHandler(this.deleteToolStripMenuItem_Click);
            //
            // splitContainer1
            //
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(3, 16);
            this.splitContainer1.Name = "splitContainer1";
            //
            // splitContainer1.Panel1
            //
            this.splitContainer1.Panel1.Controls.Add(this.treeViewFolders);
            //
            // splitContainer1.Panel2
            //
            this.splitContainer1.Panel2.Controls.Add(this.listViewFiles);
            this.splitContainer1.Size = new System.Drawing.Size(568, 356);
            this.splitContainer1.SplitterDistance = 189;
            this.splitContainer1.TabIndex = 1;
            //
            // treeViewFolders
            //
            this.treeViewFolders.ContextMenuStrip = this.contextMenuStripFolders;
            this.treeViewFolders.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeViewFolders.DrawMode = System.Windows.Forms.TreeViewDrawMode.OwnerDrawAll;
            this.treeViewFolders.FullRowSelect = true;
            this.treeViewFolders.HideSelection = false;
            this.treeViewFolders.ImageIndex = 0;
            this.treeViewFolders.ImageList = this.FileSystemIcons;
            this.treeViewFolders.ItemHeight = 24;
            this.treeViewFolders.LabelEdit = true;
            this.treeViewFolders.LineColor = System.Drawing.Color.DimGray;
            this.treeViewFolders.Location = new System.Drawing.Point(0, 0);
            this.treeViewFolders.Name = "treeViewFolders";
            this.treeViewFolders.SelectedImageIndex = 0;
            this.treeViewFolders.ShowLines = false;
            this.treeViewFolders.ShowRootLines = false;
            this.treeViewFolders.Size = new System.Drawing.Size(189, 356);
            this.treeViewFolders.StateImageList = this.FileSystemIcons;
            this.treeViewFolders.TabIndex = 0;
            this.treeViewFolders.BeforeLabelEdit += new System.Windows.Forms.NodeLabelEditEventHandler(this.treeViewFolders_BeforeLabelEdit);
            this.treeViewFolders.AfterLabelEdit += new System.Windows.Forms.NodeLabelEditEventHandler(this.treeViewFolders_AfterLabelEdit);
            this.treeViewFolders.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeViewFolders_AfterSelect);
            this.treeViewFolders.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.treeViewFolders_NodeMouseClick);
            //
            // FileSystemIcons
            //
            this.FileSystemIcons.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("FileSystemIcons.ImageStream")));
            this.FileSystemIcons.TransparentColor = System.Drawing.Color.Transparent;
            this.FileSystemIcons.Images.SetKeyName(0, "Folder.ico");
            this.FileSystemIcons.Images.SetKeyName(1, "Archive.ico");
            //
            // listViewFiles
            //
            this.listViewFiles.AllowDrop = true;
            this.listViewFiles.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderName,
            this.columnHeaderSize,
            this.columnHeaderModified});
            this.listViewFiles.ContextMenuStrip = this.contextMenuStripFiles;
            this.listViewFiles.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listViewFiles.FullRowSelect = true;
            this.listViewFiles.HideSelection = false;
            this.listViewFiles.Location = new System.Drawing.Point(0, 0);
            this.listViewFiles.Name = "listViewFiles";
            this.listViewFiles.Size = new System.Drawing.Size(375, 356);
            this.listViewFiles.SmallImageList = this.FileSystemIcons;
            this.listViewFiles.TabIndex = 0;
            this.listViewFiles.UseCompatibleStateImageBehavior = false;
            this.listViewFiles.View = System.Windows.Forms.View.Details;
            this.listViewFiles.DragDrop += new System.Windows.Forms.DragEventHandler(this.listViewFiles_DragDrop);
            this.listViewFiles.DragEnter += new System.Windows.Forms.DragEventHandler(this.listViewFiles_DragEnter);
            this.listViewFiles.DoubleClick += new System.EventHandler(this.listViewFiles_DoubleClick);
            this.listViewFiles.KeyDown += new System.Windows.Forms.KeyEventHandler(this.listViewFiles_KeyDown);
            //
            // columnHeaderName
            //
            this.columnHeaderName.Text = "Name";
            this.columnHeaderName.Width = 190;
            //
            // columnHeaderSize
            //
            this.columnHeaderSize.Text = "Size";
            this.columnHeaderSize.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.columnHeaderSize.Width = 70;
            //
            // columnHeaderModified
            //
            this.columnHeaderModified.Text = "Added";
            this.columnHeaderModified.Width = 110;
            //
            // contextMenuStripFiles
            //
            this.contextMenuStripFiles.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addFilesContextMenuItem,
            this.openFileContextMenuItem,
            this.exportFilesContextMenuItem,
            this.deleteFilesContextMenuItem});
            this.contextMenuStripFiles.Name = "contextMenuStripFiles";
            this.contextMenuStripFiles.Size = new System.Drawing.Size(135, 92);
            this.contextMenuStripFiles.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenuStripFiles_Opening);
            //
            // addFilesContextMenuItem
            //
            this.addFilesContextMenuItem.Name = "addFilesContextMenuItem";
            this.addFilesContextMenuItem.Size = new System.Drawing.Size(134, 22);
            this.addFilesContextMenuItem.Text = "Add Files...";
            this.addFilesContextMenuItem.Click += new System.EventHandler(this.addFiles_Click);
            //
            // openFileContextMenuItem
            //
            this.openFileContextMenuItem.Name = "openFileContextMenuItem";
            this.openFileContextMenuItem.Size = new System.Drawing.Size(134, 22);
            this.openFileContextMenuItem.Text = "Open";
            this.openFileContextMenuItem.Click += new System.EventHandler(this.openFile_Click);
            //
            // exportFilesContextMenuItem
            //
            this.exportFilesContextMenuItem.Name = "exportFilesContextMenuItem";
            this.exportFilesContextMenuItem.Size = new System.Drawing.Size(134, 22);
            this.exportFilesContextMenuItem.Text = "Export...";
            this.exportFilesContextMenuItem.Click += new System.EventHandler(this.exportFiles_Click);
            //
            // deleteFilesContextMenuItem
            //
            this.deleteFilesContextMenuItem.Name = "deleteFilesContextMenuItem";
            this.deleteFilesContextMenuItem.Size = new System.Drawing.Size(134, 22);
            this.deleteFilesContextMenuItem.Text = "Delete";
            this.deleteFilesContextMenuItem.Click += new System.EventHandler(this.deleteFiles_Click);
            //
            // groupBox1
            //
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.splitContainer1);
            this.groupBox1.Location = new System.Drawing.Point(5, 32);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(574, 375);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Encrypted File System";
            //
            // menuStripFileManager
            //
            this.menuStripFileManager.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
            this.menuStripFileManager.Location = new System.Drawing.Point(5, 5);
            this.menuStripFileManager.Name = "menuStripFileManager";
            this.menuStripFileManager.Size = new System.Drawing.Size(574, 24);
            this.menuStripFileManager.TabIndex = 7;
            this.menuStripFileManager.Text = "menuStrip1";
            //
            // fileToolStripMenuItem
            //
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addFilesToolStripMenuItem,
            this.exportToolStripMenuItem,
            this.toolStripSeparator1,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            this.fileToolStripMenuItem.DropDownOpening += new System.EventHandler(this.fileToolStripMenuItem_DropDownOpening);
            //
            // addFilesToolStripMenuItem
            //
            this.addFilesToolStripMenuItem.Name = "addFilesToolStripMenuItem";
            this.addFilesToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.addFilesToolStripMenuItem.Text = "Add Files...";
            this.addFilesToolStripMenuItem.Click += new System.EventHandler(this.addFiles_Click);
            //
            // exportToolStripMenuItem
            //
            this.exportToolStripMenuItem.Name = "exportToolStripMenuItem";
            this.exportToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.exportToolStripMenuItem.Text = "Export Selected...";
            this.exportToolStripMenuItem.Click += new System.EventHandler(this.exportFiles_Click);
            //
            // toolStripSeparator1
            //
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(177, 6);
            //
            // exitToolStripMenuItem
            //
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.exitToolStripMenuItem.Text = "Close";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            //
            // FormFileManager
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(584, 412);
            this.Controls.Add(this.menuStripFileManager);
            this.Controls.Add(this.groupBox1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStripFileManager;
            this.MinimumSize = new System.Drawing.Size(400, 300);
            this.Name = "FormFileManager";
            this.Padding = new System.Windows.Forms.Padding(5);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "File Manager";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.FormFileManager_FormClosed);
            this.Load += new System.EventHandler(this.FormFileManager_Load);
            this.contextMenuStripFolders.ResumeLayout(false);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.contextMenuStripFiles.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.menuStripFileManager.ResumeLayout(false);
            this.menuStripFileManager.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ContextMenuStrip contextMenuStripFolders;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.TreeView treeViewFolders;
        private System.Windows.Forms.ListView listViewFiles;
        private System.Windows.Forms.ColumnHeader columnHeaderName;
        private System.Windows.Forms.ColumnHeader columnHeaderSize;
        private System.Windows.Forms.ColumnHeader columnHeaderModified;
        private System.Windows.Forms.ContextMenuStrip contextMenuStripFiles;
        private System.Windows.Forms.ToolStripMenuItem addFilesContextMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openFileContextMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exportFilesContextMenuItem;
        private System.Windows.Forms.ToolStripMenuItem deleteFilesContextMenuItem;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.MenuStrip menuStripFileManager;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem addFilesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exportToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem newFolderToolStripMenuItem;
        private System.Windows.Forms.ImageList FileSystemIcons;
        private System.Windows.Forms.ToolStripMenuItem renameToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem deleteToolStripMenuItem;

    }
}

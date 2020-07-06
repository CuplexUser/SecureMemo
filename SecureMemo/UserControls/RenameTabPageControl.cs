using System;
using System.Windows.Forms;

namespace SecureMemo.UserControls
{
    public partial class RenameTabPageControl : UserControl
    {
        public RenameTabPageControl()
        {
            InitializeComponent();
        }

        public string TabPageName { get; set; }

        private void RenameTabPageControl_Load(object sender, EventArgs e)
        {
            txtNewName.Text = TabPageName;
            txtNewName.Select();
            txtNewName.Focus();
            if (ParentForm != null)
            {
                ParentForm.AcceptButton = btnOk;
                ParentForm.CancelButton = btnCancel;
            }
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            CompleteTabPageRenaming();
        }

        private void CompleteTabPageRenaming()
        {
            //validate
            if (txtNewName.Text.Length == 0)
            {
                MessageBox.Show(this, "Tab page name can not be empty!", "Invalid Tab page name", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            TabPageName = txtNewName.Text;
            if (ParentForm != null)
            {
                ParentForm.DialogResult = DialogResult.OK;
                ParentForm.Close();
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (ParentForm != null)
            {
                ParentForm.DialogResult = DialogResult.Cancel;
                ParentForm.Close();
            }
        }

        private void txtNewName_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                CompleteTabPageRenaming();
            }
        }
    }
}
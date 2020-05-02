namespace SecureMemo.UserControls
{
    partial class RenameTabPageControl
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnCancel = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.txtNewName = new System.Windows.Forms.TextBox();
            this.btnOk = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(175, 82);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(94, 28);
            this.btnCancel.TabIndex = 0;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(2, 13);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(77, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Tab Page Title";
            // 
            // txtNewName
            // 
            this.txtNewName.Location = new System.Drawing.Point(82, 13);
            this.txtNewName.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.txtNewName.MaxLength = 30;
            this.txtNewName.Name = "txtNewName";
            this.txtNewName.Size = new System.Drawing.Size(187, 20);
            this.txtNewName.TabIndex = 2;
            this.txtNewName.KeyUp += new System.Windows.Forms.KeyEventHandler(this.txtNewName_KeyUp);
            // 
            // btnOk
            // 
            this.btnOk.Location = new System.Drawing.Point(76, 82);
            this.btnOk.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(94, 28);
            this.btnOk.TabIndex = 4;
            this.btnOk.Text = "Ok";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // RenameTabPageControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.txtNewName);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnCancel);
            this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Name = "RenameTabPageControl";
            this.Size = new System.Drawing.Size(281, 130);
            this.Load += new System.EventHandler(this.RenameTabPageControl_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtNewName;
        private System.Windows.Forms.Button btnOk;
    }
}

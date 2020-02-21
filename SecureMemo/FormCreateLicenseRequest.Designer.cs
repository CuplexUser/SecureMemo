namespace SecureMemo
{
    partial class FormCreateLicenseRequest
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormCreateLicenseRequest));
            this.createLicenseRequestControl1 = new GeneralToolkitLib.UserControls.CreateLicenseRequestControl();
            this.SuspendLayout();
            // 
            // createLicenseRequestControl1
            // 
            this.createLicenseRequestControl1.Location = new System.Drawing.Point(12, 12);
            this.createLicenseRequestControl1.Name = "createLicenseRequestControl1";
            this.createLicenseRequestControl1.Size = new System.Drawing.Size(473, 399);
            this.createLicenseRequestControl1.TabIndex = 0;
            // 
            // FormCreateLicenseRequest
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(480, 410);
            this.Controls.Add(this.createLicenseRequestControl1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FormCreateLicenseRequest";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Create License Request";
            this.TopMost = true;
            this.ResumeLayout(false);

        }

        #endregion

        private GeneralToolkitLib.UserControls.CreateLicenseRequestControl createLicenseRequestControl1;
    }
}
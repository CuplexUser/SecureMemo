using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using GeneralToolkitLib.Encryption.License;
using GeneralToolkitLib.Encryption.License.DataModels;
using GeneralToolkitLib.UserControls;


namespace SecureMemo
{
    public partial class FormAbout : Form
    {
        public FormAbout()
        {
            InitializeComponent();
        }



        #region Assembly Attribute Accessors

        public string AssemblyTitle
        {
            get
            {
                var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof (AssemblyTitleAttribute), false);
                if (attributes.Length > 0)
                {
                    var titleAttribute = (AssemblyTitleAttribute) attributes[0];
                    if (titleAttribute.Title != "")
                        return titleAttribute.Title;
                }
                return Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().CodeBase);
            }
        }

        public string AssemblyVersion
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version.ToString(); }
        }

        public string AssemblyDescription
        {
            get
            {
                var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof (AssemblyDescriptionAttribute), false);
                if (attributes.Length == 0)
                    return "";
                return ((AssemblyDescriptionAttribute) attributes[0]).Description;
            }
        }

        public string AssemblyProduct
        {
            get
            {
                var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof (AssemblyProductAttribute), false);
                if (attributes.Length == 0)
                    return "";
                return ((AssemblyProductAttribute) attributes[0]).Product;
            }
        }

        public string AssemblyCopyright
        {
            get
            {
                var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof (AssemblyCopyrightAttribute), false);
                if (attributes.Length == 0)
                    return "";
                return ((AssemblyCopyrightAttribute) attributes[0]).Copyright;
            }
        }

        public string AssemblyCompany
        {
            get
            {
                var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof (AssemblyCompanyAttribute), false);
                if (attributes.Length == 0)
                    return "";
                return ((AssemblyCompanyAttribute) attributes[0]).Company;
            }
        }

        #endregion

        private void FormAbout_Load(object sender, EventArgs e)
        {
            Text = $"About {AssemblyTitle}";
            labelProductName.Text = AssemblyProduct;
            labelVersion.Text = $"Version {AssemblyVersion}";
            labelCopyright.Text = AssemblyCopyright;
            labelCompanyName.Text = AssemblyCompany;
            textBoxDescription.Text = AssemblyDescription;

            var licenseData = new LicenseDataModel {RegistrationData = new RegistrationDataModel {ComputerId = SysInfoManager.GetComputerId()}};
            licenseInfoControl1.InitLicenseData(licenseData);
            licenseInfoControl1.CreateRequest = ShowLicenseRequestForm;
        }

        public void ShowLicenseRequestForm()
        {
            CreateLicenseRequestControl userControl = new CreateLicenseRequestControl();
            Form containerForm = new Form();
            containerForm.Controls.Add(userControl);
            containerForm.StartPosition = FormStartPosition.CenterScreen;
            containerForm.AutoSizeMode = AutoSizeMode.GrowOnly;
            containerForm.AutoSize = true;
            containerForm.FormBorderStyle = FormBorderStyle.FixedSingle;
            containerForm.Text = "Registration form";
            containerForm.ShowIcon = false;
            containerForm.ShowInTaskbar = false;
            containerForm.Update();
            containerForm.ShowDialog(this);
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
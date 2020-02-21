using System;
using System.IO;
using GeneralToolkitLib.Encryption.License;
using GeneralToolkitLib.Encryption.License.DataConverters;
using GeneralToolkitLib.Encryption.License.DataModels;
using GeneralToolkitLib.Encryption.License.StaticData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UnitTests.Properties;

namespace UnitTests
{
    [TestClass]
    public class LicenseTests
    {
        private const string LicenseReqFilename = "LicenseRequest.txt";
        private const string LicenseKeyFilename = "License.txt";
        private readonly LicenseService licenseService = LicenseService.Instance;

        [TestInitialize]
        public void InitClass()
        {
            licenseService.Init(SerialNumbersSettings.ProtectedApp.SecureMemo);
            
        }

        [TestMethod]
        public void TestGenerateLicenseRequest()
        {
            var registrationData = new RegistrationDataModel
            {
                FirstName = Resources.RegFirstName,
                LastName = Resources.RegLastName,
                Company = Resources.RegCompany,
                ComputerId = SysInfoManager.GetComputerId(),
                ValidTo=DateTime.Today.AddYears(2),
                VersionName= "Secure memo",
            };

            var registrationDataManager = RegistrationDataManager.Create(registrationData);
            string licenseRequestString = registrationDataManager.SerializeToString();

            // Save to file
            FileStream fs = null;
            try
            {
                fs = File.Create(LicenseReqFilename);
                StreamWriter sw = new StreamWriter(fs);
                sw.Write(licenseRequestString);
                sw.Flush();
                fs.Flush(true);
            }
            catch (Exception exception)
            {
                Assert.Fail(exception.Message);
            }
            finally
            {
                fs?.Close();
            }

            Assert.IsTrue(File.Exists(LicenseReqFilename), "License request file was not generated");
      
            //System.Diagnostics.Process.Start(LicenseReqFilename);
        }

        [TestMethod]
        public void TestValidateGeneratedRequest()
        {
            Assert.IsTrue(File.Exists(LicenseReqFilename), "No license request file was found. Please run 'TestGenerateLicenseRequest' first");
            RegistrationDataModel registrationData = null;

            FileStream fs = null;
            try
            {
                fs = File.OpenRead(LicenseReqFilename);
                StreamReader reader = new StreamReader(fs);
                string regRequestContext = reader.ReadToEnd();
                registrationData = ObjectSerializer.DeserializeRegistrationDataFromString(regRequestContext);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
            finally
            {
                fs?.Close();
            }

            Assert.IsNotNull(registrationData, "registrationData could not be reconstructed");

            //licenseService.LicenseData.RegistrationData = registrationData;
            var serialNumberManager = licenseService.GetSerialNumberManager();
            string registrationKey = serialNumberManager.GenerateRegistrationKey(registrationData);

            Console.WriteLine(registrationKey);
            Assert.IsNotNull(registrationKey, "registrationKey can not be null");

            fs = null;
            try
            {
                fs = File.Create(LicenseKeyFilename);
                StreamWriter sw = new StreamWriter(fs);
                sw.Write(registrationKey);
                sw.Flush();
                fs.Flush(true);
            }
            catch (Exception exception)
            {
                Assert.Fail(exception.Message);
            }
            finally
            {
                fs?.Close();
            }

            licenseService.LoadLicenseFromFile(LicenseKeyFilename);
            licenseService.ValidateLicense();
            Assert.IsTrue(licenseService.ValidLicense, "Valid license check failed");


        }

    }
}
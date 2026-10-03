using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SecureMemo.Toolkit.Converters;

namespace UnitTests.Toolkit
{
    [TestClass]
    public class GeneralConvertersTests
    {
        [TestMethod]
        [DataRow(@"C:\Users\me\ApplicationSettings.ini", "ApplicationSettings.ini")]
        [DataRow("ApplicationSettings.ini", "ApplicationSettings.ini")]
        [DataRow(@"C:\folder\", @"C:\folder\")]
        public void GetFileNameFromPath_ReturnsTheLastPathSegment(string path, string expected)
        {
            Assert.AreEqual(expected, GeneralConverters.GetFileNameFromPath(path));
        }

        [TestMethod]
        public void GetFileNameFromPath_NullOrEmpty_Throws()
        {
            Assert.ThrowsExactly<ArgumentException>(() => GeneralConverters.GetFileNameFromPath(null));
            Assert.ThrowsExactly<ArgumentException>(() => GeneralConverters.GetFileNameFromPath(""));
        }

        [TestMethod]
        public void ByteArrayToHexString_FormatsEachByteAsTwoUppercaseDigits()
        {
            Assert.AreEqual("000AFF10", GeneralConverters.ByteArrayToHexString(new byte[] {0x00, 0x0A, 0xFF, 0x10}));
        }

        [TestMethod]
        public void GeneratePasswordDerivedString_MatchesTheValueStoredByEarlierVersions()
        {
            // FormGetPassword compares against the value saved in ApplicationSettings.ini; if this
            // ever changes, every existing user is locked out of their database.
            Assert.AreEqual(
                "8BA9F51E37C3AA8D46148438ACF324FEC16D3D55C37E7414B29AECCA457C2BF5E92B707FF4B39E22209AFEB4016E2795EAA7918AB17A75924C5F21191D59A077",
                GeneralConverters.GeneratePasswordDerivedString("salt" + "Fixture-Passw0rd" + "salt"));
        }

        [TestMethod]
        public void GeneratePasswordDerivedString_DifferentPasswords_GiveDifferentValues()
        {
            Assert.AreNotEqual(GeneralConverters.GeneratePasswordDerivedString("Password1"), GeneralConverters.GeneratePasswordDerivedString("Password2"));
        }
    }
}

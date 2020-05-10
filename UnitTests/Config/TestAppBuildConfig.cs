using System;

namespace UnitTests.Config
{
    public class TestAppBuildConfig
    {
        public string UserDataPath { get; }
        public string TestDataOutputPath { get; }

        public TestAppBuildConfig()
        {
            TestDataOutputPath = AppDomain.CurrentDomain.BaseDirectory+ "..\\..\\..\\TestDataOutput";
            UserDataPath = AppDomain.CurrentDomain.BaseDirectory+ "..\\..\\..\\TestConfigurationSetup";
        }
    }
}
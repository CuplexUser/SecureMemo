using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using GeneralToolkitLib.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ReceivedExtensions;

namespace UnitTests.Config
{
    public class TestSystemInit
    {
        public static ILifetimeScope Scope { get; private set; }
        private static string TestDataPath = "";


        public static void SetupRuntimeEnvironment()
        {
            //var testConf = Nsub

            //NSubstitute.Substitute.For<TestAppBuildConfig>().Received(string )
            Scope = AutofacConfig.CreateContainer();

        }
    }
}

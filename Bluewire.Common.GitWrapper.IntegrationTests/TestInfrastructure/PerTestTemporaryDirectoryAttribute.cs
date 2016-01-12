using System;
using System.IO;
using Bluewire.Common.GitWrapper.IntegrationTests.TestInfrastructure;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

[assembly: PerTestTemporaryDirectory]

namespace Bluewire.Common.GitWrapper.IntegrationTests.TestInfrastructure
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly)]
    public class PerTestTemporaryDirectoryAttribute : Attribute, ITestAction
    {
        public void BeforeTest(ITest test)
        {
        }
        
        public void AfterTest(ITest test)
        {
            var temporaryPath = TemporaryDirectoryForTest.Get(test.Properties);
            if (temporaryPath == null) return;
            if (!Directory.Exists(temporaryPath)) return;
            if (TestContext.CurrentContext.Result.Outcome == ResultState.Success)
            {
                FileSystemHelpers.CleanDirectory(temporaryPath);
            }
        }

        public ActionTargets Targets => ActionTargets.Test;
    }
}
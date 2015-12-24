using System;
using Bluewire.Common.Git.IntegrationTests.TestInfrastructure;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

[assembly: CleanTemporaryDirectoryWhenComplete]

namespace Bluewire.Common.Git.IntegrationTests.TestInfrastructure
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class CleanTemporaryDirectoryWhenCompleteAttribute : Attribute, ITestAction
    {
        public void BeforeTest(ITest test)
        {
        }

        public void AfterTest(ITest test)
        {
            TemporaryDirectoryForTest.CleanTemporaryDirectoryForAssembly();
        }

        public ActionTargets Targets => ActionTargets.Suite;
    }
}
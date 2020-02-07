using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Bluewire.Common.GitWrapper.IntegrationTests
{
    [TestFixture]
    public class GitFinderTests
    {
        [Test]
        public void EmptyStringCandidatePath_ThrowsArgumentException()
        {
            Assert.That(async () => await new GitFinder().FromCandidatePaths(new [] { "" }), Throws.ArgumentException);
        }

        [Test]
        public void InvalidCandidatePath_ThrowsArgumentException()
        {
            Assert.That(async () => await new GitFinder().FromCandidatePaths(new [] { ":notvalid*" }), Throws.ArgumentException);
        }

        [TestCase(@"c:\temp;")]
        [TestCase(":*:")]
        public void ToleratesInvalidEntriesInPathEnvironmentValue(string invalidValues)
        {
            Assert.That(async () => await new GitFinder().FromEnvironment(invalidValues), Throws.Nothing);
        }
    }
}

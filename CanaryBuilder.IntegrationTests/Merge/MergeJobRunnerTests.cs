using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bluewire.Common.Git;
using Bluewire.Common.Git.IntegrationTests;
using Bluewire.Common.Git.Model;
using CanaryBuilder.Logging;
using CanaryBuilder.Merge;
using Moq;
using NUnit.Framework;

namespace CanaryBuilder.IntegrationTests.Merge
{
    [TestFixture]
    public class MergeJobRunnerTests
    {
        private GitSession session;
        private GitWorkingCopy workingCopy;
        private MergeJobRunner sut;

        [SetUp]
        public async Task SetUp()
        {
            session = await Default.GitSession();
            workingCopy = await session.Init(Default.TemporaryDirectory, "repository");
            await session.Commit(workingCopy, "Initial comit", CommitOptions.AllowEmptyCommit);

            sut = new MergeJobRunner(session.Git);
        }

        [Test]
        public async Task JobWithNoMerges_Yields_FinalBranchSameAsBase()
        {
            var jobDefinition = new MergeJobDefinition
            {
                Base = new Ref("master"),
                FinalBranch = new Ref("test-branch")
            };

            await sut.Run(workingCopy, jobDefinition, Mock.Of<IJobLogger>());

            Assert.That(await session.RefExists(workingCopy, jobDefinition.FinalBranch));
            Assert.That(await session.AreRefsEquivalent(workingCopy, jobDefinition.Base, jobDefinition.FinalBranch), Is.True);
        }
    }
}

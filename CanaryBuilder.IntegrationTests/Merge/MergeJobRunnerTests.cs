using System;
using System.Collections.Generic;
using System.IO;
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

        [Test]
        public async Task JobWithSuccessfulMerge_Yields_FinalBranchWithBaseAsFirstParent()
        {
            await session.CreateBranchAndCheckout(workingCopy, "input-branch");
            await session.Commit(workingCopy, "Branch comit", CommitOptions.AllowEmptyCommit);

            var jobDefinition = new MergeJobDefinition
            {
                Base = new Ref("master"),
                Merges = {
                    new MergeCandidate(new Ref("input-branch"))
                },
                FinalBranch = new Ref("test-branch")
            };

            await sut.Run(workingCopy, jobDefinition, Mock.Of<IJobLogger>());

            Assert.That(await session.RefExists(workingCopy, jobDefinition.FinalBranch));
            Assert.That(await session.AreRefsEquivalent(workingCopy, jobDefinition.Base, jobDefinition.FinalBranch.Parent()), Is.True);
        }

        [Test]
        public async Task UnsuccessfulMerge_IsSkipped()
        {
            await session.CreateBranchAndCheckout(workingCopy, "input-branch");
            await WriteFileAndCommit("conflict.txt", "Add file on input-branch");

            await session.Checkout(workingCopy, new Ref("master"));
            await WriteFileAndCommit("conflict.txt", "Add file on master");
            var initialMaster = await session.ResolveRef(workingCopy, Ref.Head);

            var jobDefinition = new MergeJobDefinition
            {
                Base = new Ref("master"),
                Merges = {
                    new MergeCandidate(new Ref("input-branch"))
                },
                FinalBranch = new Ref("test-branch")
            };

            await sut.Run(workingCopy, jobDefinition, Mock.Of<IJobLogger>());

            Assert.That(await session.RefExists(workingCopy, jobDefinition.FinalBranch));
            Assert.That(await session.AreRefsEquivalent(workingCopy, initialMaster, jobDefinition.FinalBranch), Is.True);
        }

        private async Task WriteFileAndCommit(string relativePath, string content)
        {
            File.WriteAllText(workingCopy.Path(relativePath), content);
            await session.AddFile(workingCopy, relativePath);
            await session.Commit(workingCopy, $"Add {relativePath}");
        }
    }
}

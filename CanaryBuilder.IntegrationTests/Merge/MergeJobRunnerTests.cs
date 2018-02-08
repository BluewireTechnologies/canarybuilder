using System;
using System.IO;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper;
using Bluewire.Common.GitWrapper.IntegrationTests;
using Bluewire.Common.GitWrapper.Model;
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
        public async Task FailsIfOutputBranchAlreadyExists()
        {
            await session.CreateBranch(workingCopy, "output-branch");
            var jobDefinition = new MergeJobDefinition
            {
                Base = new Ref("master"),
                FinalBranch = new Ref("output-branch")
            };

            Assert.ThrowsAsync<OutputRefAlreadyExistsException>(() => sut.Run(workingCopy, jobDefinition, Mock.Of<IJobLogger>()));
        }

        [Test]
        public async Task FailsIfOutputTagAlreadyExists()
        {
            await session.CreateTag(workingCopy, "output-tag", Ref.Head, "conflicting tag");
            var jobDefinition = new MergeJobDefinition
            {
                Base = new Ref("master"),
                FinalTag = new Ref("output-tag")
            };

            Assert.ThrowsAsync<OutputRefAlreadyExistsException>(() => sut.Run(workingCopy, jobDefinition, Mock.Of<IJobLogger>()));
        }

        [Test]
        public void FailsIfBaseVerifierReportsFailure()
        {
            var failingVerifier = Mock.Of<IWorkingCopyVerifier>(v =>
                v.Verify(workingCopy, It.IsAny<IJobLogger>()) == Task.FromException(new InvalidWorkingCopyStateException("Failed")));

            var jobDefinition = new MergeJobDefinition
            {
                Base = new Ref("master"),
                Verifier = failingVerifier,
                FinalTag = new Ref("output-tag")
            };

            Assert.ThrowsAsync<InvalidWorkingCopyStateException>(() => sut.Run(workingCopy, jobDefinition, Mock.Of<IJobLogger>()));
        }

        [Test]
        public async Task ContinuesIfBaseVerifierReportsSuccess()
        {
            var succeedingVerifier = Mock.Of<IWorkingCopyVerifier>(v =>
                v.Verify(workingCopy, It.IsAny<IJobLogger>()) == Task.CompletedTask);

            var jobDefinition = new MergeJobDefinition
            {
                Base = new Ref("master"),
                Verifier = succeedingVerifier,
                FinalTag = new Ref("output-tag")
            };

            await sut.Run(workingCopy, jobDefinition, Mock.Of<IJobLogger>());
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
        public async Task SuccessfulJobWithMerges_DoesNotMoveBase()
        {
            var initialMaster = await session.ResolveRef(workingCopy, Ref.Head);
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
            Assert.That(await session.AreRefsEquivalent(workingCopy, jobDefinition.Base, initialMaster), Is.True);
        }

        [Test]
        public async Task UnsuccessfulMerge_IsSkipped()
        {
            await session.CreateBranchAndCheckout(workingCopy, "input-branch");
            await WriteFileAndCommit("conflict.txt", "Add file on input-branch");

            await session.Checkout(workingCopy, new Ref("master"));
            await WriteFileAndCommit("conflict.txt", "Add file on master");

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
            Assert.That(await session.AreRefsEquivalent(workingCopy, new Ref("master"), jobDefinition.FinalBranch), Is.True);
        }


        [Test]
        public async Task SuccessfulMergeWithFailedVerification_IsSkipped()
        {
            var failingVerifier = Mock.Of<IWorkingCopyVerifier>(v =>
                v.Verify(workingCopy, It.IsAny<IJobLogger>()) == Task.FromException(new InvalidWorkingCopyStateException("Failed")));

            await session.CreateBranchAndCheckout(workingCopy, "input-branch");
            await session.Commit(workingCopy, "Branch comit", CommitOptions.AllowEmptyCommit);

            var jobDefinition = new MergeJobDefinition
            {
                Base = new Ref("master"),
                Merges = {
                    new MergeCandidate(new Ref("input-branch")) { Verifier = failingVerifier }
                },
                FinalBranch = new Ref("test-branch")
            };

            await sut.Run(workingCopy, jobDefinition, Mock.Of<IJobLogger>());

            Assert.That(await session.RefExists(workingCopy, jobDefinition.FinalBranch));
            Assert.That(await session.AreRefsEquivalent(workingCopy, new Ref("master"), jobDefinition.FinalBranch), Is.True);
        }

        [Test]
        public async Task SuccessfulMergeWithDirtyWorkingCopyAfterSuccessfulVerification_IsSkipped()
        {
            var failingVerifier = new Mock<IWorkingCopyVerifier>();
            failingVerifier.Setup(v => v.Verify(workingCopy, It.IsAny<IJobLogger>()))
                .Callback(() =>
                {
                    File.WriteAllText(workingCopy.Path("temp.txt"), "Leave working copy dirty.");
                })
                .Returns(Task.CompletedTask);

            await session.CreateBranchAndCheckout(workingCopy, "input-branch");
            await session.Commit(workingCopy, "Branch comit", CommitOptions.AllowEmptyCommit);

            var jobDefinition = new MergeJobDefinition
            {
                Base = new Ref("master"),
                Merges = {
                    new MergeCandidate(new Ref("input-branch")) { Verifier = failingVerifier.Object }
                },
                FinalBranch = new Ref("test-branch")
            };

            await sut.Run(workingCopy, jobDefinition, Mock.Of<IJobLogger>());

            Assert.That(await session.RefExists(workingCopy, jobDefinition.FinalBranch));
            Assert.That(await session.AreRefsEquivalent(workingCopy, new Ref("master"), jobDefinition.FinalBranch), Is.True);
        }

        [Test]
        public async Task MergeIsNotFastForward()
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

            Assert.That(await session.AreRefsEquivalent(workingCopy, jobDefinition.FinalBranch, new Ref("input-branch")), Is.False);
        }
        [Test]
        public async Task MergeOfNonexistentRef_IsSkipped()
        {
            var jobDefinition = new MergeJobDefinition
            {
                Base = new Ref("master"),
                Merges = {
                    new MergeCandidate(new Ref("does-not-exist"))
                },
                FinalBranch = new Ref("test-branch")
            };

            await sut.Run(workingCopy, jobDefinition, Mock.Of<IJobLogger>());

            Assert.That(await session.RefExists(workingCopy, jobDefinition.FinalBranch));
            Assert.That(await session.AreRefsEquivalent(workingCopy, new Ref("master"), jobDefinition.FinalBranch), Is.True);
        }

        [Test]
        public async Task JobWithSuccessfulMerge_DeletesTemporaryBranch()
        {
            await session.CreateBranchAndCheckout(workingCopy, "input-branch");
            await WriteFileAndCommit("new-file.txt", "Content");

            var jobDefinition = new MergeJobDefinition
            {
                Base = new Ref("master"),
                TemporaryBranch = new Ref("temp-branch"),
                Merges = {
                    new MergeCandidate(new Ref("input-branch"))
                },
                FinalBranch = new Ref("test-branch")
            };

            await sut.Run(workingCopy, jobDefinition, Mock.Of<IJobLogger>());

            Assert.That(await session.RefExists(workingCopy, jobDefinition.TemporaryBranch), Is.False);
        }

        [Test]
        public async Task JobWithFailedMerge_DeletesTemporaryBranch()
        {
            await session.CreateBranchAndCheckout(workingCopy, "input-branch");
            await WriteFileAndCommit("conflict.txt", "Add file on input-branch");

            await session.Checkout(workingCopy, new Ref("master"));
            await WriteFileAndCommit("conflict.txt", "Add file on master");

            var jobDefinition = new MergeJobDefinition
            {
                Base = new Ref("master"),
                TemporaryBranch = new Ref("temp-branch"),
                Merges = {
                    new MergeCandidate(new Ref("input-branch"))
                },
                FinalBranch = new Ref("test-branch")
            };

            await sut.Run(workingCopy, jobDefinition, Mock.Of<IJobLogger>());

            Assert.That(await session.RefExists(workingCopy, jobDefinition.TemporaryBranch), Is.False);
        }

        private async Task WriteFileAndCommit(string relativePath, string content)
        {
            File.WriteAllText(workingCopy.Path(relativePath), content);
            await session.AddFile(workingCopy, relativePath);
            await session.Commit(workingCopy, $"Add {relativePath}");
        }
    }
}

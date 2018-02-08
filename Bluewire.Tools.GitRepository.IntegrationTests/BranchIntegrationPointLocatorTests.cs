using System.Threading.Tasks;
using Bluewire.Common.GitWrapper;
using Bluewire.Common.GitWrapper.IntegrationTests;
using Bluewire.Common.GitWrapper.IntegrationTests.TestInfrastructure;
using Bluewire.Common.GitWrapper.Model;
using NUnit.Framework;

namespace Bluewire.Tools.GitRepository.IntegrationTests
{
    [TestFixture]
    public class BranchIntegrationPointLocatorTests
    {
        private GitSession session;
        private GitWorkingCopy workingCopy;
        private BranchIntegrationPointLocator sut;
        private RepoStructureBuilder builder;
        private Ref startTag;
        private Ref start;

        private static Ref MasterBranch => new Ref("master");

        [SetUp]
        public async Task SetUp()
        {
            session = await Default.GitSession();
            workingCopy = await session.Init(Default.TemporaryDirectory, "repository");
            await session.Commit(workingCopy, "Initial commit", CommitOptions.AllowEmptyCommit);

            sut = new BranchIntegrationPointLocator(session);
            builder = new RepoStructureBuilder(session, workingCopy);
            startTag = await session.CreateTag(workingCopy, "start", Ref.Head, "");
            start = await session.ResolveRef(workingCopy, startTag);
        }

        [Test]
        public async Task SubjectNotInAncestryOfEndCommitIsInvalid()
        {
            await builder.CreateBranchWithCommits(startTag, "branch", 1);
            await builder.AddCommitsToBranch("master", 1);

            Assert.ThrowsAsync<CommitNotInAncestryChainException>(async () => await sut.FindCommit(workingCopy, startTag, MasterBranch, new Ref("branch")));
        }

        [Test]
        public async Task SubjectSameAsEndCommitResolvesToIdentityOfEnd()
        {
            await builder.CreateBranchWithCommits(startTag, "branch", 1);
            await builder.AddCommitsToBranch("master", 1);

            var masterIdentity = await session.ResolveRef(workingCopy, MasterBranch);

            var resolved = await sut.FindCommit(workingCopy, startTag, MasterBranch, MasterBranch);

            Assert.That(resolved, Is.EqualTo(masterIdentity));
        }

        [Test]
        public async Task SubjectInAncestryOfStartCommitResolvesToIdentityOfStart()
        {
            await builder.AddCommitsToBranch("master", 5);

            var childOfStart = await session.ResolveRef(workingCopy, MasterBranch.Ancestor(2));

            var resolved = await sut.FindCommit(workingCopy, childOfStart, MasterBranch, startTag);

            Assert.That(resolved, Is.EqualTo(childOfStart));
        }

        [Test]
        public async Task SubjectOnFirstParentAncestryChainBetweenStartAndEndResolvesToOwnIdentity()
        {
            await builder.AddCommitsToBranch("master", 8);

            var subjectRef = MasterBranch.Ancestor(4);
            var subjectIdentity = await session.ResolveRef(workingCopy, subjectRef);

            var resolved = await sut.FindCommit(workingCopy, startTag, MasterBranch, subjectRef);

            Assert.That(resolved, Is.EqualTo(subjectIdentity));
        }

        [Test]
        public async Task SubjectOnMergedBranchResolvesToIdentityOfMergeCommit()
        {
            await builder.CreateBranchWithCommits(startTag, "branch", 4);
            await builder.AddCommitsToBranch("master", 3);

            await session.Merge(workingCopy, new Ref("branch"));
            var mergeCommit = await session.ResolveRef(workingCopy, MasterBranch);

            await builder.AddCommitsToBranch("master", 2);

            var subjectRef = new Ref("branch").Ancestor(2); // On branch, but not at the tip.

            var resolved = await sut.FindCommit(workingCopy, startTag, MasterBranch, subjectRef);

            Assert.That(resolved, Is.EqualTo(mergeCommit));
        }

        [Test, Description("Ancestry chains match as far as they both go, but first-parent chain has been exhausted: subject was merged immediately after start.")]
        public async Task SubjectOnBranchMergedImmediatelyAfterStartResolvesToIdentityOfMergeCommitImmediatelyAfterStart()
        {
            await builder.CreateBranchWithCommits(startTag, "branch", 4);
            await builder.AddCommitsToBranch("master", 2);
            var thisStart = await session.ResolveRef(workingCopy, MasterBranch);

            await session.Merge(workingCopy, new Ref("branch"));
            var mergeCommit = await session.ResolveRef(workingCopy, MasterBranch);

            await builder.AddCommitsToBranch("master", 2);

            var subjectRef = new Ref("branch").Ancestor(2); // On branch, but not at the tip.

            var resolved = await sut.FindCommit(workingCopy, thisStart, MasterBranch, subjectRef);

            Assert.That(resolved, Is.EqualTo(mergeCommit));
        }

        [Test, Description("Ancestry chains match as far as they both go, but subject chain has been exhausted and first-parent chain has not: subject is the tip of the merged branch.")]
        public async Task SubjectAtTipOfMergedBranchResolvesToIdentityOfMergeCommit()
        {
            await builder.CreateBranchWithCommits(startTag, "branch", 1);
            await builder.AddCommitsToBranch("master", 2);
            var thisStart = await session.ResolveRef(workingCopy, MasterBranch);

            await session.Merge(workingCopy, new Ref("branch"));
            var mergeCommit = await session.ResolveRef(workingCopy, MasterBranch);

            await builder.AddCommitsToBranch("master", 2);

            var resolved = await sut.FindCommit(workingCopy, thisStart, MasterBranch, new Ref("branch"));

            Assert.That(resolved, Is.EqualTo(mergeCommit));
        }

        [Test, Description("Ancestry chains match entirely, but we know that subject != start: subject is the tip of the merged branch and was merged immediately after start.")]
        public async Task SubjectAtTipOfBranchMergedImmediatelyAfterStartResolvesToIdentityOfMergeCommitImmediatelyAfterStart()
        {
            await builder.CreateBranchWithCommits(startTag, "branch", 4);
            await builder.AddCommitsToBranch("master", 2);
            var thisStart = await session.ResolveRef(workingCopy, MasterBranch);

            await session.Merge(workingCopy, new Ref("branch"));
            var mergeCommit = await session.ResolveRef(workingCopy, MasterBranch);

            await builder.AddCommitsToBranch("master", 2);

            var resolved = await sut.FindCommit(workingCopy, thisStart, MasterBranch, new Ref("branch"));

            Assert.That(resolved, Is.EqualTo(mergeCommit));
        }

        [Test]
        public async Task FindsEarliestCommonMergeInFirstParentAncestry()
        {
            await builder.CreateBranchWithCommits(startTag, "branch", 4);
            await builder.AddCommitsToBranch("master", 2);
            var thisStart = await session.ResolveRef(workingCopy, MasterBranch);

            await session.Merge(workingCopy, new Ref("branch"));
            var targetMergeCommit = await session.ResolveRef(workingCopy, MasterBranch);

            await builder.CreateBranchWithCommits(MasterBranch, "branch-2", 3);
            await builder.AddCommitsToBranch("master", 2);

            await session.Merge(workingCopy, new Ref("branch-2"));
            var newerMergeCommit = await session.ResolveRef(workingCopy, MasterBranch);

            await builder.AddCommitsToBranch("master", 2);

            var subjectRef = new Ref("branch").Ancestor(2); // On branch, but not at the tip.

            var resolved = await sut.FindCommit(workingCopy, thisStart, MasterBranch, subjectRef);

            Assert.That(resolved, Is.Not.EqualTo(newerMergeCommit));
            Assert.That(resolved, Is.EqualTo(targetMergeCommit));
        }
    }
}

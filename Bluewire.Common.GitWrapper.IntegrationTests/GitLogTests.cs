using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper.IntegrationTests.TestInfrastructure;
using Bluewire.Common.GitWrapper.Model;
using NUnit.Framework;

namespace Bluewire.Common.GitWrapper.IntegrationTests
{
    [TestFixture]
    public class GitLogTests
    {
        private GitSession session;
        private GitWorkingCopy workingCopy;
        private GitRepository repository;
        private RepoStructureBuilder builder;

        [SetUp]
        public async Task SetUp()
        {
            session = await Default.GitSession();
            workingCopy = await session.Init(Default.TemporaryDirectory, "repository");
            repository = workingCopy.GetDefaultRepository();

            builder = new RepoStructureBuilder(session, workingCopy);
        }

        [Test]
        public async Task CanParseLogMessages()
        {
            const string message = @"Test commit

Multiple line commit message
with whitespace lines too.
";
            await session.Commit(workingCopy, message, CommitOptions.AllowEmptyCommit);
            var commitHash = await session.ResolveRef(workingCopy, Ref.Head);

            var logEntries = await session.ReadLog(workingCopy, new LogOptions(), Ref.Head);

            var logEntry = logEntries.Single();

            Assert.That(logEntry.Ref, Is.EqualTo(commitHash));
            Assert.That(logEntry.Author, Is.Not.Null);
            Assert.That(logEntry.Date, Is.Not.Null);
            Assert.That(logEntry.Message, Is.EqualTo(message));
        }

        [Test]
        public async Task CanParseLogMessagesContainingCarriageReturns()
        {
            var message = $@"Test commit

Multiple line{'\x0d'} commit message
";
            await session.Commit(workingCopy, message, CommitOptions.AllowEmptyCommit);
            var commitHash = await session.ResolveRef(workingCopy, Ref.Head);

            var logEntries = await session.ReadLog(workingCopy, new LogOptions(), Ref.Head);

            var logEntry = logEntries.Single();

            Assert.That(logEntry.Ref, Is.EqualTo(commitHash));
            Assert.That(logEntry.Author, Is.Not.Null);
            Assert.That(logEntry.Date, Is.Not.Null);
            Assert.That(logEntry.Message, Is.EqualTo(message));
        }

        [Test]
        public async Task LogEntriesAreReturnedInInverseCommitOrder()
        {
            await session.Commit(workingCopy, "first commit", CommitOptions.AllowEmptyCommit);
            var firstCommitHash = await session.ResolveRef(workingCopy, Ref.Head);
            await session.Commit(workingCopy, "second commit", CommitOptions.AllowEmptyCommit);
            var secondCommitHash = await session.ResolveRef(workingCopy, Ref.Head);

            var logEntries = await session.ReadLog(workingCopy, new LogOptions(), Ref.Head);

            Assert.That(logEntries.Length, Is.EqualTo(2));

            Assert.That(logEntries[0].Ref, Is.EqualTo(secondCommitHash));
            Assert.That(logEntries[0].Message, Is.EqualTo("second commit\r\n"));

            Assert.That(logEntries[1].Ref, Is.EqualTo(firstCommitHash));
            Assert.That(logEntries[1].Message, Is.EqualTo("first commit\r\n"));
        }

        [Test]
        public async Task CanFilterLogEntriesByRegexAppliedToMessage()
        {
            await session.Commit(workingCopy, "first commit", CommitOptions.AllowEmptyCommit);
            var firstCommitHash = await session.ResolveRef(workingCopy, Ref.Head);
            await session.Commit(workingCopy, "second commit", CommitOptions.AllowEmptyCommit);

            var logEntries = await session.ReadLog(workingCopy, new LogOptions { MatchMessage = new Regex("^first") }, Ref.Head);

            var logEntry = logEntries.Single();

            Assert.That(logEntry.Ref, Is.EqualTo(firstCommitHash));
        }

        [Test]
        public async Task CanListLogEntriesForMultipleBranches()
        {
            await session.Commit(workingCopy, "initial commit", CommitOptions.AllowEmptyCommit);
            await builder.CreateBranchWithCommits(new Ref("master"), new Ref("test/branch1"), 4);
            await builder.CreateBranchWithCommits(new Ref("master"), new Ref("test/branch2"), 3);
            await builder.CreateBranchWithCommits(new Ref("master"), new Ref("test/branch3"), 5);
            await builder.CreateBranchWithCommits(new Ref("master"), new Ref("test/branch4"), 12);

            var logEntries = await session.ReadLog(workingCopy, new LogOptions(),
                new Ref("test/branch1"),
                new Ref("test/branch2"),
                new Ref("test/branch3"));

            Assert.That(logEntries.Length, Is.EqualTo(13));
        }
    }
}

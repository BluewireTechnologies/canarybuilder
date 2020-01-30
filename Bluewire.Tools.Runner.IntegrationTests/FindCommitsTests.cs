using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper;
using Bluewire.Common.GitWrapper.IntegrationTests;
using Bluewire.Common.GitWrapper.IntegrationTests.TestInfrastructure;
using Bluewire.Common.GitWrapper.Model;
using Bluewire.Tools.GitRepository;
using Bluewire.Tools.Runner.FindTickets;
using NUnit.Framework;

namespace Bluewire.Tools.Runner.IntegrationTests
{
    [TestFixture]
    public class FindTicketsTests
    {
        private GitSession session;
        private GitWorkingCopy workingCopy;
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

            builder = new RepoStructureBuilder(session, workingCopy);
            startTag = await session.CreateTag(workingCopy, "start", Ref.Head, "");
            start = await session.ResolveRef(workingCopy, startTag);
        }

        [Test]
        public async Task CanIdentifyTicketInBetaMaster()
        {
            await BeginNewVersion("20.01");
            await builder.AddCommitsToBranch(MasterBranch, 4);
            await session.Commit(workingCopy, "Refs E-40000", CommitOptions.AllowEmptyCommit);
            await builder.AddCommitsToBranch(MasterBranch, 4);

            var cloned = await session.Clone(workingCopy.GetDefaultRepository().GetLocationUri(), Default.TemporaryDirectory, "clone");

            var sut = new ToolRunner.Impl { ArgumentList = { "20.01.1" }, WorkingCopyOrRepo = cloned.Root };

            using (var writer = new StringWriter())
            {
                var exitCode = await sut.Run(writer);
                Assert.That(exitCode, Is.Zero);

                var lines = Regex.Split(writer.ToString(), @"\s+").Where(l => !String.IsNullOrWhiteSpace(l)).ToArray();
                Assert.That(lines, Is.EquivalentTo(new[] { "E-40000" }));
            }
        }

        [Test]
        public async Task CanIdentifyTicketInBetaBackport()
        {
            await BeginNewVersion("20.01");
            await builder.AddCommitsToBranch(MasterBranch, 4);
            await session.Commit(workingCopy, "Refs E-40000", CommitOptions.AllowEmptyCommit);

            var backportBranch = await session.CreateBranch(workingCopy, "backport/20.01");
            await builder.AddCommitsToBranch(backportBranch, 4);

            await BeginNewVersion("20.02");
            await builder.AddCommitsToBranch(MasterBranch, 4);

            var cloned = await session.Clone(workingCopy.GetDefaultRepository().GetLocationUri(), Default.TemporaryDirectory, "clone");

            var sut = new ToolRunner.Impl { ArgumentList = { "20.01.1" }, WorkingCopyOrRepo = cloned.Root };

            using (var writer = new StringWriter())
            {
                var exitCode = await sut.Run(writer);
                Assert.That(exitCode, Is.Zero);

                var lines = Regex.Split(writer.ToString(), @"\s+").Where(l => !String.IsNullOrWhiteSpace(l)).ToArray();
                Assert.That(lines, Is.EquivalentTo(new[] { "E-40000" }));
            }
        }

        [Test]
        public async Task CanIdentifyTicketInBetaBackportSinceBranch()
        {
            await BeginNewVersion("20.01");
            await builder.AddCommitsToBranch(MasterBranch, 4);

            var backportBranch = await session.CreateBranch(workingCopy, "backport/20.01");
            await builder.AddCommitsToBranch(backportBranch, 2);
            await session.Commit(workingCopy, "Refs E-40000", CommitOptions.AllowEmptyCommit);
            await builder.AddCommitsToBranch(backportBranch, 2);

            await BeginNewVersion("20.02");
            await builder.AddCommitsToBranch(MasterBranch, 4);

            var cloned = await session.Clone(workingCopy.GetDefaultRepository().GetLocationUri(), Default.TemporaryDirectory, "clone");

            var sut = new ToolRunner.Impl { ArgumentList = { "20.01.1" }, WorkingCopyOrRepo = cloned.Root };

            using (var writer = new StringWriter())
            {
                var exitCode = await sut.Run(writer);
                Assert.That(exitCode, Is.Zero);

                var lines = Regex.Split(writer.ToString(), @"\s+").Where(l => !String.IsNullOrWhiteSpace(l)).ToArray();
                Assert.That(lines, Is.EquivalentTo(new[] { "E-40000" }));
            }
        }

        private async Task BeginNewVersion(string majorMinor)
        {
            await session.Checkout(workingCopy, MasterBranch);
            File.WriteAllText(workingCopy.Path(".current-version"), majorMinor);
            await session.AddFile(workingCopy, ".current-version");
            await session.Commit(workingCopy, $"Initial commit of {majorMinor}", CommitOptions.AllowEmptyCommit);
            await session.CreateTag(workingCopy, majorMinor, Ref.Head, "");
        }
    }
}

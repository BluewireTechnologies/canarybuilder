using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper;
using Bluewire.Common.GitWrapper.Model;
using Bluewire.Conventions;
using Bluewire.Stash.Tool;
using McMaster.Extensions.CommandLineUtils;
using NUnit.Framework;

namespace Bluewire.Stash.IntegrationTests.Tool
{
    [TestFixture]
    public class ApplicationTests
    {
        [Test]
        public async Task CanCommit()
        {
            var sourceDirectory = Path.Combine(Default.TemporaryDirectory, "source");
            Directory.CreateDirectory(sourceDirectory);
            File.WriteAllText(Path.Combine(sourceDirectory, "file.txt"), "Test");

            var commitArgs = AddTestSandbox("commit", "teststash", sourceDirectory, "--version", "21.01.78-beta");
            await ExecuteAsync(commitArgs.ToArray());
        }

        [Test]
        public async Task CanCommitAndCheckout()
        {
            var sourceDirectory = Path.Combine(Default.TemporaryDirectory, "source");
            Directory.CreateDirectory(sourceDirectory);
            File.WriteAllText(Path.Combine(sourceDirectory, "file.txt"), "Test");

            var commitArgs = AddTestSandbox("commit", "teststash", sourceDirectory, "--version", "21.01.78-beta");
            await ExecuteAsync(commitArgs.ToArray());

            var destinationDirectory = Path.Combine(Default.TemporaryDirectory, "destination");

            var checkoutArgs = AddTestSandbox("checkout", "teststash", destinationDirectory, "--version", "21.01.78-beta");
            await ExecuteAsync(checkoutArgs.ToArray());

            Assert.That(Path.Combine(destinationDirectory, "file.txt"), Does.Exist);
            Assert.That(File.ReadAllText(Path.Combine(destinationDirectory, "file.txt")), Is.EqualTo("Test"));
        }

        [Test]
        public async Task CanCheckoutViaLaterVersion()
        {
            var sourceDirectory = Path.Combine(Default.TemporaryDirectory, "source");
            Directory.CreateDirectory(sourceDirectory);
            File.WriteAllText(Path.Combine(sourceDirectory, "file.txt"), "Test");

            var commitArgs = AddTestSandbox("commit", "teststash", sourceDirectory, "--version", "21.01.78-beta");
            await ExecuteAsync(commitArgs.ToArray());

            var destinationDirectory = Path.Combine(Default.TemporaryDirectory, "destination");

            var checkoutArgs = AddTestSandbox("checkout", "teststash", destinationDirectory, "--version", "21.01.108-beta");
            await ExecuteAsync(checkoutArgs.ToArray());

            Assert.That(Path.Combine(destinationDirectory, "file.txt"), Does.Exist);
            Assert.That(File.ReadAllText(Path.Combine(destinationDirectory, "file.txt")), Is.EqualTo("Test"));
        }

        [Test]
        public async Task CanCommitAndShow()
        {
            var sourceDirectory = Path.Combine(Default.TemporaryDirectory, "source");
            Directory.CreateDirectory(sourceDirectory);
            File.WriteAllText(Path.Combine(sourceDirectory, "file.txt"), "Test");

            var commitArgs = AddTestSandbox("commit", "teststash", sourceDirectory, "--version", "21.01.78-beta");
            await ExecuteAsync(commitArgs.ToArray());

            var showArgs = AddTestSandbox("show", "teststash", "--version", "21.01.108-beta");
            var showResult = await ExecuteAsync(showArgs.ToArray());

            Assert.That(showResult, Is.EqualTo(VersionMarkerStringConverter.ForIdentifierRoundtrip().ToString(new VersionMarker(SemanticVersion.FromString("21.01.78-beta"))) + Environment.NewLine));
        }

        [Test]
        public async Task CanCommitAndShowByCommitHash()
        {
            var gitSession = await Default.GitSession();
            var workingCopy = await CreateGitWorkingCopy(gitSession);
            await gitSession.CreateBranchAndCheckout(workingCopy, "some-branch");
            await gitSession.Commit(workingCopy, "1", CommitOptions.AllowEmptyCommit);
            await gitSession.Commit(workingCopy, "2", CommitOptions.AllowEmptyCommit);
            var headRef = await gitSession.ResolveRef(workingCopy, Ref.Head);

            var expectedVersion = new SemanticVersion("21", "01", 2, new AlphaTagFormatter().Format(headRef));

            var sourceDirectory = Path.Combine(Default.TemporaryDirectory, "source");
            Directory.CreateDirectory(sourceDirectory);
            File.WriteAllText(Path.Combine(sourceDirectory, "file.txt"), "Test");

            var commitArgs = AddTestSandbox(workingCopy, "commit", "teststash", sourceDirectory, "--hash", headRef.ToString());
            await ExecuteAsync(commitArgs.ToArray());

            var showArgs = AddTestSandbox(workingCopy, "show", "teststash", "--hash", headRef.ToString());
            var showResult = await ExecuteAsync(showArgs.ToArray());

            Assert.That(showResult, Is.EqualTo(VersionMarkerStringConverter.ForIdentifierRoundtrip().ToString(new VersionMarker(expectedVersion, headRef.ToString())) + Environment.NewLine));
        }

        [Test]
        public async Task CanCommitAndList()
        {
            var sourceDirectory = Path.Combine(Default.TemporaryDirectory, "source");
            Directory.CreateDirectory(sourceDirectory);
            File.WriteAllText(Path.Combine(sourceDirectory, "file.txt"), "Test");

            var commitArgs = AddTestSandbox("commit", "teststash", sourceDirectory, "--version", "21.01.78-beta");
            await ExecuteAsync(commitArgs.ToArray());

            var listArgs = AddTestSandbox("list", "teststash");
            var listResult = await ExecuteAsync(listArgs.ToArray());

            Assert.That(listResult, Is.EqualTo(VersionMarkerStringConverter.ForIdentifierRoundtrip().ToString(new VersionMarker(SemanticVersion.FromString("21.01.78-beta"))) + Environment.NewLine));
        }

        [Test]
        public async Task CanCommitAndDelete()
        {
            var sourceDirectory = Path.Combine(Default.TemporaryDirectory, "source");
            Directory.CreateDirectory(sourceDirectory);
            File.WriteAllText(Path.Combine(sourceDirectory, "file.txt"), "Test");

            var commitArgs = AddTestSandbox("commit", "teststash", sourceDirectory, "--version", "21.01.78-beta");
            await ExecuteAsync(commitArgs.ToArray());

            var deleteArgs = AddTestSandbox("delete", "teststash", "--version", "21.01.78-beta");
            await ExecuteAsync(deleteArgs.ToArray());

            var listArgs = AddTestSandbox("list", "teststash");
            var listResult = await ExecuteAsync(listArgs.ToArray());

            Assert.That(listResult, Is.Empty);
        }

        [Test]
        public async Task CanCommitAndDeleteByIdentifier()
        {
            var sourceDirectory = Path.Combine(Default.TemporaryDirectory, "source");
            Directory.CreateDirectory(sourceDirectory);
            File.WriteAllText(Path.Combine(sourceDirectory, "file.txt"), "Test");

            var commitArgs = AddTestSandbox("commit", "teststash", sourceDirectory, "--version", "21.01.78-beta");
            await ExecuteAsync(commitArgs.ToArray());

            var showArgs = AddTestSandbox("show", "teststash", "--version", "21.01.108-beta");
            var showResult = await ExecuteAsync(showArgs.ToArray());

            Assume.That(showResult, Is.Not.Empty);

            var deleteArgs = AddTestSandbox("delete", "teststash", "--identifier", showResult.TrimEnd());
            await ExecuteAsync(deleteArgs.ToArray());

            var listArgs = AddTestSandbox("list", "teststash");
            var listResult = await ExecuteAsync(listArgs.ToArray());

            Assert.That(listResult, Is.Empty);
        }

        [Test]
        public async Task CanInvokeGC()
        {
            var commitArgs = AddTestSandbox("gc", "teststash");
            await ExecuteAsync(commitArgs.ToArray());
        }

        private async Task<string> ExecuteAsync(string[] args)
        {
            var stderr = new StringWriter();
            var stdout = new StringWriter();
            var app = Program.Configure(Program.CreateDefaultApplication(), new CommandLineApplication { Out = stdout, Error = stderr });
            var exitCode = await app.ExecuteAsync(args);
            Assert.That(exitCode, Is.Zero, "STDERR: {0}", stderr);
            return stdout.ToString();
        }

        private IEnumerable<string> AddTestSandbox(params string[] args) => AddTestSandbox(null, args);

        private IEnumerable<string> AddTestSandbox(GitWorkingCopy? getWorkingCopy, params string[] args)
        {
            yield return "--stash-root";
            yield return Path.Combine(Default.TemporaryDirectory, ".stashes");
            yield return "--git-topology";
            yield return getWorkingCopy?.Root ?? "";
            foreach (var arg in args) yield return arg;
        }

        private async Task<GitWorkingCopy> CreateGitWorkingCopy(GitSession gitSession)
        {
            var workingCopy = await gitSession.Init(Default.TemporaryDirectory, "repository");
            await gitSession.Commit(workingCopy, "Initial commit", CommitOptions.AllowEmptyCommit);
            File.AppendAllText(workingCopy.Path(".current-version"), "21.01");
            await gitSession.AddFile(workingCopy, ".current-version");
            await gitSession.Commit(workingCopy, "Start of 21.01");
            await gitSession.CreateTag(workingCopy, "21.01", Ref.Head, "Start");
            return workingCopy;
        }
    }
}

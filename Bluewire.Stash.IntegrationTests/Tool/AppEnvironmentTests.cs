using System;
using System.Linq;
using Bluewire.Stash.IntegrationTests.TestInfrastructure;
using Bluewire.Stash.Tool;
using Moq;
using NUnit.Framework;

namespace Bluewire.Stash.IntegrationTests.Tool
{
    /// <summary>
    /// AppEnvironment is handled the same for all commands, so only bother testing it for the diagnostics case.
    /// </summary>
    [TestFixture]
    public class AppEnvironmentTests
    {
        [Test]
        public void UsesCurrentDirectoryIfNoGitTopologyPathIsSpecified()
        {
            var application = Mock.Of<StubApplication>(a =>
                    a.GetCurrentDirectory() == @"c:\some\dir")
                .CallBase();

            var app = Program.Configure(application);
            app.Execute("diagnostics");

            var model = application.Invocations.OfType<DiagnosticsArguments>().SingleOrDefault();
            Assert.That(model, Is.Not.Null);
            Assert.That(model!.AppEnvironment.GitTopologyPath, Is.EqualTo(new ArgumentValue<string>(@"c:\some\dir", ArgumentSource.Default)));
        }

        [Test]
        public void UsesSpecifiedDirectoryForGitTopologyPath()
        {
            var application = Mock.Of<StubApplication>(a =>
                    a.GetCurrentDirectory() == @"c:\some\dir")
                .CallBase();

            var app = Program.Configure(application);
            app.Execute("-C", @"e:\some\repo", "diagnostics");

            var model = application.Invocations.OfType<DiagnosticsArguments>().SingleOrDefault();
            Assert.That(model, Is.Not.Null);
            Assert.That(model!.AppEnvironment.GitTopologyPath, Is.EqualTo(new ArgumentValue<string>(@"e:\some\repo", ArgumentSource.Argument)));
        }

        [Test]
        public void ResolvesSpecifiedDirectoryForGitTopologyPathRelativeToCurrentDirectory()
        {
            var application = Mock.Of<StubApplication>(a =>
                    a.GetCurrentDirectory() == @"c:\some\dir")
                .CallBase();

            var app = Program.Configure(application);
            app.Execute("-C", "repo", "diagnostics");

            var model = application.Invocations.OfType<DiagnosticsArguments>().SingleOrDefault();
            Assert.That(model, Is.Not.Null);
            Assert.That(model!.AppEnvironment.GitTopologyPath, Is.EqualTo(new ArgumentValue<string>(@"c:\some\dir\repo", ArgumentSource.Argument)));
        }

        [Test]
        public void UsesNoGitTopologyPathIfExplicitlyRequested()
        {
            var application = Mock.Of<StubApplication>(a =>
                    a.GetCurrentDirectory() == @"c:\some\dir")
                .CallBase();

            var app = Program.Configure(application);
            app.Execute("-C", "", "diagnostics");

            var model = application.Invocations.OfType<DiagnosticsArguments>().SingleOrDefault();
            Assert.That(model, Is.Not.Null);
            Assert.That(model!.AppEnvironment.GitTopologyPath, Is.EqualTo(new ArgumentValue<string?>(null, ArgumentSource.Argument)));
        }

        [Test]
        public void UsesTemporaryDirectoryStashRootIfNoEnvironmentVariableOrArgumentIsSpecified()
        {
            var application = Mock.Of<StubApplication>(a =>
                    a.GetTemporaryDirectory() == @"c:\temp" &&
                    a.GetEnvironmentVariable("STASH_ROOT") == null)
                .CallBase();

            var app = Program.Configure(application);
            app.Execute("diagnostics");

            var model = application.Invocations.OfType<DiagnosticsArguments>().SingleOrDefault();
            Assert.That(model, Is.Not.Null);
            Assert.That(model!.AppEnvironment.StashRoot, Is.EqualTo(new ArgumentValue<string>(@"c:\temp\.stashes\", ArgumentSource.Default)));
        }

        [Test]
        public void UsesEnvironmentVariableStashRootIfNoArgumentIsSpecified()
        {
            var application = Mock.Of<StubApplication>(a =>
                    a.GetTemporaryDirectory() == @"c:\temp" &&
                    a.GetEnvironmentVariable("STASH_ROOT") == @"c:\Users\Me\temp\stashes")
                .CallBase();

            var app = Program.Configure(application);
            app.Execute("diagnostics");

            var model = application.Invocations.OfType<DiagnosticsArguments>().SingleOrDefault();
            Assert.That(model, Is.Not.Null);
            Assert.That(model!.AppEnvironment.StashRoot, Is.EqualTo(new ArgumentValue<string>(@"c:\Users\Me\temp\stashes\", ArgumentSource.Environment)));
        }

        [Test]
        public void UsesSpecifiedDirectoryForStashRoot()
        {
            var application = Mock.Of<StubApplication>(a =>
                    a.GetTemporaryDirectory() == @"c:\temp" &&
                    a.GetEnvironmentVariable("STASH_ROOT") == @"c:\Users\Me\temp\stashes")
                .CallBase();

            var app = Program.Configure(application);
            app.Execute("-S", @"e:\some\repo\stashes\", "diagnostics");

            var model = application.Invocations.OfType<DiagnosticsArguments>().SingleOrDefault();
            Assert.That(model, Is.Not.Null);
            Assert.That(model!.AppEnvironment.StashRoot, Is.EqualTo(new ArgumentValue<string>(@"e:\some\repo\stashes\", ArgumentSource.Argument)));
        }

        [Test]
        public void UsesEnvironmentVariableRemoteStashRootIfNoArgumentIsSpecified()
        {
            var application = Mock.Of<StubApplication>(a =>
                    a.GetEnvironmentVariable("REMOTE_STASH_ROOT") == @"https://server.com/stash")
                .CallBase();

            var app = Program.Configure(application);
            app.Execute("diagnostics");

            var model = application.Invocations.OfType<DiagnosticsArguments>().SingleOrDefault();
            Assert.That(model, Is.Not.Null);
            Assert.That(model!.AppEnvironment.RemoteStashRoot, Is.EqualTo(new ArgumentValue<Uri?>(new Uri("https://server.com/stash/"), ArgumentSource.Environment)));
        }

        [Test]
        public void UsesSpecifiedUriForRemoteStashRoot()
        {
            var application = Mock.Of<StubApplication>(a =>
                    a.GetEnvironmentVariable("REMOTE_STASH_ROOT") == @"https://server.com/stash")
                .CallBase();

            var app = Program.Configure(application);
            app.Execute("-R", @"https://other.server/test", "diagnostics");

            var model = application.Invocations.OfType<DiagnosticsArguments>().SingleOrDefault();
            Assert.That(model, Is.Not.Null);
            Assert.That(model!.AppEnvironment.RemoteStashRoot, Is.EqualTo(new ArgumentValue<Uri?>(new Uri("https://other.server/test/"), ArgumentSource.Argument)));
        }

        [Test]
        public void InvalidSpecifiedUriOverridesEnvironmentVariableRemoteStashRoot()
        {
            var application = Mock.Of<StubApplication>(a =>
                    a.GetEnvironmentVariable("REMOTE_STASH_ROOT") == @"https://server.com/stash")
                .CallBase();

            var app = Program.Configure(application);
            app.Execute("-R", @":///not.valid.uri", "diagnostics");

            var model = application.Invocations.OfType<DiagnosticsArguments>().SingleOrDefault();
            Assert.That(model, Is.Not.Null);
            Assert.That(model!.AppEnvironment.RemoteStashRoot, Is.EqualTo(new ArgumentValue<Uri?>(null, ArgumentSource.Argument)));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bluewire.Conventions;
using Bluewire.Stash.IntegrationTests.TestInfrastructure;
using Bluewire.Stash.Tool;
using Moq;
using NUnit.Framework;

namespace Bluewire.Stash.IntegrationTests.Tool
{
    [TestFixture]
    public class CommitCommandArgumentsTests
    {
        [Test]
        public void ParsesAllArguments()
        {
            var application = Mock.Of<StubApplication>(a =>
                a.GetCurrentDirectory() == @"c:\working-dir")
                .CallBase();

            var app = Program.Configure(application);
            app.Execute("commit", "teststash", "sourcepath", "--version", "21.01.4-beta", "--hash", "some-hash", "--force");

            var model = application.Invocations.OfType<CommitArguments>().SingleOrDefault();
            Assert.That(model, Is.Not.Null);
            Assert.That(model!.StashName, Is.EqualTo(new ArgumentValue<string>("teststash", ArgumentSource.Argument)));
            Assert.That(model!.SourcePath, Is.EqualTo(new ArgumentValue<string>(@"c:\working-dir\sourcepath\", ArgumentSource.Argument)));

            Assert.That(model!.Version.Value, Is.EqualTo(new VersionMarker(SemanticVersion.FromString("21.01.4-beta"), "some-hash")).Using(VersionMarker.EqualityComparer));
            Assert.That(model!.Version.Source, Is.EqualTo(ArgumentSource.Argument));

            Assert.That(model!.Force, Is.EqualTo(new ArgumentValue<bool>(true, ArgumentSource.Argument)));
        }
    }
}

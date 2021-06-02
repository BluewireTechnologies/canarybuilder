using System.Linq;
using Bluewire.Conventions;
using Bluewire.Stash.IntegrationTests.TestInfrastructure;
using Bluewire.Stash.Tool;
using Moq;
using NUnit.Framework;

namespace Bluewire.Stash.IntegrationTests.Tool
{
    [TestFixture]
    public class DeleteCommandArgumentsTests
    {
        [Test]
        public void ParsesVersionOrHashArguments()
        {
            var application = Mock.Of<StubApplication>(a =>
                a.GetCurrentDirectory() == @"c:\working-dir")
                .CallBase();

            var app = Program.Configure(application);
            app.Execute("delete", "teststash", "--version", "21.01.4-beta", "--hash", "some-hash");

            var model = application.Invocations.OfType<DeleteArguments>().SingleOrDefault();
            Assert.That(model, Is.Not.Null);
            Assert.That(model!.StashName, Is.EqualTo(new ArgumentValue<string>("teststash", ArgumentSource.Argument)));

            Assert.That(model!.Version.Value, Is.EqualTo(new VersionMarker(SemanticVersion.FromString("21.01.4-beta"), "some-hash")).Using(VersionMarker.EqualityComparer));
            Assert.That(model!.Version.Source, Is.EqualTo(ArgumentSource.Argument));
        }

        [Test]
        public void ParsesIdentifierArguments()
        {
            var application = Mock.Of<StubApplication>(a =>
                    a.GetCurrentDirectory() == @"c:\working-dir")
                .CallBase();

            var app = Program.Configure(application);
            app.Execute("delete", "teststash", "--identifier", "some-hash:21.01.4-beta");

            var model = application.Invocations.OfType<DeleteArguments>().SingleOrDefault();
            Assert.That(model, Is.Not.Null);
            Assert.That(model!.StashName, Is.EqualTo(new ArgumentValue<string>("teststash", ArgumentSource.Argument)));

            Assert.That(model!.Version.Value, Is.EqualTo(new VersionMarker(SemanticVersion.FromString("21.01.4-beta"), "some-hash")).Using(VersionMarker.EqualityComparer));
            Assert.That(model!.Version.Source, Is.EqualTo(ArgumentSource.Argument));
        }
    }
}

using System.Linq;
using Bluewire.Stash.IntegrationTests.TestInfrastructure;
using Bluewire.Stash.Tool;
using Moq;
using NUnit.Framework;

namespace Bluewire.Stash.IntegrationTests.Tool
{
    [TestFixture]
    public class DiagnosticsCommandArgumentsTests
    {
        [Test]
        public void ParsesPlainCommand()
        {
            var application = Mock.Of<StubApplication>(a =>
                a.GetCurrentDirectory() == @"c:\working-dir")
                .CallBase();

            var app = Program.Configure(application);
            app.Execute("diagnostics");

            var model = application.Invocations.OfType<DiagnosticsArguments>().SingleOrDefault();
            Assert.That(model, Is.Not.Null);
        }

        [Test]
        public void ParsesRemoteStashName()
        {
            var application = Mock.Of<StubApplication>(a =>
                    a.GetCurrentDirectory() == @"c:\working-dir")
                .CallBase();

            var app = Program.Configure(application);
            app.Execute("diagnostics", "teststash");

            var model = application.Invocations.OfType<DiagnosticsArguments>().SingleOrDefault();
            Assert.That(model, Is.Not.Null);
            Assert.That(model!.RemoteStashName, Is.EqualTo(new ArgumentValue<string>("teststash", ArgumentSource.Argument)));
        }
    }
}

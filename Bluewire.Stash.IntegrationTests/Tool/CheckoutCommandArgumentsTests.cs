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
    public class CheckoutCommandArgumentsTests
    {
        [Test]
        public void ParsesAllArguments()
        {
            var application = Mock.Of<StubApplication>(a =>
                a.GetCurrentDirectory() == @"c:\working-dir")
                .CallBase();

            var app = Program.Configure(application);
            app.Execute("checkout", "teststash", "targetpath", "--version", "21.01.4-beta", "--hash", "some-hash");

            var model = application.Invocations.OfType<CheckoutArguments>().SingleOrDefault();
            Assert.That(model, Is.Not.Null);
            Assert.That(model!.StashName, Is.EqualTo(new ArgumentValue<string>("teststash", ArgumentSource.Argument)));
            Assert.That(model!.DestinationPath, Is.EqualTo(new ArgumentValue<string>(@"c:\working-dir\targetpath\", ArgumentSource.Argument)));

            Assert.That(model!.Version.Value, Is.EqualTo(new VersionMarker(SemanticVersion.FromString("21.01.4-beta"), "some-hash")).Using(VersionMarker.EqualityComparer));
            Assert.That(model!.Version.Source, Is.EqualTo(ArgumentSource.Argument));
        }
    }
}

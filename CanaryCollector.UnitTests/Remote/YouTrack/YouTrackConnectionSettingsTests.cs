using System;
using CanaryCollector.Remote.YouTrack;
using NUnit.Framework;

namespace CanaryCollector.UnitTests.Remote.YouTrack
{
    [TestFixture]
    public class YouTrackConnectionSettingsTests
    {
        [Test]
        public void ParsesPlainHttpUri()
        {
            var parsed = YouTrackConnectionSettings.Parse(new Uri("http://hostname/youtrack/"));

            Assert.That(parsed.Host, Is.EqualTo("hostname"));
            Assert.That(parsed.Port, Is.EqualTo(80));
            Assert.That(parsed.Path, Is.EqualTo("youtrack"));
            Assert.That(parsed.UseSSL, Is.False);
        }

        [Test]
        public void ParsesAuthenticatedHttpsUri()
        {
            var parsed = YouTrackConnectionSettings.Parse(new Uri("https://myuser:mypassword@mysite.myjetbrains.com/youtrack/"));

            Assert.That(parsed.Host, Is.EqualTo("mysite.myjetbrains.com"));
            Assert.That(parsed.Port, Is.EqualTo(443));
            Assert.That(parsed.Path, Is.EqualTo("youtrack"));
            Assert.That(parsed.UseSSL, Is.True);
            Assert.That(parsed.Credentials.UserName, Is.EqualTo("myuser"));
            Assert.That(parsed.Credentials.Password, Is.EqualTo("mypassword"));
        }
    }
}

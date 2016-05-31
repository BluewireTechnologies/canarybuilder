using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using RefCleaner.Collectors;

namespace Bluewire.Common.GitWrapper.UnitTests.Parsing
{
    [TestFixture]
    public class StrictISO8601Tests
    {
        [Test]
        public void ParsesValidDatestamp()
        {
            var datestamp = new DateTimeOffset(2016, 06, 01, 14, 37, 32, TimeSpan.FromHours(1));
            var str = "2016-06-01T14:37:32+01:00";
            Assert.That(StrictISO8601.TryParseExact(str), Is.EqualTo(datestamp));
        }
    }
}

using System.IO;
using Bluewire.Common.Git.Model;
using CanaryBuilder.Merge;
using CanaryBuilder.Parsers;
using NUnit.Framework;

namespace CanaryBuilder.UnitTests.Merge
{
    [TestFixture]
    public class MergeJobValidationTests
    {
        private MergeJobParser validator = new MergeJobParser();

        [Test]
        public void BaseMustBeSpecified()
        {
            Assert.Throws<MissingParameterException>(() => validator.Validate(new MergeJobDefinition()));
            Assert.DoesNotThrow(() => validator.Validate(new MergeJobDefinition { Base = new Ref("master") } ));
        }
    }
}

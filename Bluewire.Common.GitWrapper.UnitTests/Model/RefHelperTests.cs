using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper.Model;
using NUnit.Framework;

namespace Bluewire.Common.GitWrapper.UnitTests.Model
{
    [TestFixture]
    public class RefHelperTests
    {
        [Test]
        public void UnqualifyStripsPrefixesFromBranchNames()
        {
            Assert.That(RefHelper.Unqualify("heads", new Ref("refs/heads/branchName")), Is.EqualTo(new Ref("branchName")));
            Assert.That(RefHelper.Unqualify("heads", new Ref("heads/branch/name")), Is.EqualTo(new Ref("branch/name")));
        }

        [Test]
        public void UnqualifyFailsWhenAppliedToBareHierarchies()
        {
            Assert.Throws<ArgumentException>(() => RefHelper.Unqualify("heads", new Ref("refs/heads")));
            Assert.Throws<ArgumentException>(() => RefHelper.Unqualify("tags", new Ref("tags")));
        }
    }
}

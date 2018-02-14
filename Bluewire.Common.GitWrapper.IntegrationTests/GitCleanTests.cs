using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper.Model;
using NUnit.Framework;

namespace Bluewire.Common.GitWrapper.IntegrationTests
{
    [TestFixture]
    public class GitCleanTests
    {
        private GitSession session;
        private GitWorkingCopy workingCopy;

        [SetUp]
        public async Task SetUp()
        {
            session = await Default.GitSession();
            workingCopy = await session.Init(Default.TemporaryDirectory, "repository");
            await session.Commit(workingCopy, "Initial commit", CommitOptions.AllowEmptyCommit);
        }

        [Test]
        public async Task CanResetCompletelyClean()
        {
            File.WriteAllText(workingCopy.Path("temp.txt"), "Leave working copy dirty.");

            Assert.False(await session.IsClean(workingCopy));

            await session.ResetCompletelyClean(workingCopy);

            Assert.True(await session.IsClean(workingCopy));
        }

        [Test]
        public async Task CanResetToRef_CompletelyClean()
        {
            var branch = await session.CreateBranchAndCheckout(workingCopy, "input-branch");
            await session.Commit(workingCopy, "Branch commit", CommitOptions.AllowEmptyCommit);
            await session.Checkout(workingCopy, new Ref("master"));
            await session.Merge(workingCopy, branch);

            File.WriteAllText(workingCopy.Path("temp.txt"), "Leave working copy dirty.");

            Assert.False(await session.IsClean(workingCopy));

            await session.ResetCompletelyClean(workingCopy, Ref.Head.Parent());

            Assert.True(await session.IsClean(workingCopy));
        }
    }
}

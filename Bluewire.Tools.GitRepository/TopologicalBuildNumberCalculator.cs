using System;
using System.Linq;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper;
using Bluewire.Common.GitWrapper.Model;

namespace Bluewire.Tools.GitRepository
{
    /// <summary>
    /// Calculate a build number based on the number of commits between the subject commit and a start commit.
    /// </summary>
    /// <remarks>
    /// Reference implementation of the optimised algorithm used in TopologicalBuildNumberProvider.
    /// </remarks>
    public class TopologicalBuildNumberCalculator
    {
        private readonly GitSession session;

        public TopologicalBuildNumberCalculator(GitSession session)
        {
            this.session = session;
        }

        public async Task<int?> GetBuildNumber(IGitFilesystemContext workingCopyOrRepo, Ref start, Ref subject)
        {
            if (workingCopyOrRepo == null) throw new ArgumentNullException(nameof(workingCopyOrRepo));
            if (start == null) throw new ArgumentNullException(nameof(start));
            if (subject == null) throw new ArgumentNullException(nameof(subject));

            var cmd = session.CommandHelper.CreateCommand("rev-list", new Difference(start, subject), "--count");
            var line = await session.CommandHelper.RunSingleLineCommand(workingCopyOrRepo, cmd);

            int buildNumber;
            if (!int.TryParse(line, out buildNumber)) return null;
            return buildNumber;
        }
    }
}

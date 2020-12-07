using System;

namespace Bluewire.Tools.Builds.Shared
{
    public class PullRequestMergeNotFoundException : ApplicationException
    {
        public PullRequestMergeNotFoundException(int pullRequestNumber) : base($"Could not find a merge of PR #{pullRequestNumber}.")
        {
        }
    }
}

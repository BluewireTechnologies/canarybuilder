using System;
using Bluewire.Common.GitWrapper.Model;

namespace Bluewire.Tools.Builds.Shared
{
    public class PullRequestMergesHaveNoCommonParentException : ApplicationException
    {
        public PullRequestMergesHaveNoCommonParentException(int pullRequestNumber, Ref[] prMerges)
            : base($"Could not find a common parent of all {prMerges.Length} merges of PR #{pullRequestNumber}.")
        {
        }
    }
}

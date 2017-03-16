using System;

namespace Bluewire.Tools.GitRepository
{
    public class CommitNotInAncestryChainException : InvalidOperationException
    {
        public CommitNotInAncestryChainException(string message) : base(message)
        {
        }
    }
}

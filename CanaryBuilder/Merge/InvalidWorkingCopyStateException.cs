using System;

namespace CanaryBuilder.Merge
{
    public class InvalidWorkingCopyStateException : JobRunnerException
    {
        public InvalidWorkingCopyStateException(string messageDetail) : base(messageDetail)
        {
        }
    }
}

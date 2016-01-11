using System;

namespace CanaryBuilder.Merge
{
    public abstract class JobRunnerException : ApplicationException
    {
        protected JobRunnerException(string message) : base(message)
        {
        }
    }
}
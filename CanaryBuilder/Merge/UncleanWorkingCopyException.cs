namespace CanaryBuilder.Merge
{
    public class UncleanWorkingCopyException : JobRunnerException
    {
        public UncleanWorkingCopyException() : base("Working copy is not clean.")
        {
        }
    }
}
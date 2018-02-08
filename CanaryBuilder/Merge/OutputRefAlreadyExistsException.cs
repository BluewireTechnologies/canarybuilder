namespace CanaryBuilder.Merge
{
    public class OutputRefAlreadyExistsException : JobRunnerException
    {
        public OutputRefAlreadyExistsException(string message) : base(message)
        {
        }
    }
}


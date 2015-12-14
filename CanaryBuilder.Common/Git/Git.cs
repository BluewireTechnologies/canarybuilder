namespace CanaryBuilder.Common.Git
{
    /// <summary>
    /// Wraps invocation of the Git binary.
    /// </summary>
    public class Git
    {
        private readonly string exePath;

        public Git(string exePath)
        {
            this.exePath = exePath;
        }


        public void Validate()
        {
            // check that the binary can execute
        }
        
    }
}
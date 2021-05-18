namespace Bluewire.Stash.Tool
{
    public class GCArguments
    {
        public GCArguments(AppEnvironment appEnvironment)
        {
            AppEnvironment = appEnvironment;
        }

        public AppEnvironment AppEnvironment { get; }

        public ArgumentValue<string> StashName { get; set; }
        public ArgumentValue<bool> Aggressive { get; set; }
        public ArgumentValue<int> Verbosity { get; set; }
    }
}

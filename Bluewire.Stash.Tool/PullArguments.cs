namespace Bluewire.Stash.Tool
{
    public class PullArguments
    {
        public PullArguments(AppEnvironment appEnvironment)
        {
            AppEnvironment = appEnvironment;
        }

        public AppEnvironment AppEnvironment { get; }

        public ArgumentValue<string> StashName { get; set; }
        public ArgumentValue<string> RemoteStashName { get; set; }
        public ArgumentValue<VersionMarker?> Version { get; set; }
        public ArgumentValue<bool> IgnoreIfExists { get; set; }
        public ArgumentValue<int> Verbosity { get; set; }
    }
}

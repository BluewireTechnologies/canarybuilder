namespace Bluewire.Stash.Tool
{
    public class CommitArguments
    {
        public CommitArguments(AppEnvironment appEnvironment)
        {
            AppEnvironment = appEnvironment;
        }

        public AppEnvironment AppEnvironment { get; }

        public ArgumentValue<string> StashName { get; set; }
        public ArgumentValue<string> SourcePath { get; set; }
        public ArgumentValue<VersionMarker?> Version { get; set; }
        public ArgumentValue<bool> Force { get; set; }
        public ArgumentValue<int> Verbosity { get; set; }
    }
}

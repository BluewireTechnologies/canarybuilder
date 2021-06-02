namespace Bluewire.Stash.Tool
{
    public class ShowArguments
    {
        public ShowArguments(AppEnvironment appEnvironment)
        {
            AppEnvironment = appEnvironment;
        }

        public AppEnvironment AppEnvironment { get; }

        public ArgumentValue<string> StashName { get; set; }
        public ArgumentValue<VersionMarker?> Version { get; set; }
        public ArgumentValue<bool> ExactMatch { get; set; }
        public ArgumentValue<int> Verbosity { get; set; }
    }
}

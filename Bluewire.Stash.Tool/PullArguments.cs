using static Bluewire.Stash.Tool.PullCommand;

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
        public ArgumentValue<ExistsBehaviour> ExistsLocallyBehaviour { get; set; }
        public ArgumentValue<int> Verbosity { get; set; }
    }
}

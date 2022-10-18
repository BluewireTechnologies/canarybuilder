namespace Bluewire.Stash.Tool
{
    public class PushArguments
    {
        public PushArguments(AppEnvironment appEnvironment)
        {
            AppEnvironment = appEnvironment;
        }

        public AppEnvironment AppEnvironment { get; }

        public ArgumentValue<string> StashName { get; set; }
        public ArgumentValue<string> RemoteStashName { get; set; }
        public ArgumentValue<VersionMarker> Version { get; set; }
        public ArgumentValue<ExistsBehaviour> ExistsRemotelyBehaviour { get; set; }
        public ArgumentValue<int> Verbosity { get; set; }
    }
}

namespace Bluewire.Stash.Tool
{
    public class RemoteDeleteArguments
    {
        public RemoteDeleteArguments(AppEnvironment appEnvironment)
        {
            AppEnvironment = appEnvironment;
        }

        public AppEnvironment AppEnvironment { get; }

        public ArgumentValue<string> RemoteStashName { get; set; }
        public ArgumentValue<VersionMarker> Version { get; set; }
        public ArgumentValue<int> Verbosity { get; set; }
    }
}

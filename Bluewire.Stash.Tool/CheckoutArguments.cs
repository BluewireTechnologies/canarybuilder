namespace Bluewire.Stash.Tool
{
    public class CheckoutArguments
    {
        public CheckoutArguments(AppEnvironment appEnvironment)
        {
            AppEnvironment = appEnvironment;
        }

        public AppEnvironment AppEnvironment { get; }

        public ArgumentValue<string> StashName { get; set; }
        public ArgumentValue<string> DestinationPath { get; set; }
        public ArgumentValue<VersionMarker?> Version { get; set; }
        public ArgumentValue<int> Verbosity { get; set; }
    }
}

namespace Bluewire.Stash.Tool
{
    public class ListArguments
    {
        public ListArguments(AppEnvironment appEnvironment)
        {
            AppEnvironment = appEnvironment;
        }

        public AppEnvironment AppEnvironment { get; }

        public ArgumentValue<string> StashName { get; set; }
        public ArgumentValue<int> Verbosity { get; set; }
    }
}

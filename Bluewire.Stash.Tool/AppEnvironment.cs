namespace Bluewire.Stash.Tool
{
    public class AppEnvironment
    {
        public AppEnvironment(ArgumentValue<string?> gitTopologyPath, ArgumentValue<string> stashRoot)
        {
            GitTopologyPath = gitTopologyPath;
            StashRoot = stashRoot;
        }

        public ArgumentValue<string?> GitTopologyPath { get; }
        public ArgumentValue<string> StashRoot { get; }
    }
}

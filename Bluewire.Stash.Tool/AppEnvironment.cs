using System;

namespace Bluewire.Stash.Tool
{
    public class AppEnvironment
    {
        public AppEnvironment(ArgumentValue<string?> gitTopologyPath, ArgumentValue<string> stashRoot, ArgumentValue<Uri?> remoteStashRoot)
        {
            GitTopologyPath = gitTopologyPath;
            StashRoot = stashRoot;
            RemoteStashRoot = remoteStashRoot;
        }

        public ArgumentValue<string?> GitTopologyPath { get; }
        public ArgumentValue<string> StashRoot { get; }
        public ArgumentValue<Uri?> RemoteStashRoot { get; }
    }
}

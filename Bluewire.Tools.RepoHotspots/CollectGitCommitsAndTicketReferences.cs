using System.Collections.Generic;
using Bluewire.Common.GitWrapper;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper.Model;

namespace Bluewire.Tools.RepoHotspots
{
    public class CollectGitCommitsAndTicketReferences
    {
        private readonly GitSession gitSession;

        public CollectGitCommitsAndTicketReferences(GitSession gitSession)
        {
            this.gitSession = gitSession;
        }

        public async Task<IDictionary<string, CommitRecord>> ReadAllCommits(IGitFilesystemContext workingCopyOrRepo, List<Ref> startPoints)
        {
            var cmd = gitSession.CommandHelper.CreateCommand("rev-list", "--count", "--all");
            var commitCountString = await gitSession.CommandHelper.RunSingleLineCommand(workingCopyOrRepo, cmd);

            var commitCount = int.TryParse(commitCountString, out var value) ? value : 50000;

            var state = new State(commitCount);

            foreach (var tip in startPoints)
            {
                ParseCommits(state, workingCopyOrRepo, tip);
            }

            return state.Commits;
        }

        private void ParseCommits(State state, IGitFilesystemContext workingCopyOrRepo, Ref branch)
        {
        }

        class State
        {
            public State(int commitCount)
            {
                // Guess: number of unique paths about the same as the number of commits?
                PathTable = new StringTable(commitCount);
                Commits = new Dictionary<string, CommitRecord>(commitCount);
            }

            public Dictionary<string, CommitRecord> Commits { get; }
            public StringTable PathTable { get; }
        }
    }
}

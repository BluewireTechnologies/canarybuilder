using Bluewire.Common.GitWrapper;
using Bluewire.Common.GitWrapper.Model;
using Bluewire.Conventions;
using Bluewire.Tools.Builds.Shared;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace Bluewire.Tools.Builds.FindTickets
{
    public class ResolveTicketsBetweenRefs : ITicketsResolutionJob
    {
        private Ref startRef;
        private Ref endRef;

        public ResolveTicketsBetweenRefs(Ref startRef, Ref endRef)
        {
            this.startRef = startRef;
            this.endRef = endRef;
        }

        public async Task<string[]> ResolveTickets(GitSession session, IGitFilesystemContext workingCopyOrRepo)
        {
            if (string.IsNullOrEmpty(startRef?.ToString()))
            {
                throw new InvalidOperationException("Starting Ref (commit) must be supplied");
            }
            if (string.IsNullOrEmpty(endRef?.ToString()))
            {
                throw new InvalidOperationException("Ending Ref (commit) must be supplied");
            }

            var includeCommits = await session.ReadLog(workingCopyOrRepo, new LogOptions(), new Difference(startRef, endRef));
            var excludeCommits = await session.ReadLog(workingCopyOrRepo, new LogOptions(), new Difference(endRef, startRef));

            var includeTicketsStrings = includeCommits.SelectMany(c => Patterns.TicketIdentifier.Matches(c.Message).OfType<Match>().Select(m => m.Value));
            var excludeTicketsStrings = excludeCommits.SelectMany(c => Patterns.TicketIdentifier.Matches(c.Message).OfType<Match>().Select(m => m.Value));

            return includeTicketsStrings.Except(excludeTicketsStrings).ToArray();
        }

    }
}

using System.Collections.Generic;
using System.Linq;
using Bluewire.Conventions;
using CanaryCollector.Model;

namespace CanaryCollector.Collectors
{
    /// <summary>
    /// Associates tickets with branches based on ticket identifier.
    /// </summary>
    /// <remarks>
    /// A ticket may have multiple relevant branches, destined for different release branches. We need
    /// to identify those branches specifically intended for inclusion in master.
    ///
    /// Bluewire.Conventions defines the expected branch name format. It is assumed that any branch
    /// specifically targetted at a release is not a candidate for merging into master.
    /// </remarks>
    public class TicketAndBranchAssociator
    {
        public IEnumerable<TicketLinkedBranch> Apply(IEnumerable<IssueTicket> tickets, IEnumerable<string> availableBranchNames)
        {
            var ticketedBranches = CollectTicketBranchesForMaster(availableBranchNames);

            return tickets.Join(ticketedBranches,
                t => t.Identifier,
                b => b.TicketIdentifier,
                (t, b) => new TicketLinkedBranch { Branch = b.Branch, Ticket = t });
        }

        private static IEnumerable<BranchWithTicketIdentifier> CollectTicketBranchesForMaster(IEnumerable<string> availableBranchNames)
        {
            foreach (var name in availableBranchNames)
            {
                StructuredBranch branch;
                if (!StructuredBranch.TryParse(name, out branch)) continue;
                if (branch.TargetRelease != null) continue;
                if (branch.TicketIdentifier == null) continue;

                yield return new BranchWithTicketIdentifier { Branch = new Branch { Name = name }, TicketIdentifier = branch.TicketIdentifier };
            }
        }

        struct BranchWithTicketIdentifier
        {
            public Branch Branch { get; set; }
            public string TicketIdentifier { get; set; }
        }
    }
}

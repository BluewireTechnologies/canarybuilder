using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CanaryCollector.Model;

namespace CanaryCollector.Collectors
{
    public class TaggedTicketBranchCollector : IBranchCollector
    {
        private readonly ITicketProvider ticketProvider;
        private readonly IBranchProvider branchProvider;
        private readonly string tagName;

        public TaggedTicketBranchCollector(ITicketProvider ticketProvider, IBranchProvider branchProvider, string tagName)
        {
            if (ticketProvider == null) throw new ArgumentNullException(nameof(ticketProvider));
            if (branchProvider == null) throw new ArgumentNullException(nameof(branchProvider));
            this.ticketProvider = ticketProvider;
            this.branchProvider = branchProvider;
            this.tagName = tagName;
        }

        public async Task<IEnumerable<Branch>> CollectBranches()
        {
            var pendingIssuesTask = ticketProvider.GetTicketsWithTag(tagName);

            var availableBranchesTask = branchProvider.GetUnmergedBranches("master");

            return new TicketAndBranchAssociator().Apply(await pendingIssuesTask, await availableBranchesTask)
                .OrderBy(a => a.Ticket.Type).ThenBy(a => a.Branch.Name)
                .Select(a => a.Branch);
        }
    }
}

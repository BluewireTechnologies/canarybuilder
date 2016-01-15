using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper;
using CanaryCollector.Model;

namespace CanaryCollector.Collectors
{
    public class PendingReviewTicketBranchCollector : IBranchCollector
    {
        private readonly ITicketProvider ticketProvider;
        private readonly IBranchProvider branchProvider;

        public PendingReviewTicketBranchCollector(ITicketProvider ticketProvider, IBranchProvider branchProvider)
        {
            if (ticketProvider == null) throw new ArgumentNullException(nameof(ticketProvider));
            if (branchProvider == null) throw new ArgumentNullException(nameof(branchProvider));
            this.ticketProvider = ticketProvider;
            this.branchProvider = branchProvider;
        }

        public async Task<IEnumerable<Branch>> CollectBranches()
        {
            var pendingIssues = ticketProvider.GetTicketsPendingReview();

            var availableBranches = await branchProvider.GetUnmergedBranches("master");

            return new TicketAndBranchAssociator().Apply(pendingIssues, availableBranches)
                .OrderBy(a => a.Ticket.Type).ThenBy(a => a.Branch.Name)
                .Select(a => a.Branch);
        }    
    }
}

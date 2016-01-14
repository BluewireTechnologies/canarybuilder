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
        private readonly GitRepository gitRepository;

        public PendingReviewTicketBranchCollector(ITicketProvider ticketProvider, GitRepository gitRepository)
        {
            if (ticketProvider == null) throw new ArgumentNullException(nameof(ticketProvider));
            if (gitRepository == null) throw new ArgumentNullException(nameof(gitRepository));
            this.ticketProvider = ticketProvider;
            this.gitRepository = gitRepository;
        }

        public async Task<IEnumerable<Branch>> CollectBranches()
        {
            var pendingIssues = ticketProvider.GetTicketsPendingReview();

            return pendingIssues.Select(i => new Branch { Name = $"{i.Identifier}: {i.Type}" });
        }    
    }
}
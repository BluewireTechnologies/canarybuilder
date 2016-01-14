using System.Collections.Generic;
using CanaryCollector.Model;

namespace CanaryCollector.Collectors
{
    public interface ITicketProvider
    {
        IEnumerable<IssueTicket> GetTicketsPendingReview();
    }
}
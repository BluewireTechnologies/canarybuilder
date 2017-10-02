using System.Collections.Generic;
using System.Threading.Tasks;
using CanaryCollector.Model;

namespace CanaryCollector.Collectors
{
    public interface ITicketProvider
    {
        Task<IssueTicket[]> GetTicketsPendingMerge();
        Task<IssueTicket[]> GetTicketsWithTag(string tagName);
    }
}
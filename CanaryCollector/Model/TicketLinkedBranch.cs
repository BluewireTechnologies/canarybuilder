namespace CanaryCollector.Model
{
    public struct TicketLinkedBranch
    {
        public Branch Branch { get; set; }
        public IssueTicket Ticket { get; set; }
    }
}


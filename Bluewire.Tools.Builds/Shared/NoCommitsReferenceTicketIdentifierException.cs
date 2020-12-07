using System;

namespace Bluewire.Tools.Builds.Shared
{
    public class NoCommitsReferenceTicketIdentifierException : ApplicationException
    {
        public NoCommitsReferenceTicketIdentifierException(string ticketIdentifier) : base($"Could not find any references to ticket {ticketIdentifier}.")
        {
        }
    }
}

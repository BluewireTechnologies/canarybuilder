using Bluewire.Common.Console;
using CanaryCollector.Collectors;

namespace CanaryCollector.Remote
{
    public class NoTicketProviderFactory : ITicketProviderFactory
    {
        public ITicketProvider Create(string dependentParameter)
        {
            throw new InvalidArgumentsException($"The --youtrack or --jira parameter must be specified in order to use {dependentParameter}.");
        }
    }
}

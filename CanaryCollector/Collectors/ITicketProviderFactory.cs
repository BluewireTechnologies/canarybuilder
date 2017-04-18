namespace CanaryCollector.Collectors
{
    public interface ITicketProviderFactory
    {
        ITicketProvider Create(string dependentParameter);
    }
}

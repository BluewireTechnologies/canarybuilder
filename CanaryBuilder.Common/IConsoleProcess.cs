using System.Threading;
using System.Threading.Tasks;

namespace CanaryBuilder.Common
{
    public interface IConsoleProcess
    {
        CommandLine CommandLine { get; }
        Task<int> CompletedAsync();
        Task<int> CompletedAsync(CancellationToken token);
    }
}
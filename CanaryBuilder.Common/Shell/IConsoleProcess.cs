using System.Threading.Tasks;

namespace CanaryBuilder.Common.Shell
{
    public interface IConsoleProcess
    {
        CommandLine CommandLine { get; }
        IOutputPipe StdOut { get; }
        IOutputPipe StdErr { get; }
        void Kill();

        Task<int> Completed { get; }
    }
}
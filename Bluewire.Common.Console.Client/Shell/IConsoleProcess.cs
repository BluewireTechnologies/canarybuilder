using System.Threading.Tasks;

namespace Bluewire.Common.Console.Client.Shell
{
    public interface IConsoleProcess
    {
        ICommandLine CommandLine { get; }
        IOutputPipe StdOut { get; }
        IOutputPipe StdErr { get; }
        void Kill();

        Task<int> Completed { get; }
    }
}
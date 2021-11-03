using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Bluewire.Stash.Tool
{
    public interface IApplication
    {
        string GetCurrentDirectory();
        string GetTemporaryDirectory();
        string GetUserDataDirectory();
        string? GetEnvironmentVariable(string name);
        Task ShowDiagnostics(TextWriter stdout, DiagnosticsArguments model, CancellationToken token);
        Task Authenticate(TextWriter stdout, AuthenticateArguments model, CancellationToken token);
        Task Commit(TextWriter stderr, CommitArguments model, CancellationToken token);
        Task Checkout(TextWriter stderr, CheckoutArguments model, CancellationToken token);
        Task List(TextWriter stdout, TextWriter stderr, ListArguments model, CancellationToken token);
        Task Show(TextWriter stdout, TextWriter stderr, ShowArguments model, CancellationToken token);
        Task Delete(TextWriter stderr, DeleteArguments model, CancellationToken token);
        Task GarbageCollect(TextWriter stderr, GCArguments model, CancellationToken token);
        Task Push(TextWriter stderr, PushArguments model, CancellationToken token);
        Task Pull(TextWriter stderr, PullArguments model, CancellationToken token);
    }
}

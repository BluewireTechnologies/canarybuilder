using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace Bluewire.Stash.Tool
{
    public interface IAuthenticationProvider
    {
        Task<AuthenticationResult> Authenticate(CancellationToken token);
        Task Clear();
    }
}

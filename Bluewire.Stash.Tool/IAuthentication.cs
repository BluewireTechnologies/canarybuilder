using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace Bluewire.Stash.Tool
{
    public interface IAuthentication
    {
        Task<IAuthenticationProvider> Create();
        Task<AuthenticationResult?> Test(TextWriter stdout, CancellationToken token);
    }
}

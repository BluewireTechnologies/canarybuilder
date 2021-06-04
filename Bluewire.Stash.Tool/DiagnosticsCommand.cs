using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace Bluewire.Stash.Tool
{
    public class DiagnosticsCommand
    {
        internal LocalFileSystem LocalFileSystem { get; set; } = new LocalFileSystem();
        internal CommandServicesHelper CommandServicesHelper { get; set; } = new CommandServicesHelper();

        public async Task Execute(TextWriter stdout, DiagnosticsArguments model, CancellationToken token)
        {
            stdout.WriteLine($"Git topology:       {model.AppEnvironment.GitTopologyPath}");
            stdout.WriteLine($"Stash root:         {model.AppEnvironment.StashRoot}");
            stdout.WriteLine($"Remote stash root:  {model.AppEnvironment.RemoteStashRoot}");

            if (model.AppEnvironment.RemoteStashRoot.Value != null)
            {
                await TestRemote(stdout, model, token);
            }
        }

        private async Task TestRemote(TextWriter stdout, DiagnosticsArguments model, CancellationToken token)
        {
            var auth = await AuthenticationProvider.Create();
            var accounts = await auth.ListCachedAccounts();
            if (!accounts.Any())
            {
                stdout.WriteLine("No cached accounts.");
                return;
            }

            stdout.WriteLine("Cached accounts:");
            foreach (var account in accounts)
            {
                stdout.WriteLine($" * {account}");
            }

            var authResult = await TryAuthenticate(stdout, auth, token);
            if (authResult == null)
            {
                stdout.WriteLine("Cached credentials are no longer valid. Please use the 'authenticate' command to renew them.");
                return;
            }
            stdout.WriteLine($"Trying to access remote using account {authResult.Account.Username}...");
            await TestRemote(stdout, model, authResult, token);
        }

        private async Task TestRemote(TextWriter stdout, DiagnosticsArguments model, AuthenticationResult authResult, CancellationToken token)
        {
            try
            {
                var content = await HttpRemoteStashRepository.Ping(model.AppEnvironment.RemoteStashRoot.Value!, authResult, token);
                stdout.WriteLine("Ping successful.");
                stdout.WriteLine(content);
            }
            catch
            {
                stdout.WriteLine("Ping failed.");
                throw;
            }

            if (model.RemoteStashName.Value == null) return;

            stdout.WriteLine($"Try to list remote stash {model.RemoteStashName.Value}...");
            var remote = new HttpRemoteStashRepository(model.AppEnvironment.RemoteStashRoot.Value!, model.RemoteStashName.Value, authResult);

            var count = 0;
            await foreach (var entry in remote.List(token))
            {
                count++;
            }
            stdout.WriteLine($"Remote stash {model.RemoteStashName.Value} reported {count} entries.");
        }

        private async Task<AuthenticationResult?> TryAuthenticate(TextWriter stdout, AuthenticationProvider auth, CancellationToken token)
        {
            try
            {
                return await auth.AuthenticateCached(token);
            }
            catch (MsalUiRequiredException)
            {
                return null;
            }
        }
    }
}

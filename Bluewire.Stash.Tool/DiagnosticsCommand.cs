using System.IO;
using System.Threading;
using System.Threading.Tasks;

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
            var authResult = await model.AppEnvironment.Authentication.Test(stdout, token);
            if (authResult == null) return;
            if (authResult.Account == null)
            {
                stdout.WriteLine("Trying to access remote as confidential client...");
            }
            else
            {
                stdout.WriteLine($"Trying to access remote using account {authResult.Account.Username}...");
            }

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
    }
}

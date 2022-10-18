using Bluewire.Stash.Remote;
using System.Threading;
using System.Threading.Tasks;

namespace Bluewire.Stash.Tool
{
    public class RemoteDeleteCommand
    {
        internal CommandServicesHelper CommandServicesHelper { get; set; } = new CommandServicesHelper();

        public async Task Execute(RemoteDeleteArguments model, VerboseLogger logger, CancellationToken token)
        {
            logger.WriteLine(VerbosityLevels.ShowArguments, $"Remote stash root:  {model.AppEnvironment.RemoteStashRoot}");
            logger.WriteLine(VerbosityLevels.ShowArguments, $"Remote stash name:  {model.RemoteStashName}");
            logger.WriteLine(VerbosityLevels.ShowArguments, $"Version:            {model.Version}");

            var services = await SetUpServices(model, logger, token);

            logger.WriteLine(VerbosityLevels.DescribeActions, $"Deleting {services.Version}");

            await services.RemoteStashRepository.Delete(services.Version, token);

            logger.WriteLine(VerbosityLevels.DescribeActions, "Done");
        }

        private async Task<Services> SetUpServices(RemoteDeleteArguments model, VerboseLogger logger, CancellationToken token)
        {
            var services = new Services();

            services.RemoteStashRepository = await CommandServicesHelper.GetRemoteStashRepository(model.AppEnvironment.Authentication, model.AppEnvironment.RemoteStashRoot.Value, model.RemoteStashName.Value, token);

            services.Version = model.Version.Value;
            return services;
        }

        struct Services
        {
            public IRemoteStashRepository RemoteStashRepository { get; set; }
            public VersionMarker Version { get; set; }
        }
    }
}

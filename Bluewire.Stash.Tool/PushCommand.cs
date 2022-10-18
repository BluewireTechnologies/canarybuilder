using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Bluewire.Stash.Remote;

namespace Bluewire.Stash.Tool
{
    public class PushCommand
    {
        internal LocalFileSystem LocalFileSystem { get; set; } = new LocalFileSystem();
        internal CommandServicesHelper CommandServicesHelper { get; set; } = new CommandServicesHelper();

        public async Task Execute(PushArguments model, VerboseLogger logger, CancellationToken token)
        {
            logger.WriteLine(VerbosityLevels.ShowArguments, $"Stash root:         {model.AppEnvironment.StashRoot}");
            logger.WriteLine(VerbosityLevels.ShowArguments, $"Remote stash root:  {model.AppEnvironment.RemoteStashRoot}");
            logger.WriteLine(VerbosityLevels.ShowArguments, $"Stash name:         {model.StashName}");
            logger.WriteLine(VerbosityLevels.ShowArguments, $"Remote stash name:  {model.RemoteStashName}");
            logger.WriteLine(VerbosityLevels.ShowArguments, $"Version:            {model.Version}");
            logger.WriteLine(VerbosityLevels.ShowArguments, $"If exists remotely: {model.ExistsRemotelyBehaviour}");

            var services = await SetUpServices(model, logger, token);
            using var gc = new GarbageCollection(logger).RunBackground(services.StashRepository);

            logger.WriteLine(VerbosityLevels.DescribeActions, $"Pushing {services.Version} to {model.RemoteStashName.Value}");

            using (var stash = await services.StashRepository.TryGet(services.Version))
            {
                if (stash == null)
                {
                    logger.WriteLine(VerbosityLevels.DescribeActions, "No matching stash found");
                    throw new ApplicationException($"No matching stash was found for {services.Version}");
                }

                logger.WriteLine(VerbosityLevels.DescribeActions, $"Found matching stash: {stash.VersionMarker}");

                var txId = Guid.NewGuid();

                var existsOnRemote = await services.RemoteStashRepository.Exists(stash.VersionMarker, token);
                if (existsOnRemote)
                {
                    logger.WriteLine(VerbosityLevels.DescribeActions, $"The stash {stash.VersionMarker} already exists on the remote.");
                    if (model.ExistsRemotelyBehaviour.Value == ExistsBehaviour.Error) throw new ApplicationException($"The stash {services.Version} already exists on the remote.");
                    if (model.ExistsRemotelyBehaviour.Value == ExistsBehaviour.Ignore) return;
                }

                await foreach (var relativePath in stash.List(token))
                {
                    using (var sourceStream = await stash.Get(relativePath, token))
                    {
                        if (sourceStream == null) throw new ApplicationException($"Unable to fetch {relativePath}. The stream was unavailable.");
                        logger.WriteLine(VerbosityLevels.DescribeActions, $"Uploading {relativePath}");
                        await services.RemoteStashRepository.Push(txId, relativePath, sourceStream, token);
                    }
                }
                if (existsOnRemote)
                {
                    logger.WriteLine(VerbosityLevels.DescribeActions, "Removing existing stash on the remote");
                    await services.RemoteStashRepository.Delete(stash.VersionMarker, token);
                }
                logger.WriteLine(VerbosityLevels.DescribeActions, "Committing transaction");
                await services.RemoteStashRepository.Commit(stash.VersionMarker, txId, token);
                logger.WriteLine(VerbosityLevels.DescribeActions, "Done");
            }
        }

        private async Task<Services> SetUpServices(PushArguments model, VerboseLogger logger, CancellationToken token)
        {
            var services = new Services();

            var repositoryPath = Path.Combine(model.AppEnvironment.StashRoot.Value, model.StashName.Value);
            services.StashRepository = new LocalStashRepository(repositoryPath);
            services.RemoteStashRepository = await CommandServicesHelper.GetRemoteStashRepository(model.AppEnvironment.Authentication, model.AppEnvironment.RemoteStashRoot.Value, model.RemoteStashName.Value, token);
            services.Version = model.Version.Value;
            return services;
        }

        struct Services
        {
            public ILocalStashRepository StashRepository { get; set; }
            public IRemoteStashRepository RemoteStashRepository { get; set; }
            public VersionMarker Version { get; set; }
        }
    }
}

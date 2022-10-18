using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper;
using Bluewire.Common.GitWrapper.Model;
using Bluewire.Stash.Remote;

namespace Bluewire.Stash.Tool
{
    public class PullCommand
    {
        internal LocalFileSystem LocalFileSystem { get; set; } = new LocalFileSystem();
        internal CommandServicesHelper CommandServicesHelper { get; set; } = new CommandServicesHelper();

        public async Task Execute(PullArguments model, VerboseLogger logger, CancellationToken token)
        {
            logger.WriteLine(VerbosityLevels.ShowArguments, $"Git topology:       {model.AppEnvironment.GitTopologyPath}");
            logger.WriteLine(VerbosityLevels.ShowArguments, $"Stash root:         {model.AppEnvironment.StashRoot}");
            logger.WriteLine(VerbosityLevels.ShowArguments, $"Remote stash root:  {model.AppEnvironment.RemoteStashRoot}");
            logger.WriteLine(VerbosityLevels.ShowArguments, $"Stash name:         {model.StashName}");
            logger.WriteLine(VerbosityLevels.ShowArguments, $"Remote stash name:  {model.RemoteStashName}");
            logger.WriteLine(VerbosityLevels.ShowArguments, $"Version:            {model.Version}");
            logger.WriteLine(VerbosityLevels.ShowArguments, $"If exists locally:  {model.ExistsLocallyBehaviour}");

            var services = await SetUpServices(model, logger, token);
            using var gc = new GarbageCollection(logger).RunBackground(services.StashRepository);

            var sourceMarker = await GetSourceVersionMarker(services);

            logger.WriteLine(VerbosityLevels.DescribeActions, $"Pulling {sourceMarker} from {model.RemoteStashName.Value}");

            var cached =  await CachedStashRepository.Preload(services.RemoteStashRepository, token);

            var stashVersionMarker = await FindSourceStash(services, cached, sourceMarker);
            if (stashVersionMarker == null)
            {
                throw new ApplicationException($"No matching stash was found for {sourceMarker} on the remote");
            }

            logger.WriteLine(VerbosityLevels.DescribeActions, $"Found matching stash: {stashVersionMarker}");

            var existsLocally = await services.StashRepository.TryGet(stashVersionMarker.Value) != null;
            if (existsLocally)
            {
                logger.WriteLine(VerbosityLevels.DescribeActions, $"The stash {stashVersionMarker.Value} already exists locally.");
                if (model.ExistsLocallyBehaviour.Value == ExistsBehaviour.Error) throw new ApplicationException($"The stash {stashVersionMarker.Value} already exists locally.");
                if (model.ExistsLocallyBehaviour.Value == ExistsBehaviour.Ignore) return;
                await services.StashRepository.Delete(stashVersionMarker.Value);
            }

            var localStash = await services.StashRepository.GetOrCreate(stashVersionMarker.Value);

            await foreach (var relativePath in services.RemoteStashRepository.ListFiles(stashVersionMarker.Value, token))
            {
                using (var sourceStream = await services.RemoteStashRepository.Pull(stashVersionMarker.Value, relativePath, token))
                {
                    if (sourceStream == null) throw new ApplicationException($"Unable to fetch {relativePath}. The stream was unavailable.");
                    logger.WriteLine(VerbosityLevels.DescribeActions, $"Downloading {relativePath}");
                    await localStash.Store(sourceStream, relativePath, token);
                }
                await localStash.Commit(token);
            }
            logger.WriteLine(VerbosityLevels.DescribeActions, "Done");
        }

        private async Task<Services> SetUpServices(PullArguments model, VerboseLogger logger, CancellationToken token)
        {
            var services = new Services();

            var repositoryPath = Path.Combine(model.AppEnvironment.StashRoot.Value, model.StashName.Value);
            services.StashRepository = new LocalStashRepository(repositoryPath);
            services.RemoteStashRepository = await CommandServicesHelper.GetRemoteStashRepository(model.AppEnvironment.Authentication, model.AppEnvironment.RemoteStashRoot.Value, model.RemoteStashName.Value, token);

            services.GitSession = await CommandServicesHelper.PrepareGitSession(logger);
            services.GitFilesystemContext = await CommandServicesHelper.TryFindGitFilesystemContext(services.GitSession, model.AppEnvironment.GitTopologyPath, logger);
            services.Version = model.Version.Value ?? await CommandServicesHelper.GetVersionMarkerFromGitWorkingCopy(services.GitSession, services.GitFilesystemContext);
            services.CommitTopology = await CommandServicesHelper.GetCommitTopology(services.GitSession, services.GitFilesystemContext);
            return services;
        }

        private async Task<VersionMarker> GetSourceVersionMarker(Services services)
        {
            if (services.CommitTopology != null)
            {
                var resolved = await services.CommitTopology.FullyResolve(services.Version);
                if (resolved != null) return resolved.Value;
            }
            return services.Version;
        }

        private async Task<VersionMarker?> FindSourceStash(Services services, CachedStashRepository cached, VersionMarker marker)
        {
            if (services.CommitTopology != null)
            {
                return  await cached.FindClosestAncestor(services.CommitTopology, marker);
            }
            return await cached.FindClosestAncestor(marker);
        }

        struct Services
        {
            public ILocalStashRepository StashRepository { get; set; }
            public IRemoteStashRepository RemoteStashRepository { get; set; }
            public VersionMarker Version { get; set; }
            public GitSession? GitSession { get; set; }
            public IGitFilesystemContext? GitFilesystemContext { get; set; }
            public ICommitTopology? CommitTopology { get; set; }
        }
    }
}

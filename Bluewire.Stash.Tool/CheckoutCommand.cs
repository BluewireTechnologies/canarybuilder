using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper;
using Bluewire.Common.GitWrapper.Model;

namespace Bluewire.Stash.Tool
{
    public class CheckoutCommand
    {
        internal LocalFileSystem LocalFileSystem { get; set; } = new LocalFileSystem();
        internal CommandServicesHelper CommandServicesHelper { get; set; } = new CommandServicesHelper();

        public async Task Execute(CheckoutArguments model, VerboseLogger logger, CancellationToken token)
        {
            logger.WriteLine(VerbosityLevels.ShowArguments, $"Git topology:       {model.AppEnvironment.GitTopologyPath}");
            logger.WriteLine(VerbosityLevels.ShowArguments, $"Stash root:         {model.AppEnvironment.StashRoot}");
            logger.WriteLine(VerbosityLevels.ShowArguments, $"Stash name:         {model.StashName}");
            logger.WriteLine(VerbosityLevels.ShowArguments, $"Destination path:   {model.DestinationPath}");
            logger.WriteLine(VerbosityLevels.ShowArguments, $"Version:            {model.Version}");

            if (LocalFileSystem.DirectoryExists(model.DestinationPath.Value))
            {
                await foreach (var _ in LocalFileSystem.EnumerateRelativePaths(model.DestinationPath.Value, true).WithCancellation(token))
                {
                    throw new Exception($"Directory exists and is not empty: {model.DestinationPath.Value}");
                }
            }

            var services = await SetUpServices(model, logger);
            using var gc = new GarbageCollection(logger).RunBackground(services.StashRepository);

            var sourceMarker = await GetSourceVersionMarker(services);

            logger.WriteLine(VerbosityLevels.DescribeActions, $"Will checkout {sourceMarker} to {model.DestinationPath}");

            using (var stash = await FindSourceStash(services, sourceMarker))
            {
                if (stash == null)
                {
                    logger.WriteLine(VerbosityLevels.DescribeActions, "No matching stash found");
                    throw new ApplicationException($"No matching stash was found for {sourceMarker}");
                }

                logger.WriteLine(VerbosityLevels.DescribeActions, $"Found matching stash: {stash.VersionMarker}");

                await foreach (var relativePath in stash.List(token))
                {
                    var absolutePath = Path.Combine(model.DestinationPath.Value, relativePath);
                    using (var destinationStream = LocalFileSystem.CreateForExclusiveWrite(absolutePath))
                    {
                        logger.WriteLine(VerbosityLevels.DescribeActions, $"Fetching {relativePath}");
                        using (var sourceStream = await stash.Get(relativePath, token))
                        {
                            if (sourceStream == null) throw new ApplicationException($"Unable to fetch {relativePath}. The stream was unavailable.");
                            await sourceStream.CopyToAsync(destinationStream);
                        }
                    }
                }
                logger.WriteLine(VerbosityLevels.DescribeActions, "Done");
            }
        }

        private async Task<Services> SetUpServices(CheckoutArguments model, VerboseLogger logger)
        {
            var services = new Services();

            var repositoryPath = Path.Combine(model.AppEnvironment.StashRoot.Value, model.StashName.Value);
            services.StashRepository = new LocalStashRepository(repositoryPath);

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

        private async Task<IStash?> FindSourceStash(Services services, VersionMarker marker)
        {
            if (services.CommitTopology != null)
            {
                return  await services.StashRepository.FindClosestAncestor(services.CommitTopology, marker);
            }
            return  await services.StashRepository.FindClosestAncestor(marker);
        }

        struct Services
        {
            public ILocalStashRepository StashRepository { get; set; }
            public VersionMarker Version { get; set; }
            public GitSession? GitSession { get; set; }
            public IGitFilesystemContext? GitFilesystemContext { get; set; }
            public ICommitTopology? CommitTopology { get; set; }
        }
    }
}

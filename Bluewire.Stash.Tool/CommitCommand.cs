using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper;
using Bluewire.Common.GitWrapper.Model;

namespace Bluewire.Stash.Tool
{
    public class CommitCommand
    {
        internal LocalFileSystem LocalFileSystem { get; set; } = new LocalFileSystem();
        internal CommandServicesHelper CommandServicesHelper { get; set; } = new CommandServicesHelper();

        public async Task Execute(CommitArguments model, VerboseLogger logger, CancellationToken token)
        {
            logger.WriteLine(VerbosityLevels.ShowArguments, $"Git topology:       {model.AppEnvironment.GitTopologyPath}");
            logger.WriteLine(VerbosityLevels.ShowArguments, $"Stash root:         {model.AppEnvironment.StashRoot}");
            logger.WriteLine(VerbosityLevels.ShowArguments, $"Stash name:         {model.StashName}");
            logger.WriteLine(VerbosityLevels.ShowArguments, $"Source path:        {model.SourcePath}");
            logger.WriteLine(VerbosityLevels.ShowArguments, $"Version:            {model.Version}");
            logger.WriteLine(VerbosityLevels.ShowArguments, $"Force:              {model.Force}");

            if (!LocalFileSystem.DirectoryExists(model.SourcePath.Value)) throw new Exception($"Directory not found: {model.SourcePath.Value}");

            var services = await SetUpServices(model, logger);
            using var gc = new GarbageCollection(logger).RunBackground(services.StashRepository);

            var targetMarker = await GetTargetVersionMarker(services);

            logger.WriteLine(VerbosityLevels.DescribeActions, $"Will commit {model.SourcePath} to {targetMarker}");

            if (model.Force.Value)
            {
                logger.WriteLine(VerbosityLevels.DescribeActions, $"Deleting existing stash for {targetMarker}");
                await services.StashRepository.Delete(targetMarker);
            }

            using (var stash = await services.StashRepository.GetOrCreateExact(targetMarker))
            {
                await foreach (var relativePath in LocalFileSystem.EnumerateRelativePaths(model.SourcePath.Value).WithCancellation(token))
                {
                    var absolutePath = Path.Combine(model.SourcePath.Value, relativePath);
                    using (var stream = LocalFileSystem.OpenForRead(absolutePath))
                    {
                        logger.WriteLine(VerbosityLevels.DescribeActions, $"Adding {relativePath}");
                        await stash.Store(stream, relativePath, token);
                    }
                }
                logger.WriteLine(VerbosityLevels.DescribeActions, "Committing");
                await stash.Commit();
                logger.WriteLine(VerbosityLevels.DescribeActions, "Done");
            }
        }

        private async Task<Services> SetUpServices(CommitArguments model, VerboseLogger logger)
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

        private async Task<VersionMarker> GetTargetVersionMarker(Services services)
        {
            if (services.CommitTopology != null)
            {
                var resolved = await services.CommitTopology.FullyResolve(services.Version);
                if (resolved != null) return resolved.Value;
            }
            return services.Version;
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

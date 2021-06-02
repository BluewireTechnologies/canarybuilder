using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper;
using Bluewire.Common.GitWrapper.Model;
using Bluewire.Conventions;

namespace Bluewire.Stash.Tool
{
    public class GCCommand
    {
        internal LocalFileSystem LocalFileSystem { get; set; } = new LocalFileSystem();
        internal CommandServicesHelper CommandServicesHelper { get; set; } = new CommandServicesHelper();

        public async Task Execute(GCArguments model, VerboseLogger logger, CancellationToken token)
        {
            logger.WriteLine(VerbosityLevels.ShowArguments, $"Git topology:       {model.AppEnvironment.GitTopologyPath}");
            logger.WriteLine(VerbosityLevels.ShowArguments, $"Stash root:         {model.AppEnvironment.StashRoot}");
            logger.WriteLine(VerbosityLevels.ShowArguments, $"Stash name:         {model.StashName}");

            var services = await SetUpServices(model, logger);
            var tempObjectCount = 0;
            await foreach (var cleaned in services.StashRepository.CleanUpTemporaryObjects(token))
            {
                logger.WriteLine(VerbosityLevels.DescribeActions, $"Cleaned {cleaned}");
                tempObjectCount++;
            }
            logger.WriteLine(VerbosityLevels.DescribeActions, $"Cleaned {tempObjectCount} temporary objects");

            if (services.CommitTopology != null && services.GitSession != null)
            {
                var stashEntryCount = 0;
                await foreach (var entry in services.StashRepository.List().WithCancellation(token))
                {
                    if (await ShouldCleanVersion(services, entry, model.Aggressive.Value))
                    {
                        try
                        {
                            await services.StashRepository.Delete(entry);
                            logger.WriteLine(VerbosityLevels.DescribeActions, $"Cleaned {VersionMarkerStringConverter.ForIdentifierRoundtrip().ToString(entry)}");
                            stashEntryCount++;
                        }
                        catch (Exception ex)
                        {
                            logger.WriteLine(VerbosityLevels.Default, $"Unable to clean {VersionMarkerStringConverter.ForIdentifierRoundtrip().ToString(entry)}: {ex.Message}");
                        }
                    }
                }
                logger.WriteLine(VerbosityLevels.DescribeActions, $"Cleaned {stashEntryCount} stash entries");
            }

            logger.WriteLine(VerbosityLevels.DescribeActions, "Done");
        }

        private async Task<bool> ShouldCleanVersion(Services services, VersionMarker entry, bool aggressive)
        {
            if (services.GitSession == null) return false;
            if (services.CommitTopology == null) return false;

            if (entry.SemanticVersion != null && new VersionSemantics().IsCanonicalVersion(entry.SemanticVersion))
            {
                // If aggressive GC has not been requested, keep all entries with canonical versions.
                if (!aggressive) return false;
            }
            if (entry.IsComplete)
            {
                // Complete entry has version and commit.
                // If the commit still exists, keep this stash entry. Otherwise get rid of it.
                return !await services.GitSession.RefExists(services.GitFilesystemContext, new Ref(entry.CommitHash));
            }
            var resolved = await services.CommitTopology.FullyResolve(entry);
            // If the commit cannot be resolved, it doesn't exist. Get rid of the stash entry.
            return resolved == null;
        }

        private async Task<Services> SetUpServices(GCArguments model, VerboseLogger logger)
        {
            var services = new Services();

            var repositoryPath = Path.Combine(model.AppEnvironment.StashRoot.Value, model.StashName.Value);
            services.StashRepository = new LocalStashRepository(repositoryPath);

            services.GitSession = await CommandServicesHelper.PrepareGitSession(logger);
            services.GitFilesystemContext = await CommandServicesHelper.TryFindGitFilesystemContext(services.GitSession, model.AppEnvironment.GitTopologyPath, logger);
            services.CommitTopology = await CommandServicesHelper.GetCommitTopology(services.GitSession, services.GitFilesystemContext);
            return services;
        }

        struct Services
        {
            public ILocalStashRepository StashRepository { get; set; }
            public GitSession? GitSession { get; set; }
            public IGitFilesystemContext? GitFilesystemContext { get; set; }
            public ICommitTopology? CommitTopology { get; set; }
        }
    }
}

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper;
using Bluewire.Common.GitWrapper.Model;
using Bluewire.Conventions;

namespace Bluewire.Stash.Tool
{
    public class ShowCommand
    {
        internal LocalFileSystem LocalFileSystem { get; set; } = new LocalFileSystem();
        internal CommandServicesHelper CommandServicesHelper { get; set; } = new CommandServicesHelper();

        public async Task Execute(ShowArguments model, TextWriter output, VerboseLogger logger, CancellationToken token)
        {
            logger.WriteLine(VerbosityLevels.ShowArguments, $"Git topology:       {model.AppEnvironment.GitTopologyPath}");
            logger.WriteLine(VerbosityLevels.ShowArguments, $"Stash root:         {model.AppEnvironment.StashRoot}");
            logger.WriteLine(VerbosityLevels.ShowArguments, $"Stash name:         {model.StashName}");
            logger.WriteLine(VerbosityLevels.ShowArguments, $"Version:            {model.Version}");

            var services = await SetUpServices(model, logger);

            var sourceMarker = await TryResolveVersionMarker(services, services.Version);

            logger.WriteLine(VerbosityLevels.DescribeActions, $"Finding preferred stash entry for {sourceMarker}");

            using (var stash = await FindSourceStash(services, sourceMarker))
            {
                if (stash == null)
                {
                    logger.WriteLine(VerbosityLevels.Default, "No matching stash found");
                    return;
                }

                if (model.ExactMatch.Value)
                {
                    var resolved = await TryResolveVersionMarker(services, stash.VersionMarker);
                    if (!IsExactMatch(sourceMarker, resolved))
                    {
                        logger.WriteLine(VerbosityLevels.DescribeActions, $"Found a match, but it wasn't exact: {stash.VersionMarker}");
                        return;
                    }
                }

                logger.WriteLine(VerbosityLevels.DescribeActions, $"Found matching stash: {stash.VersionMarker}");
                output.WriteLine(VersionMarkerStringConverter.ForIdentifierRoundtrip().ToString(stash.VersionMarker));
            }
        }

        private bool IsExactMatch(VersionMarker requested, VersionMarker resolved)
        {
            if (requested.CommitHash != null && resolved.CommitHash != null)
            {
                // If hashes are known for both, require that they match exactly.
                return StringComparer.OrdinalIgnoreCase.Equals(requested.CommitHash, resolved.CommitHash);
            }
            if (requested.SemanticVersion != null && resolved.SemanticVersion != null)
            {
                // If version numbers are known for both and identical, treat as an exact match.
                if (SemanticVersion.EqualityComparer.Equals(requested.SemanticVersion, resolved.SemanticVersion)) return true;
            }
            // Otherwise, we're unable to tell.
            return false;
        }

        private async Task<Services> SetUpServices(ShowArguments model, VerboseLogger logger)
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

        private async Task<VersionMarker> TryResolveVersionMarker(Services services, VersionMarker version)
        {
            if (services.CommitTopology != null)
            {
                var resolved = await services.CommitTopology.FullyResolve(version);
                if (resolved != null) return resolved.Value;
            }
            return version;
        }

        private async Task<IStash?> FindSourceStash(Services services, VersionMarker marker)
        {
            if (services.CommitTopology != null)
            {
                return await services.StashRepository.FindClosestAncestor(services.CommitTopology, marker);
            }
            return await services.StashRepository.FindClosestAncestor(marker);
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

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Bluewire.Stash.Tool
{
    public class DeleteCommand
    {
        internal LocalFileSystem LocalFileSystem { get; set; } = new LocalFileSystem();

        public async Task Execute(DeleteArguments model, VerboseLogger logger, CancellationToken token)
        {
            logger.WriteLine(VerbosityLevels.ShowArguments, $"Git topology:       {model.AppEnvironment.GitTopologyPath}");
            logger.WriteLine(VerbosityLevels.ShowArguments, $"Stash root:         {model.AppEnvironment.StashRoot}");
            logger.WriteLine(VerbosityLevels.ShowArguments, $"Stash name:         {model.StashName}");
            logger.WriteLine(VerbosityLevels.ShowArguments, $"Version:            {model.Version}");

            var services = await SetUpServices(model, logger);
            using var gc = new GarbageCollection(logger).RunBackground(services.StashRepository);

            logger.WriteLine(VerbosityLevels.DescribeActions, $"Deleting {services.Version}");

            await services.StashRepository.Delete(services.Version);

            logger.WriteLine(VerbosityLevels.DescribeActions, "Done");
        }

        private async Task<Services> SetUpServices(DeleteArguments model, VerboseLogger logger)
        {
            var services = new Services();

            var repositoryPath = Path.Combine(model.AppEnvironment.StashRoot.Value, model.StashName.Value);
            services.StashRepository = new LocalStashRepository(repositoryPath);
            services.Version = model.Version.Value;
            return services;
        }

        struct Services
        {
            public ILocalStashRepository StashRepository { get; set; }
            public VersionMarker Version { get; set; }
        }
    }
}

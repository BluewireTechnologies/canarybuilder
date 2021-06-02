using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Bluewire.Stash.Tool
{
    public class ListCommand
    {
        internal LocalFileSystem LocalFileSystem { get; set; } = new LocalFileSystem();

        public async Task Execute(ListArguments model, TextWriter output, VerboseLogger logger, CancellationToken token)
        {
            logger.WriteLine(VerbosityLevels.ShowArguments, $"Git topology:       {model.AppEnvironment.GitTopologyPath}");
            logger.WriteLine(VerbosityLevels.ShowArguments, $"Stash root:         {model.AppEnvironment.StashRoot}");
            logger.WriteLine(VerbosityLevels.ShowArguments, $"Stash name:         {model.StashName}");

            var services = await SetUpServices(model, logger);

            await foreach (var entry in services.StashRepository.List().WithCancellation(token))
            {
                output.WriteLine(VersionMarkerStringConverter.ForIdentifierRoundtrip().ToString(entry));
            }
        }

        private async Task<Services> SetUpServices(ListArguments model, VerboseLogger logger)
        {
            var services = new Services();

            var repositoryPath = Path.Combine(model.AppEnvironment.StashRoot.Value, model.StashName.Value);
            services.StashRepository = new LocalStashRepository(repositoryPath);

            return services;
        }

        struct Services
        {
            public ILocalStashRepository StashRepository { get; set; }
        }
    }
}

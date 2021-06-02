using System;
using System.Threading;
using System.Threading.Tasks;

namespace Bluewire.Stash.Tool
{
    public class GarbageCollection
    {
        private readonly VerboseLogger logger;

        public GarbageCollection(VerboseLogger logger)
        {
            this.logger = logger;
        }

        public IDisposable RunBackground(ILocalStashRepository repository)
        {
            var job = new GCJob(repository, logger);
            job.Start();
            return job;
        }

        class GCJob : IDisposable
        {
            private readonly ILocalStashRepository repository;
            private readonly VerboseLogger logger;
            private Task? task;

            public GCJob(ILocalStashRepository repository, VerboseLogger logger)
            {
                this.repository = repository;
                this.logger = logger;
            }

            public Task Start()
            {
                task ??= RunJob();
                return task;
            }

            private async Task RunJob()
            {
                var count = 0;
                await foreach (var cleaned in repository.CleanUpTemporaryObjects(cts.Token))
                {
                    count++;
                }
                logger.WriteLine(VerbosityLevels.ShowBackgroundJobs, $"Cleaned {count} temporary objects");
            }

            private readonly CancellationTokenSource cts = new CancellationTokenSource();

            public void Dispose()
            {
                cts.Cancel();

            }
        }
    }
}

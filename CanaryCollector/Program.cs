using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bluewire.Common.Console;
using Bluewire.Common.Console.Logging;
using Bluewire.Common.Console.ThirdParty;
using CanaryCollector.Model;
using log4net.Core;

namespace CanaryCollector
{
    class Program
    {
        static int Main(string[] args)
        {
            var arguments = new Arguments();
            var options = new OptionSet
            {
                { "pending", "Include tickets pending review.", o => arguments.IncludePending = true },
                { "tag=", "Include tickets with the specified tag.", o => arguments.IncludeTags.Add(o) },
                { "url=", "Include branches listed by the specified resource. Currently only Google Docs spreadsheets are supported.", (Uri o) => arguments.IncludeUris.Add(o) },
                { "youtrack=", "Use the specified Youtrack URL as the source of tickets.", (Uri o) => arguments.YoutrackUri = o },
                { "repo=|repository=", "Use the specified repository as the source of branches.", o => arguments.RepositoryPath = o }
            };

            var session = new ConsoleSession<Arguments>(arguments, options);

            return session.Run(args, async a => await new Program(a).Run());
        }

        private readonly Arguments arguments;

        private Program(Arguments arguments)
        {
            this.arguments = arguments;
            Log.Configure();
            Log.SetConsoleVerbosity(arguments.Verbosity);
        }

        private async Task<int> Run()
        {
            var factory = new BranchCollectorFactory(arguments.YoutrackUri, arguments.RepositoryPath);

            var collectors = new List<IBranchCollector>();
            collectors.AddRange(factory.CreateUriCollectors(arguments.IncludeUris));
            collectors.AddRange(factory.CreateTagCollectors(arguments.IncludeTags));
            if (arguments.IncludePending) collectors.Add(await factory.CreatePendingCollector());
            if (!collectors.Any()) throw new InvalidArgumentsException("Nothing to do. Please specify --pending, --tag or --uri.");

            var branches = new List<Branch>();
            foreach (var collector in collectors)
            {
                branches.AddRange(await collector.CollectBranches());
            }
            
            foreach (var branch in DeduplicateBranchesByName(branches))
            {
                Console.Out.WriteLine(branch.Name);
            }
            
            return 0;
        }

        private IEnumerable<Branch> DeduplicateBranchesByName(IEnumerable<Branch> branches)
        {
            var seen = new HashSet<string>();
            foreach (var branch in branches)
            {
                if (seen.Add(branch.Name))
                {
                    yield return branch;
                }
            }
        }

        class Arguments : IVerbosityArgument
        {
            private readonly IEnumerable<Level> logLevels = new List<Level> { Level.Warn, Level.Info, Level.Debug };
            private int logLevel = 1;

            public Level Verbosity => logLevels.ElementAtOrDefault(logLevel) ?? Level.All;

            public void Verbose()
            {
                logLevel++;
            }

            public ICollection<string> IncludeTags { get; } = new HashSet<string>();
            public ICollection<Uri> IncludeUris { get; } = new HashSet<Uri>();
            public bool IncludePending { get; set; }

            public Uri YoutrackUri { get; set; }
            public string RepositoryPath { get; set; }
        }
    }
}

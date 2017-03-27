using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Bluewire.Common.Console;
using Bluewire.Common.Console.Logging;
using Bluewire.Common.Console.ThirdParty;
using Bluewire.Common.GitWrapper;
using log4net.Core;
using Bluewire.Common.GitWrapper.Model;
using Bluewire.Conventions;
using Bluewire.Tools.Runner.Shared;

namespace Bluewire.Tools.Runner.FindTickets
{
    public class ToolRunner : IToolRunner
    {
        public string Name => "find-tickets";

        public void Describe(TextWriter writer)
        {
            writer.WriteLine($"  {Name}: Find the tickets mentioned between two semantic versions (release, build number and optional semantic tag). Supply only one semantic version to produce a list of tickets to the head of the branch that build is found on.");
        }

        private static OptionSet CreateOptions(Arguments arguments)
        {
            return new OptionSet
            {
                {"s=|start-semver=", "Starting (lower) semantic version (<major>.<minor>.<build>[-semtag]). Omit the semtag if you want all semantic version tags to be searched for a match in this order: release, rc, beta.", o => arguments.StartSemanticVersion = o },
                { "e=|end-semver=", "Ending (higher) semantic version (<major>.<minor>.<build>[-semtag]). Omit the semtag if you want all semantic version tags to be searched for a match in this order: release, rc, beta.", o => arguments.EndSemanticVersion = o },
                {"repo=|repository=", "Specify the repository to use. Default: current directory.", o => arguments.WorkingCopyOrRepo = o}
            };
        }

        public int RunMain(string[] args, string parentArgs)
        {
            var arguments = new Arguments();
            var options = CreateOptions(arguments);
            var sessionArgs = new SessionArguments<Arguments>(arguments, options);
            sessionArgs.Application += parentArgs;
            var consoleSession = new ConsoleSession<Arguments>(sessionArgs);
            consoleSession.ListParameterUsage = "<from version> [to version]";
            return consoleSession.Run(args, async a => await new Impl(a).Run());
        }

        class Impl
        {
            private readonly Arguments arguments;

            public Impl(Arguments arguments)
            {
                this.arguments = arguments;

                Log.Configure();
                Log.SetConsoleVerbosity(arguments.Verbosity);
            }

            public async Task<int> Run()
            {
                TryInferArgumentsFromList(arguments);


                var git = await new GitFinder().FromEnvironment();
                var gitSession = new GitSession(git, new ConsoleInvocationLogger());
                var gitRepository = GetGitRepository(arguments.WorkingCopyOrRepo);

                var startCommitJob = new FindCommits.ResolveCommitFromSemanticVersion(arguments.StartSemanticVersion);
                var startBuilds = await startCommitJob.ResolveCommits(gitSession, gitRepository);
                if (startBuilds.Length > 1)
                {
                    Log.Console.Error("Ambiguous start build. Please supply a semantic tag.");
                    return 1;
                }
                if (startBuilds.Length == 0)
                {
                    Log.Console.Error("Could not find start build. Please check it's correct and your repo is up to date.");
                    return 2;
                }
                var startBuild = startBuilds[0];
                Build endBuild;

                if (!string.IsNullOrEmpty(arguments.EndSemanticVersion))
                {
                    var endCommitJob = new FindCommits.ResolveCommitFromSemanticVersion(arguments.EndSemanticVersion);
                    var endBuilds = await endCommitJob.ResolveCommits(gitSession, gitRepository);
                    if (endBuilds.Length > 1)
                    {
                        Log.Console.Error("Ambiguous end build. Please supply a semantic tag.");
                        return 3;
                    }
                    if (endBuilds.Length == 0)
                    {
                        Log.Console.Error("Could not find end build. Please check it's correct and your repo is up to date.");
                        return 4;
                    }
                    endBuild = endBuilds[0];
                }
                else
                {
                    var endCommit = await FindEndCommitFromStartBuild(gitSession, gitRepository, startBuild.SemanticVersion);
                    endBuild = new Build() { Commit = endCommit, SemanticVersion = null };
                }

                Log.Console.Debug($"Finding tickets between semantic version {startBuild.SemanticVersion} and {endBuild.SemanticVersion?.ToString() ?? endBuild.Commit}");
                var job = new ResolveTicketsBetweenRefs(startBuild.Commit, endBuild.Commit);
                var tickets = await job.ResolveTickets(gitSession, gitRepository);                
                RenderTickets(tickets);
                return 0;
            }

            private static void RenderTickets(string[] tickets)
            {
                foreach (var ticket in tickets)
                {
                    Console.WriteLine(ticket);
                }
            }

            private static void TryInferArgumentsFromList(Arguments arguments)
            {
                if (!arguments.ArgumentList.Any()) return;
                if (arguments.ArgumentList.Count > 2)
                {
                    Log.Console.Warn($"Excess arguments detected: {string.Join(" ", arguments.ArgumentList.Skip(1))}");
                    // Continue parsing the first two arguments.
                }

                arguments.StartSemanticVersion = arguments.ArgumentList.First().Trim();
                arguments.EndSemanticVersion = arguments.ArgumentList.ElementAtOrDefault(1)?.Trim();
            }
            
            private async Task<Ref> FindEndCommitFromStartBuild(GitSession session, Common.GitWrapper.GitRepository repository, SemanticVersion startSemanticVersion)
            {
                    var branchSemantics = new BranchSemantics();
                    var refName = branchSemantics.GetVersionLatestBranchName(startSemanticVersion);
                    if (refName == "master")
                    {
                        var maintTag = new Ref($"tags/maint/{startSemanticVersion.Major}.{startSemanticVersion.Minor}");
                        if (await session.TagExists(repository, maintTag))
                        {
                            return maintTag;
                        }
                    }
                    return RefHelper.GetRemoteRef(new Ref(refName));
            }
            
            private static Common.GitWrapper.GitRepository GetGitRepository(string path)
            {
                try
                {
                    return Common.GitWrapper.GitRepository.Find(path);
                }
                catch (Exception ex)
                {
                    throw new InvalidArgumentsException(ex);
                }
            }
        }

        public class Arguments : IVerbosityArgument, IArgumentList
        {
            private readonly IEnumerable<Level> logLevels = new List<Level> { Level.Warn, Level.Info, Level.Debug };
            private int logLevel = 1;

            public Level Verbosity => logLevels.ElementAtOrDefault(logLevel) ?? Level.All;

            public void Verbose()
            {
                logLevel++;
                LogInvocations = true;
            }

            public bool LogInvocations { get; private set; }

            public string StartSemanticVersion { get; set; }
            public string EndSemanticVersion { get; set; }

            public string WorkingCopyOrRepo { get; set; } = Environment.CurrentDirectory;
            public IList<string> ArgumentList { get; } = new List<string>();
        }
    }
}

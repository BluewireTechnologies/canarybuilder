using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Bluewire.Common.Console;
using Bluewire.Common.Console.Logging;
using Bluewire.Common.GitWrapper;
using log4net.Core;
using Bluewire.Common.GitWrapper.Model;
using Bluewire.Conventions;
using Bluewire.Tools.Builds.Shared;

namespace Bluewire.Tools.Runner.FindTickets
{
    public class ToolRunner : IToolRunner
    {
        public string Name => "find-tickets";

        public void Describe(TextWriter writer)
        {
            writer.WriteLine($"  {Name}: Find the tickets mentioned between two semantic versions (release, build number and optional semantic tag). Supply only one semantic version to produce a list of tickets to the head of the branch that build is found on.");
        }

        public int RunMain(string[] args, string parentArgs)
        {
            var tool = new Impl();
            var consoleSession = new ConsoleSession()
            {
                Options = {
                    { "s=|start-semver=", "Starting (lower) semantic version (<major>.<minor>.<build>[-semtag]). Omit the semtag if you want all semantic version tags to be searched for a match in this order: release, rc, beta.", o => tool.StartSemanticVersion = o },
                    { "e=|end-semver=", "Ending (higher) semantic version (<major>.<minor>.<build>[-semtag]). Omit the semtag if you want all semantic version tags to be searched for a match in this order: release, rc, beta.", o => tool.EndSemanticVersion = o },
                    { "repo=|repository=", "Specify the repository to use. Default: current directory.", o => tool.WorkingCopyOrRepo = o }
                },
                ListParameterUsage = "<from version> [to version]"
            };
            consoleSession.ArgumentList.AddRemainder("", tool.ArgumentList.Add);
            consoleSession.Application += parentArgs;
            var logger = consoleSession.Options.AddCollector(new SimpleConsoleLoggingPolicy { Verbosity = { Default = Level.Info } });

            return consoleSession.Run(args, async () => {
                using (LoggingPolicy.Register(consoleSession, logger))
                {
                    return await tool.Run();
                }
            });
        }

        class Impl
        {
            public string StartSemanticVersion { get; set; }
            public string EndSemanticVersion { get; set; }

            public string WorkingCopyOrRepo { get; set; } = Environment.CurrentDirectory;
            public IList<string> ArgumentList { get; } = new List<string>();

            public async Task<int> Run()
            {
                TryInferArgumentsFromList();

                var git = await new GitFinder().FromEnvironment();
                var gitSession = new GitSession(git, new ConsoleInvocationLogger());
                var gitRepository = GetGitRepository(WorkingCopyOrRepo);

                var startCommitJob = new Builds.FindCommits.ResolveCommitFromSemanticVersion(StartSemanticVersion);
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

                if (!string.IsNullOrEmpty(EndSemanticVersion))
                {
                    var endCommitJob = new Builds.FindCommits.ResolveCommitFromSemanticVersion(EndSemanticVersion);
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
                var job = new Builds.FindTickets.ResolveTicketsBetweenRefs(startBuild.Commit, endBuild.Commit);
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

            private void TryInferArgumentsFromList()
            {
                if (!ArgumentList.Any()) return;
                if (ArgumentList.Count > 2)
                {
                    Log.Console.Warn($"Excess arguments detected: {string.Join(" ", ArgumentList.Skip(1))}");
                    // Continue parsing the first two arguments.
                }

                StartSemanticVersion = ArgumentList.First().Trim();
                EndSemanticVersion = ArgumentList.ElementAtOrDefault(1)?.Trim();
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
    }
}

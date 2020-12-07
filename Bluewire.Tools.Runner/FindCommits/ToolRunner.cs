using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Bluewire.Common.Console;
using Bluewire.Common.Console.Logging;
using Bluewire.Common.GitWrapper;
using log4net.Core;
using Bluewire.Tools.Builds.Shared;
using Bluewire.Tools.Builds.FindBuild;
using Bluewire.Conventions;

namespace Bluewire.Tools.Runner.FindCommits
{
    public class ToolRunner : IToolRunner
    {
        public string Name => "find-commits";

        public void Describe(TextWriter writer)
        {
            writer.WriteLine($"  {Name}: Find the commit hash(es) for the specified semantic version (release, build number and optional semantic tag).");
        }

        public int RunMain(string[] args, string parentArgs)
        {
            var tool = new Impl();
            var consoleSession = new ConsoleSession()
            {
                Options = {
                    {"s=|semver=", "Resolve a commit hash for a semantic version (<major>.<minor>.<build>[-semtag]). Omit the semtag if you want all semantic version tags to be searched.", o => tool.Request(RequestType.SemanticVersion, o) },
                    {"repo=|repository=", "Specify the repository to use. Default: current directory.", o => tool.WorkingCopyOrRepo = o}
                }
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
            public RequestType RequestType { get; private set; }
            public string Identifier { get; private set; }

            public void Request(RequestType type, string identifier)
            {
                RequestType = type;
                Identifier = identifier;
            }

            public string WorkingCopyOrRepo { get; set; } = Environment.CurrentDirectory;
            public IList<string> ArgumentList { get; } = new List<string>();

            public async Task<int> Run()
            {
                try
                {
                    TryInferArgumentsFromList();

                    var job = CreateJob();

                    var git = await new GitFinder().FromEnvironment();
                    var gitSession = new GitSession(git, new ConsoleInvocationLogger());

                    var gitRepository = GetGitRepository(WorkingCopyOrRepo);

                    var builds = await job.ResolveCommits(gitSession, gitRepository);

                    if (builds.Length == 0)
                    {
                        Console.Error.WriteLine("No commits found for that build. Try running 'git fetch' to update your repo.");
                        return 1;
                    }
                    else
                    {
                        var originalBuilds = await FindOriginalBuildNumbers(builds, gitSession, gitRepository);
                        var selectedBuilds = BuildUtils.DeduplicateAndPrioritiseResult(originalBuilds);
                        foreach (var build in selectedBuilds)
                        {
                            Console.WriteLine(build);
                        }
                    }

                    return 0;
                }
                catch (RefNotFoundException ex)
                {
                    throw new ErrorWithReturnCodeException(3, ex.Message);
                }
            }

            private async static Task<Build[]> FindOriginalBuildNumbers(Build[] builds, GitSession session, Common.GitWrapper.GitRepository repository)
            {
                List<Build> originalBuilds = new List<Build>();

                // The actual build number to be rendered to the user needs to be calculated because otherwise builds
                // that were originally built as beta will be marked as rc or release if those branches have since been created.
                foreach (var build in builds)
                {
                    var originalBuildVersions = await new ResolveBuildVersionsFromCommit(build.Commit).ResolveBuildVersions(session, repository);
                    originalBuilds.Add(new Build() { SemanticVersion = SemanticVersion.FindEarliestSemanticTag(originalBuildVersions), Commit = build.Commit});
                }

                return originalBuilds.ToArray();
            }

            private void TryInferArgumentsFromList()
            {
                if (!ArgumentList.Any()) return;
                if (RequestType != RequestType.None)
                {
                    Log.Console.Warn($"Excess arguments detected: {String.Join(" ", ArgumentList)}");
                    return;
                }
                if (ArgumentList.Count > 1)
                {
                    Log.Console.Warn($"Excess arguments detected: {String.Join(" ", ArgumentList.Skip(1))}");
                    // Continue parsing the first argument.
                }

                var unqualifiedArgument = ArgumentList.First().Trim();

                Request(RequestType.SemanticVersion, unqualifiedArgument);
            }

            private Builds.FindCommits.IBuildVersionResolutionJob CreateJob()
            {
                switch (RequestType)
                {
                    case RequestType.SemanticVersion:
                        Log.Console.Debug($"Resolving commit from semantic version {Identifier}");
                        return new Builds.FindCommits.ResolveCommitFromSemanticVersion(Identifier);
                }
                throw new InvalidArgumentsException("Semantic version (--semver) must be specified.");
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

        public enum RequestType
        {
            None,
            SemanticVersion
        }
    }
}

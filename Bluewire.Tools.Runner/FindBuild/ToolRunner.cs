using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Bluewire.Common.Console;
using Bluewire.Common.Console.Logging;
using Bluewire.Common.GitWrapper;
using Bluewire.Conventions;
using log4net.Core;
using Bluewire.Tools.Builds.FindBuild;
using Bluewire.Tools.Builds.Shared;
using Bluewire.Tools.GitRepository;

namespace Bluewire.Tools.Runner.FindBuild
{
    public class ToolRunner : IToolRunner
    {
        public string Name => "find-build";

        public void Describe(TextWriter writer)
        {
            writer.WriteLine($"  {Name}: Determine build versions containing a given commit or pull request.");
        }

        public int RunMain(string[] args, string parentArgs)
        {
            var tool = new Impl();
            var consoleSession = new ConsoleSession()
            {
                Options = {
                    {"p=|pull-request=", "Resolve a pull request's merge commit to a build version.", o => tool.Request(RequestType.PullRequest, o) },
                    {"c=|commit=|sha1=", "Resolve a commit hash to a build version.", o => tool.Request(RequestType.Commit, o) },
                    {"t=|ticket=", "Resolve a ticket identifier to a build version.", o => tool.Request(RequestType.TicketIdentifier, o) },
                    {"repo=|repository=", "Specify the repository to use. Default: current directory.", o => tool.WorkingCopyOrRepo = o}
                },
                ListParameterUsage = "<identifier>"
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
                if (RequestType != RequestType.None) throw new InvalidArgumentsException("Only one of --commit, --pull-request or --ticket may be specified.");
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

                    var buildNumbers = await job.ResolveBuildVersions(gitSession, gitRepository);

                    foreach (var buildNumber in buildNumbers) Console.Out.WriteLine(buildNumber);

                    return 0;
                }
                catch (RepositoryStructureException ex)
                {
                    throw new ErrorWithReturnCodeException(3, ex.Message);
                }
                catch (RefNotFoundException ex)
                {
                    throw new ErrorWithReturnCodeException(3, ex.Message);
                }
                catch (PullRequestMergeNotFoundException ex)
                {
                    throw new ErrorWithReturnCodeException(3, ex.Message);
                }
                catch (PullRequestMergesHaveNoCommonParentException ex)
                {
                    throw new ErrorWithReturnCodeException(3, $"{ex.Message} Please specify a commit instead.");
                }
                catch (CannotDetermineBuildNumberException ex)
                {
                    throw new ErrorWithReturnCodeException(4, ex.Message);
                }
                catch (NoCommitsReferenceTicketIdentifierException ex)
                {
                    throw new ErrorWithReturnCodeException(3, ex.Message);
                }
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

                if (unqualifiedArgument.StartsWith("#"))
                {
                    Request(RequestType.PullRequest, unqualifiedArgument);
                }
                else if (Patterns.TicketIdentifierOnly.IsMatch(unqualifiedArgument))
                {
                    Request(RequestType.TicketIdentifier, unqualifiedArgument);
                }
                else
                {
                    Request(RequestType.Commit, unqualifiedArgument);
                }
            }

            private IBuildVersionResolutionJob CreateJob()
            {
                switch (RequestType)
                {
                    case RequestType.Commit:
                        Log.Console.Debug($"Resolving build versions from commit {Identifier}");
                        return new ResolveBuildVersionsFromCommit(Identifier);

                    case RequestType.TicketIdentifier:
                        var trimmedTicketNumber = Identifier.Trim();
                        if (!Patterns.TicketIdentifierOnly.IsMatch(trimmedTicketNumber)) throw new ErrorWithReturnCodeException(3, $"Ticket identifier {trimmedTicketNumber} did not match the expected format.");

                        Log.Console.Debug($"Resolving build versions from ticket identifier {trimmedTicketNumber}");
                        return new ResolveBuildVersionsFromTicketIdentifier(trimmedTicketNumber);

                    case RequestType.PullRequest:
                        int prNumber;
                        if (!int.TryParse(Identifier.Trim().TrimStart('#'), out prNumber)) throw new ErrorWithReturnCodeException(3, $"Could not parse PR number {Identifier}.");

                        Log.Console.Debug($"Resolving build versions from GitHub PR #{prNumber}");
                        return new ResolveBuildVersionsFromGitHubPullRequest(prNumber);
                }
                throw new InvalidArgumentsException("One of --commit, --pull-request or --ticket must be specified.");
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
            PullRequest,
            Commit,
            TicketIdentifier
        }
    }
}

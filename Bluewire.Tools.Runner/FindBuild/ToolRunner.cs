using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Bluewire.Common.Console;
using Bluewire.Common.Console.Logging;
using Bluewire.Common.Console.ThirdParty;
using Bluewire.Common.GitWrapper;
using Bluewire.Conventions;
using log4net.Core;

namespace Bluewire.Tools.Runner.FindBuild
{
    public class ToolRunner : IToolRunner
    {
        public string Name => "find-build";

        public void Describe(TextWriter writer)
        {
            writer.WriteLine($"  {Name}: Determine build versions containing a given commit or pull request.");
        }

        private static OptionSet CreateOptions(Arguments arguments)
        {
            return new OptionSet
            {
                {"p=|pull-request=", "Resolve a pull request's merge commit to a build version.", o => arguments.Request(RequestType.PullRequest, o) },
                {"c=|commit=|sha1=", "Resolve a commit hash to a build version.", o => arguments.Request(RequestType.Commit, o) },
                {"t=|ticket=", "Resolve a ticket identifier to a build version.", o => arguments.Request(RequestType.TicketIdentifier, o) },
                {"repo=|repository=", "Specify the repository to use. Default: current directory.", o => arguments.WorkingCopyOrRepo = o}
            };
        }

        public int RunMain(string[] args, string parentArgs)
        {
            var arguments = new Arguments();
            var options = CreateOptions(arguments);
            var sessionArgs = new SessionArguments<Arguments>(arguments, options);
            sessionArgs.Application += parentArgs;
            return new ConsoleSession<Arguments>(sessionArgs).Run(args, async a => await new Impl(a).Run());
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

                var job = CreateJob(arguments);

                var git = await new GitFinder().FromEnvironment();
                var gitSession = new GitSession(git, new ConsoleInvocationLogger());

                var gitRepository = GetGitRepository(arguments.WorkingCopyOrRepo);

                var buildNumbers = await job.ResolveBuildVersions(gitSession, gitRepository);

                foreach (var buildNumber in buildNumbers) Console.Out.WriteLine(buildNumber);

                return 0;
            }

            private static void TryInferArgumentsFromList(Arguments arguments)
            {
                if (!arguments.ArgumentList.Any()) return;
                if (arguments.RequestType != RequestType.None)
                {
                    Log.Console.Warn($"Excess arguments detected: {String.Join(" ", arguments.ArgumentList)}");
                    return;
                }
                if (arguments.ArgumentList.Count > 1)
                {
                    Log.Console.Warn($"Excess arguments detected: {String.Join(" ", arguments.ArgumentList.Skip(1))}");
                    // Continue parsing the first argument.
                }

                var unqualifiedArgument = arguments.ArgumentList.First().Trim();

                if (unqualifiedArgument.StartsWith("#"))
                {
                    arguments.Request(RequestType.PullRequest, unqualifiedArgument);
                }
                else if (Patterns.TicketIdentifierOnly.IsMatch(unqualifiedArgument))
                {
                    arguments.Request(RequestType.TicketIdentifier, unqualifiedArgument);
                }
                else
                {
                    arguments.Request(RequestType.Commit, unqualifiedArgument);
                }
            }

            private static IBuildVersionResolutionJob CreateJob(Arguments arguments)
            {
                switch (arguments.RequestType)
                {
                    case RequestType.Commit:
                        Log.Console.Debug($"Resolving build versions from commit {arguments.Identifier}");
                        return new ResolveBuildVersionsFromCommit(arguments.Identifier);

                    case RequestType.TicketIdentifier:
                        var trimmedTicketNumber = arguments.Identifier.Trim();
                        if (!Patterns.TicketIdentifierOnly.IsMatch(trimmedTicketNumber)) throw new ErrorWithReturnCodeException(3, $"Ticket identifier {trimmedTicketNumber} did not match the expected format.");

                        Log.Console.Debug($"Resolving build versions from ticket identifier {trimmedTicketNumber}");
                        return new ResolveBuildVersionsFromTicketIdentifier(trimmedTicketNumber);

                    case RequestType.PullRequest:
                        int prNumber;
                        if (!int.TryParse(arguments.Identifier.Trim().TrimStart('#'), out prNumber)) throw new ErrorWithReturnCodeException(3, $"Could not parse PR number {arguments.Identifier}.");

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

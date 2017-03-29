﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Bluewire.Common.Console;
using Bluewire.Common.Console.Logging;
using Bluewire.Common.Console.ThirdParty;
using Bluewire.Common.GitWrapper;
using log4net.Core;

namespace Bluewire.Tools.Runner.FindCommits
{
    public class ToolRunner : IToolRunner
    {
        public string Name => "find-commits";

        public void Describe(TextWriter writer)
        {
            writer.WriteLine($"  {Name}: Find the commit hash(es) for the specified semantic version (release, build number and optional semantic tag).");
        }

        private static OptionSet CreateOptions(Arguments arguments)
        {
            return new OptionSet
            {
                {"s=|semver=", "Resolve a commit hash for a semantic version (<major>.<minor>.<build>[-semtag]). Omit the semtag if you want all semantic version tags to be searched.", o => arguments.Request(RequestType.SemanticVersion, o) },
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
            consoleSession.ListParameterUsage = "<semantic version>";
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

                var job = CreateJob(arguments);

                var git = await new GitFinder().FromEnvironment();
                var gitSession = new GitSession(git, new ConsoleInvocationLogger());

                var gitRepository = GetGitRepository(arguments.WorkingCopyOrRepo);

                var builds = await job.ResolveCommits(gitSession, gitRepository);

                if (builds.Length == 0)
                {
                    Console.Error.WriteLine("No commits found for that build. Try running 'git fetch' to update your repo.");
                    return 1;
                }

                foreach (var build in builds)
                {
                    Console.WriteLine(build);
                }

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
                
                arguments.Request(RequestType.SemanticVersion, unqualifiedArgument);
            }

            private static IBuildVersionResolutionJob CreateJob(Arguments arguments)
            {
                switch (arguments.RequestType)
                {
                    case RequestType.SemanticVersion:
                        Log.Console.Debug($"Resolving commit from semantic version {arguments.Identifier}");
                        return new ResolveCommitsFromSemanticVersion(arguments.Identifier);
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
                RequestType = type;
                Identifier = identifier;
            }

            public string WorkingCopyOrRepo { get; set; } = Environment.CurrentDirectory;
            public IList<string> ArgumentList { get; } = new List<string>();
        }

        public enum RequestType
        {
            None,
            SemanticVersion
        }
    }
}

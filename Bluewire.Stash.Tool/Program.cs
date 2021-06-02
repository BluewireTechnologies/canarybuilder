using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Bluewire.Conventions;
using McMaster.Extensions.CommandLineUtils;

namespace Bluewire.Stash.Tool
{
    public static class Program
    {
        private static async Task<int> Main(string[] args)
        {
            var app = Configure(CreateDefaultApplication());
            try
            {
                return await app.ExecuteAsync(args);
            }
            catch (UnrecognizedCommandParsingException ex)
            {
                await app.Error.WriteLineAsync(ex.Message);
                app.ShowHelp();
                return 1;
            }
        }

        public static IApplication CreateDefaultApplication() => new Application();

        public static CommandLineApplication Configure(IApplication application, CommandLineApplication? app = default)
        {
            app ??= new CommandLineApplication();
            var originalStdout = app.Out;
            // 'Default' output should be the error stream. STDOUT is reserved for machine-readable output.
            app.Out = app.Error;
            app.HelpOption(true);
            app.ValueParsers.Add(new SemanticVersionValueParser());
            app.ValueParsers.Add(new VersionMarkerIdentifierValueParser());

            var gitTopologyPathOption = app.Option<string>("-C|--git-topology <repository>",
                "Path to the Git repository or working copy to use for topology information. If not specified, this will be inferred from current directory. Specify ':none:' to disable it entirely",
                CommandOptionType.SingleValue,
                o => o.Inherited = true);

            var stashRootOption = app.Option<string>("-S|--stash-root <path>",
                "Directory in which local stashes should be kept. By default, uses the environment variable STASH_ROOT or %TEMP%/.stash.",
                CommandOptionType.SingleValue,
                o => o.Inherited = true);

            var verbosityOption = app.Option("-v|--verbose",
                "Increase output verbosity.",
                CommandOptionType.NoValue,
                o => o.Inherited = true);

            var argumentsProvider = new ArgumentsProvider(application);

            app.Command("diagnostics", c =>
            {
                c.AddName("diag");
                c.OnExecute(() =>
                {
                    var model = new DiagnosticsArguments(argumentsProvider.GetAppEnvironment(gitTopologyPathOption, stashRootOption));
                    application.ShowDiagnostics(originalStdout, model);
                });
            });

            app.Command("commit", c =>
            {
                var stashNameArgument = c.Argument<string>("stash name", "The name of the stash to use.", o => o.IsRequired());
                var sourcePathArgument = c.Argument<string>("source", "The directory to store in the stash.", o => o.IsRequired());
                var semanticVersionOption = c.Option<SemanticVersion?>("--version <version>", "The version to stash against.", CommandOptionType.SingleValue);
                var commitHashOption = c.Option<string>("--hash <hash>", "The commit hash to stash against.", CommandOptionType.SingleValue);
                var forceOption = c.Option("-f|--force", "Overwrite any existing, conflicting stash.", CommandOptionType.NoValue);

                c.OnExecuteAsync(async token =>
                {
                    var model = new CommitArguments(argumentsProvider.GetAppEnvironment(gitTopologyPathOption, stashRootOption))
                    {
                        StashName = argumentsProvider.GetStashName(stashNameArgument),
                        SourcePath = argumentsProvider.GetSourcePath(sourcePathArgument),
                        Version = argumentsProvider.GetVersionMarker(semanticVersionOption, commitHashOption),
                        Force = argumentsProvider.GetFlag(forceOption),
                        Verbosity = argumentsProvider.GetVerbosityLevel(verbosityOption),
                    };
                    await application.Commit(app.Error, model, token);
                });
            });

            app.Command("checkout", c =>
            {
                var stashNameArgument = c.Argument<string>("stash name", "The name of the stash to use.", o => o.IsRequired());
                var destinationPathArgument = c.Argument<string>("destination", "The directory to populate with stashed content.", o => o.IsRequired());
                var semanticVersionOption = c.Option<SemanticVersion?>("--version <version>", "The version to resolve.", CommandOptionType.SingleValue);
                var commitHashOption = c.Option<string>("--hash <hash>", "The commit hash to resolve.", CommandOptionType.SingleValue);
                var versionMarkerOption = c.Option<VersionMarker?>("--identifier <identifier>", "The version identifier to resolve.", CommandOptionType.SingleValue);

                c.OnExecuteAsync(async token =>
                {
                    var model = new CheckoutArguments(argumentsProvider.GetAppEnvironment(gitTopologyPathOption, stashRootOption))
                    {
                        StashName = argumentsProvider.GetStashName(stashNameArgument),
                        DestinationPath = argumentsProvider.GetDestinationPath(destinationPathArgument),
                        Version = argumentsProvider.GetVersionMarker(semanticVersionOption, commitHashOption, versionMarkerOption),
                        Verbosity = argumentsProvider.GetVerbosityLevel(verbosityOption),
                    };
                    await application.Checkout(app.Error, model, token);
                });
            });

            app.Command("list", c =>
            {
                var stashNameArgument = c.Argument<string>("stash name", "The name of the stash to use.", o => o.IsRequired());

                c.OnExecuteAsync(async token =>
                {
                    var model = new ListArguments(argumentsProvider.GetAppEnvironment(gitTopologyPathOption, stashRootOption))
                    {
                        StashName = argumentsProvider.GetStashName(stashNameArgument),
                        Verbosity = argumentsProvider.GetVerbosityLevel(verbosityOption),
                    };
                    await application.List(originalStdout, app.Error, model, token);
                });
            });

            app.Command("show", c =>
            {
                var stashNameArgument = c.Argument<string>("stash name", "The name of the stash to use.", o => o.IsRequired());
                var semanticVersionOption = c.Option<SemanticVersion?>("--version <version>", "The version to resolve.", CommandOptionType.SingleValue);
                var commitHashOption = c.Option<string>("--hash <hash>", "The commit hash to resolve.", CommandOptionType.SingleValue);
                var exactMatchOption = c.Option("--exact", "Only exact matches.", CommandOptionType.NoValue);

                c.OnExecuteAsync(async token =>
                {
                    var model = new ShowArguments(argumentsProvider.GetAppEnvironment(gitTopologyPathOption, stashRootOption))
                    {
                        StashName = argumentsProvider.GetStashName(stashNameArgument),
                        Version = argumentsProvider.GetVersionMarker(semanticVersionOption, commitHashOption),
                        ExactMatch = argumentsProvider.GetFlag(exactMatchOption),
                        Verbosity = argumentsProvider.GetVerbosityLevel(verbosityOption),
                    };
                    await application.Show(originalStdout, app.Error, model, token);
                });
            });

            app.Command("delete", c =>
            {
                var stashNameArgument = c.Argument<string>("stash name", "The name of the stash to use.", o => o.IsRequired());
                var semanticVersionOption = c.Option<SemanticVersion?>("--version <version>", "The version to delete.", CommandOptionType.SingleValue);
                var commitHashOption = c.Option<string>("--hash <hash>", "The commit hash to delete.", CommandOptionType.SingleValue);
                var versionMarkerOption = c.Option<VersionMarker?>("--identifier <identifier>", "The version identifier to delete.", CommandOptionType.SingleValue);

                c.OnExecuteAsync(async token =>
                {
                    var model = new DeleteArguments(argumentsProvider.GetAppEnvironment(gitTopologyPathOption, stashRootOption))
                    {
                        StashName = argumentsProvider.GetStashName(stashNameArgument),
                        Version = argumentsProvider.GetRequiredVersionMarker(semanticVersionOption, commitHashOption, versionMarkerOption),
                        Verbosity = argumentsProvider.GetVerbosityLevel(verbosityOption),
                    };
                    await application.Delete(app.Error, model, token);
                });
            });

            app.Command("gc", c =>
            {
                var stashNameArgument = c.Argument<string>("stash name", "The name of the stash to use.", o => o.IsRequired());
                var aggressiveOption = c.Option("-a|--aggressive", "Clean up unresolvable versioned entries too.", CommandOptionType.NoValue);

                c.OnExecuteAsync(async token =>
                {
                    var model = new GCArguments(argumentsProvider.GetAppEnvironment(gitTopologyPathOption, stashRootOption))
                    {
                        StashName = argumentsProvider.GetStashName(stashNameArgument),
                        Aggressive = argumentsProvider.GetFlag(aggressiveOption),
                        Verbosity = argumentsProvider.GetVerbosityLevel(verbosityOption),
                    };
                    await application.GarbageCollect(app.Error, model, token);
                });
            });

            app.OnExecute(() =>
            {
                Console.WriteLine("Specify a command");
                app.ShowHelp();
                return 1;
            });

            return app;
        }

        class Application : IApplication
        {
            public string GetCurrentDirectory() => Directory.GetCurrentDirectory();
            public string GetTemporaryDirectory() => Path.GetTempPath();
            public string? GetEnvironmentVariable(string name) => Environment.GetEnvironmentVariable(name);

            public void ShowDiagnostics(TextWriter stdout, DiagnosticsArguments model)
            {
                stdout.WriteLine($"Git topology:       {model.AppEnvironment.GitTopologyPath}");
                stdout.WriteLine($"Stash root:         {model.AppEnvironment.StashRoot}");
            }

            public async Task Commit(TextWriter stderr, CommitArguments model, CancellationToken token)
            {
                await new CommitCommand().Execute(model, new VerboseLogger(stderr, model.Verbosity.Value), token);
            }

            public async Task Checkout(TextWriter stderr, CheckoutArguments model, CancellationToken token)
            {
                await new CheckoutCommand().Execute(model, new VerboseLogger(stderr, model.Verbosity.Value), token);
            }

            public async Task List(TextWriter stdout, TextWriter stderr, ListArguments model, CancellationToken token)
            {
                await new ListCommand().Execute(model, stdout, new VerboseLogger(stderr, model.Verbosity.Value), token);
            }

            public async Task Show(TextWriter stdout, TextWriter stderr, ShowArguments model, CancellationToken token)
            {
                await new ShowCommand().Execute(model, stdout, new VerboseLogger(stderr, model.Verbosity.Value), token);
            }

            public async Task Delete(TextWriter stderr, DeleteArguments model, CancellationToken token)
            {
                await new DeleteCommand().Execute(model, new VerboseLogger(stderr, model.Verbosity.Value), token);
            }

            public async Task GarbageCollect(TextWriter stderr, GCArguments model, CancellationToken token)
            {
                await new GCCommand().Execute(model, new VerboseLogger(stderr, model.Verbosity.Value), token);
            }
        }
    }
}

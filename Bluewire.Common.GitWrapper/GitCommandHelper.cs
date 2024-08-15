using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper.Model;
using CliWrap;
using CliWrap.Buffered;
using CliWrap.EventStream;

namespace Bluewire.Common.GitWrapper
{
    /// <summary>
    /// Helper class for handling common/simple use cases of the Git wrapper.
    /// </summary>
    public class GitCommandHelper
    {
        public Git Git { get; }
        public IConsoleInvocationLogger Logger { get; }

        public GitCommandHelper(Git git, IConsoleInvocationLogger logger = null)
        {
            Git = git;
            Logger = logger;
        }

        /// <summary>
        /// Creates a Git command line invocation of the specified command and arguments.
        /// </summary>
        public Command CreateCommand(string gitCommand, params string[] arguments)
        {
            return Cli.Wrap(Git.GetExecutableFilePath())
                .WithValidation(CommandResultValidation.None)
                .WithStandardOutputPipe(PipeTarget.ToDelegate(x => { }))
                .AddArguments(gitCommand)
                .AddArguments(arguments);
        }

        /// <summary>
        /// Helper method. Runs a command which is expected to return true or false via exit code. Output is ignored.
        /// </summary>
        public async Task<bool> RunTestCommand(IGitFilesystemContext workingCopyOrRepo, Command command)
        {
            if (workingCopyOrRepo == null) throw new ArgumentNullException(nameof(workingCopyOrRepo));

            var result = await command
                .RunFrom(workingCopyOrRepo)
                .LogInvocation(Logger, out var log)
                .ExecuteAsync()
                .LogResult(log, true);

            return result.ExitCode == 0;
        }

        /// <summary>
        /// Helper method. Runs a command which is expected to simply succeed or fail. Output is ignored.
        /// </summary>
        public async Task RunSimpleCommand(IGitFilesystemContext workingCopyOrRepo, Command command)
        {
            if (workingCopyOrRepo == null) throw new ArgumentNullException(nameof(workingCopyOrRepo));

            var result = await command
                .RunFrom(workingCopyOrRepo)
                .CaptureErrors(out var checker)
                .LogInvocation(Logger, out var log)
                .ExecuteAsync()
                .LogResult(log);

            checker.CheckSuccess(result);
        }

         /// <summary>
        /// Helper method. Runs a command which is expected to produce a single line of output.
        /// </summary>
        public async Task<string> RunSingleLineCommand(IGitFilesystemContext workingCopyOrRepo, Command command)
        {
            if (workingCopyOrRepo == null) throw new ArgumentNullException(nameof(workingCopyOrRepo));

            var result = await command
                .RunFrom(workingCopyOrRepo)
                .LogInvocation(Logger, out var log)
                .ExecuteBufferedAsync()
                .LogResult(log);

            return GitHelpers.ExpectOneLine(command, result);
        }

         /// <summary>
         /// Helper method. Runs a command which is expected to emit a binary stream on STDOUT.
         /// </summary>
         public async Task RunStreamOutputCommand(IGitFilesystemContext workingCopyOrRepo, Command command, Stream targetStream)
         {
             if (workingCopyOrRepo == null) throw new ArgumentNullException(nameof(workingCopyOrRepo));

             var result = await command
                 .RunFrom(workingCopyOrRepo)
                 .CaptureErrors(out var checker)
                 .LogInvocation(Logger, out var log)
                 // Replace the output pipe to prevent logging binaries.
                 .WithStandardOutputPipe(PipeTarget.ToStream(targetStream))
                 .ExecuteAsync()
                 .LogResult(log);

             checker.CheckSuccess(result);
         }

        /// <summary>
        /// Helper method. Runs a command which is expected to produce output which can be parsed asynchronously.
        /// The output is expected to be in Unix format, ie. uses LF only as the line-break character.
        /// </summary>
        public async Task<T> RunCommand<T>(IGitFilesystemContext workingCopyOrRepo, Command command, IGitAsyncOutputParser<T> parser, CancellationToken token = default(CancellationToken))
        {
            T result = default;
            var stdoutPipe = PipeTarget.Create(async (s, t) =>
                result = await parser.Parse(new GitAsyncUnixOutputParserAdapter(Encoding.UTF8)
                    .Parse(s, t), t)
                    .ConfigureAwait(false));

            await PrepareTextOutputCommand(workingCopyOrRepo, command, stdoutPipe, token);
            if (parser.Errors.Any())
            {
                throw new UnexpectedGitOutputFormatException(command, parser.Errors.ToArray());
            }
            return result;
        }

        private async Task PrepareTextOutputCommand(IGitFilesystemContext workingCopyOrRepo, Command command, PipeTarget target, CancellationToken token = default(CancellationToken))
        {
            if (workingCopyOrRepo == null) throw new ArgumentNullException(nameof(workingCopyOrRepo));
            var result = await command
                .RunFrom(workingCopyOrRepo)
                .CaptureErrors(out var checker)
                .WithStandardOutputPipe(target)
                .LogInvocation(Logger, out var log)
                .ExecuteAsync(token)
                .LogResult(log);
            checker.CheckSuccess(result);
        }

        /// <summary>
        /// Helper method. Runs a command which is expected to produce output which can be parsed and collected asynchronously.
        /// The output is expected to be in Unix format, ie. uses LF only as the line-break character.
        /// </summary>
        public async Task RunCommand(IGitFilesystemContext workingCopyOrRepo, Command command, Action<string> collectLine, CancellationToken token = default(CancellationToken))
        {
            var stdoutPipe = PipeTarget.Create(async (s, t) =>
                await new GitAsyncUnixOutputParserAdapter(Encoding.UTF8)
                    .CollectLines(s, collectLine, t)
                    .ConfigureAwait(false));
            await PrepareTextOutputCommand(workingCopyOrRepo, command, stdoutPipe, token);
        }

        /// <summary>
        /// Waits only for the task to complete, ignoring the manner in which it completes.
        /// Does not rethrow exceptions, etc as t.Wait() would.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static async Task WaitForCompletion(Task t)
        {
            if (t.IsCompleted) return;
            await t.ContinueWith(c => { }).ConfigureAwait(false);
        }
    }
}

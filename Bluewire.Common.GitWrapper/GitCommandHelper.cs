using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using Bluewire.Common.Console.Client.Shell;
using Bluewire.Common.GitWrapper.Async;
using Bluewire.Common.GitWrapper.Model;

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
        public CommandLine CreateCommand(string gitCommand, params string[] arguments)
        {
            var cmd = new CommandLine(Git.GetExecutableFilePath(), gitCommand);
            cmd.Add(arguments);
            return cmd;
        }

        /// <summary>
        /// Helper method. Runs a command which is expected to return true or false via exit code. Output is ignored.
        /// </summary>
        public async Task<bool> RunTestCommand(IGitFilesystemContext workingCopyOrRepo, CommandLine command)
        {
            if (workingCopyOrRepo == null) throw new ArgumentNullException(nameof(workingCopyOrRepo));

            var process = workingCopyOrRepo.Invoke(command);
            using (var log = Logger?.LogInvocation(process))
            {
                process.StdOut.StopBuffering();
                log?.IgnoreExitCode();

                var exitCode = await process.Completed;
                return exitCode == 0;
            }
        }

        /// <summary>
        /// Helper method. Runs a command which is expected to simply succeed or fail. Output is ignored.
        /// </summary>
        public Task RunSimpleCommand(IGitFilesystemContext workingCopyOrRepo, string gitCommand, params string[] arguments)
        {
            var cmd = CreateCommand(gitCommand, arguments);
            return RunSimpleCommand(workingCopyOrRepo, cmd);
        }

        /// <summary>
        /// Helper method. Runs a command which is expected to simply succeed or fail. Output is ignored.
        /// </summary>
        [Obsolete("Use 'RunSimpleCommand(IGitFilesystemContext workingCopyOrRepo, CommandLine command)' instead.")]
        public async Task RunSimpleCommand(IGitFilesystemContext workingCopyOrRepo, string gitCommand, Action<CommandLine> prepareCommand)
        {
            var cmd = CreateCommand(gitCommand);
            prepareCommand(cmd);
            await RunSimpleCommand(workingCopyOrRepo, cmd);
        }

        /// <summary>
        /// Helper method. Runs a command which is expected to simply succeed or fail. Output is ignored.
        /// </summary>
        public async Task RunSimpleCommand(IGitFilesystemContext workingCopyOrRepo, CommandLine command)
        {
            if (workingCopyOrRepo == null) throw new ArgumentNullException(nameof(workingCopyOrRepo));

            var process = workingCopyOrRepo.Invoke(command);
            using (Logger?.LogInvocation(process))
            {
                process.StdOut.StopBuffering();

                await GitHelpers.ExpectSuccess(process);
            }
        }

         /// <summary>
        /// Helper method. Runs a command which is expected to produce a single line of output.
        /// </summary>
        public async Task<string> RunSingleLineCommand(IGitFilesystemContext workingCopyOrRepo, string gitCommand, params string[] arguments)
        {
            var cmd = CreateCommand(gitCommand, arguments);
            return await RunSingleLineCommand(workingCopyOrRepo, cmd);
        }

         /// <summary>
        /// Helper method. Runs a command which is expected to produce a single line of output.
        /// </summary>
        public async Task<string> RunSingleLineCommand(IGitFilesystemContext workingCopyOrRepo, CommandLine command)
        {
            if (workingCopyOrRepo == null) throw new ArgumentNullException(nameof(workingCopyOrRepo));

            var process = workingCopyOrRepo.Invoke(command);
            using (Logger?.LogInvocation(process))
            {
                return await GitHelpers.ExpectOneLine(process);
            }
        }

        /// <summary>
        /// Helper method. Runs a command which is expected to produce output which can be consumed asynchronously.
        /// </summary>
        public async Task<T> RunCommand<T>(IGitFilesystemContext workingCopyOrRepo, CommandLine command, Func<IAsyncEnumerator<string>, Task<T>> parseLines)
        {
            var process = workingCopyOrRepo.Invoke(command);
            using (Logger?.LogInvocation(process))
            {
                using (var stdoutEnumerator = process.StdOut.GetAsyncEnumerator())
                {
                    process.StdOut.StopBuffering();
                    var result = parseLines(stdoutEnumerator);
                    await GitHelpers.ExpectSuccess(process);
                    return await result;
                }
            }
        }

        /// <summary>
        /// Helper method. Runs a command which is expected to produce output which can be parsed easily as a series of lines.
        /// </summary>
        public async Task<T[]> RunCommand<T>(IGitFilesystemContext workingCopyOrRepo, CommandLine command, Func<IObservable<string>, IObservable<T>> pipeline)
        {
            var process = workingCopyOrRepo.Invoke(command);
            using (Logger?.LogInvocation(process))
            {
                var result = pipeline(process.StdOut).ToArray().ToTask();
                process.StdOut.StopBuffering();
                await GitHelpers.ExpectSuccess(process);
                return await result;
            }
        }

        /// <summary>
        /// Helper method. Given a running process, parses its STDOUT stream line-by-line using the specified
        /// parser instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="process"></param>
        /// <param name="parser"></param>
        /// <returns></returns>
        public async Task<T[]> ParseLineOutput<T>(IConsoleProcess process, IGitLineOutputParser<T> parser)
        {
            using (Logger?.LogInvocation(process))
            {
                var parsedList = process.StdOut.Select(l => parser.ParseOrNull(l)).ToArray().ToTask();
                process.StdOut.StopBuffering();
                await GitHelpers.ExpectSuccess(process);
                await WaitForCompletion(parsedList);
                if (parser.Errors.Any())
                {
                    throw new UnexpectedGitOutputFormatException(process.CommandLine, parser.Errors.ToArray());
                }
                return await parsedList;
            }
        }

        /// <summary>
        /// Helper method. Given a running process, parses its STDOUT stream line-by-line using the specified
        /// asynchronous parser instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="process"></param>
        /// <param name="parser"></param>
        /// <returns></returns>
        public async Task<T> ParseOutput<T>(IConsoleProcess process, IGitAsyncOutputParser<T> parser, CancellationToken token)
        {
            using (Logger?.LogInvocation(process))
            {
                using (var stdoutEnumerator = process.StdOut.GetAsyncEnumerator())
                {
                    process.StdOut.StopBuffering();
                    var result = parser.Parse(stdoutEnumerator, token);
                    await GitHelpers.ExpectSuccess(process);
                    await WaitForCompletion(result);
                    if (parser.Errors.Any())
                    {
                        throw new UnexpectedGitOutputFormatException(process.CommandLine, parser.Errors.ToArray());
                    }
                    return await result;
                }
            }
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

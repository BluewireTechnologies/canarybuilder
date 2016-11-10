using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
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
        /// Helper method. Runs a command which is expected to simply succeed or fail. Output is ignored.
        /// </summary>
        public Task RunSimpleCommand(IGitFilesystemContext workingCopyOrRepo, string gitCommand, params string[] arguments)
        {
            return RunSimpleCommand(workingCopyOrRepo, gitCommand, c => c.Add(arguments));
        }

        /// <summary>
        /// Helper method. Runs a command which is expected to simply succeed or fail. Output is ignored.
        /// </summary>
        public async Task RunSimpleCommand(IGitFilesystemContext workingCopyOrRepo, string gitCommand, Action<CommandLine> prepareCommand)
        {
            if (workingCopyOrRepo == null) throw new ArgumentNullException(nameof(workingCopyOrRepo));

            var cmd = new CommandLine(Git.GetExecutableFilePath(), gitCommand);
            prepareCommand(cmd);
            var process = workingCopyOrRepo.Invoke(cmd);
            using (Logger?.LogInvocation(process))
            {
                process.StdOut.StopBuffering();

                await GitHelpers.ExpectSuccess(process);
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

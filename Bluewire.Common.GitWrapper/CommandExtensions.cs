using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bluewire.Common.GitWrapper.Model;
using CliWrap;
using CliWrap.Builders;
using CliWrap.EventStream;

namespace Bluewire.Common.GitWrapper
{
    /// <summary>
    /// Various helper methods for injecting logging and result checks, as well as conveniently appending to argument lists.
    /// </summary>
    public static class CommandExtensions
    {
        public static Command LogInvocation(this Command command, IConsoleInvocationLogger logger, out IConsoleInvocationLogScope log)
        {
            log = logger?.Create();
            return log?.LogOutputs(command) ?? command;
        }

        public static Command LogInvocationErrorsOnly(this Command command, IConsoleInvocationLogger logger, out IConsoleInvocationLogScope log)
        {
            log = logger?.Create();
            return log?.LogOutputs(command) ?? command;
        }

        public static Command LogMinorInvocation(this Command command, IConsoleInvocationLogger logger, out IConsoleInvocationLogScope log)
        {
            log = logger?.CreateMinor();
            return log?.LogOutputs(command) ?? command;
        }

        public static T LogResult<T>(this T result, IConsoleInvocationLogScope log, bool ignoreExitCode = false) where T : CommandResult
        {
            log?.LogResult(result, ignoreExitCode);
            return result;
        }

        public static CommandTask<T> LogResult<T>(this CommandTask<T> resultTask, IConsoleInvocationLogScope log, bool ignoreExitCode = false) where T : CommandResult
        {
            return resultTask.Select(r =>
            {
                log?.LogResult(r, ignoreExitCode);
                return r;
            });
        }

        public static async IAsyncEnumerable<CommandEvent> OnExit(this IAsyncEnumerable<CommandEvent> enumerable, Action<CommandResult> onExit)
        {
            var listener = new CommandResultEventListener(onExit);
            await foreach (var commandEvent in enumerable)
            {
                listener.OnNext(commandEvent);
                yield return commandEvent;
            }
        }

        public static IAsyncEnumerable<CommandEvent> LogResult(this IAsyncEnumerable<CommandEvent> enumerable, IConsoleInvocationLogScope log, bool ignoreExitCode = false)
        {
            return enumerable.OnExit(r => log?.LogResult(r, ignoreExitCode));
        }

        public static Command TeeStandardOutput(this Command command, Action<string> onLine) => TeeStandardOutput(command, PipeTarget.ToDelegate(onLine));
        public static Command TeeStandardOutput(this Command command, PipeTarget target)
        {
            return command.WithStandardOutputPipe(PipeTarget.Merge(command.StandardOutputPipe, target));
        }

        public static Command TeeStandardError(this Command command, Action<string> onLine) => TeeStandardError(command, PipeTarget.ToDelegate(onLine));
        public static Command TeeStandardError(this Command command, PipeTarget target)
        {
            return command.WithStandardErrorPipe(PipeTarget.Merge(command.StandardErrorPipe, target));
        }

        public static Command AddArguments(this Command command, params string[] arguments)
        {
            var notNull = arguments.Where(a => a != null);
            return command.AddArguments(a => a.Add(notNull, true));
        }

        public static Command AddArguments(this Command command, Action<ArgumentsBuilder> build)
        {
            return command.WithArguments(a => {
                // Keep the existing arguments as-is.
                a.Add(command.Arguments, false);
                build(a);
            });
        }

        public static void Add(this ArgumentsBuilder builder, params string[] args) => builder.Add(args);

        public static Command CaptureErrors(this Command command, out IResultChecker checker)
        {
            var impl = new ResultChecker(command);
            checker = impl;
            return impl.Attach();
        }

        class ResultChecker : IResultChecker
        {
            private readonly Command command;
            private readonly StringBuilder buffer = new StringBuilder();

            public ResultChecker(Command command)
            {
                this.command = command;
            }

            public Command Attach()
            {
                return command.WithStandardErrorPipe(PipeTarget.Merge(command.StandardErrorPipe, PipeTarget.ToStringBuilder(buffer)));
            }

            public void CheckSuccess(CommandResult result)
            {
                if (result.ExitCode != 0) throw new GitException(command, result.ExitCode, buffer.ToString().Trim());
            }
        }

        public interface IResultChecker
        {
            void CheckSuccess(CommandResult result);
        }

        public static Command RunFrom(this Command command, IGitFilesystemContext workingCopyOrRepo)
        {
            return command.WithWorkingDirectory(workingCopyOrRepo.GitWorkingDirectory);
        }

        class CommandResultEventListener : IObserver<CommandEvent>
        {
            private readonly Action<CommandResult> onExit;
            private readonly IConsoleInvocationLogger logger;
            private readonly bool ignoreExitCode;

            public CommandResultEventListener(Action<CommandResult> onExit)
            {
                this.onExit = onExit;
                this.logger = logger;
                this.ignoreExitCode = ignoreExitCode;
            }

            private DateTimeOffset start;
            private DateTimeOffset? end;
            private int exitCode;

            public void OnNext(CommandEvent value)
            {
                switch (value)
                {
                    case StartedCommandEvent _:
                        start = DateTimeOffset.Now;
                        break;

                    case ExitedCommandEvent exitedCommandEvent:
                        end = DateTimeOffset.Now;
                        exitCode = exitedCommandEvent.ExitCode;
                        onExit(new CommandResult(exitCode, start, end.Value));
                        break;
                }
            }

            public void OnError(Exception error) { }
            public void OnCompleted() { }
        }
    }
}

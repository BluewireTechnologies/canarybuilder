using System;
using System.Reactive;
using System.Reactive.Disposables;
using Bluewire.Common.Console.Client.Shell;

namespace CanaryBuilder.Logging
{
    public class PlainTextJobLogger : IJobLogger
    {
        private readonly IPlainTextLogWriter output;
        private int indentLevel;

        public PlainTextJobLogger(IPlainTextLogWriter output)
        {
            this.output = output;
        }

        private void Write(int indentLevel, string markerString, string text, ConsoleColor? colour = null)
        {
            var indent = new String(' ', indentLevel);
            var marker = $"[{markerString}]".PadRight(8, ' ');
            output.WriteLine($"{indent} {marker}  {text}", colour);
        }
        
        public void Info(string message)
        {
            Write(indentLevel, "INFO", message);
        }

        public void Warn(string message)
        {
            Write(indentLevel, "WARN", message, ConsoleColor.Yellow);
        }

        public void Error(string message)
        {
            Write(indentLevel, "ERROR", message, ConsoleColor.Red);
        }

        public IDisposable EnterScope(string message)
        {
            var currentIndent = indentLevel++;
            Write(currentIndent, "BEGIN", message, ConsoleColor.Cyan);

            return Disposable.Create(() => { indentLevel--; });
        }

        public IDisposable LogInvocation(IConsoleProcess process)
        {
            Write(indentLevel, "SHELL", process.CommandLine.ToString(), ConsoleColor.White);
            var scopeIndent = indentLevel + 1;

            var stdOutLogger = Observer.Create<string>(l => Write(scopeIndent, "STDOUT", l));
            var stdErrLogger = Observer.Create<string>(l => Write(scopeIndent, "STDERR", l, ConsoleColor.DarkYellow));

            var stdOutSubscription = process.StdOut.Subscribe(stdOutLogger);
            var stdErrSubscription = process.StdErr.Subscribe(stdErrLogger);

            return Disposable.Create(() => {
                var exitCode = process.Completed.Result;
                Write(scopeIndent, "SHELL", $"Exit code: {exitCode}", exitCode == 0 ? ConsoleColor.White : ConsoleColor.Yellow);
                stdOutSubscription.Dispose();
                stdErrSubscription.Dispose();
            });
        }

        public IDisposable LogMinorInvocation(IConsoleProcess process)
        {
            return Disposable.Create(() => {
                var exitCode = process.Completed.Result;
                Write(indentLevel, "SHELL", $"{process.CommandLine} exited with code {exitCode}");
            });
        }
    }
}

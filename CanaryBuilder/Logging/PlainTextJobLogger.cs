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

        public void Error(string message, Exception exception)
        {
            Write(indentLevel, "ERROR", message, ConsoleColor.Red);
        }

        public void Error(Exception exception)
        {
            Write(indentLevel, "ERROR", exception.Message, ConsoleColor.Red);
        }

        public IDisposable EnterScope(string message)
        {
            var currentIndent = indentLevel++;
            Write(currentIndent, "BEGIN", message, ConsoleColor.Cyan);

            return Disposable.Create(() => { indentLevel--; });
        }

        public IConsoleInvocationLogScope LogInvocation(IConsoleProcess process)
        {
            Write(indentLevel, "SHELL", process.CommandLine.ToString(), ConsoleColor.White);
            return new InvocationLogScope(this, process);
        }

        public IConsoleInvocationLogScope LogMinorInvocation(IConsoleProcess process)
        {
            return new MinorInvocationLogScope(this, process);
        }

        class InvocationLogScope : ConsoleInvocationLogScope
        {
            private readonly PlainTextJobLogger parent;
            private readonly IConsoleProcess process;
            private readonly int scopeIndent;
            private bool highlightErrorCode = true;

            public InvocationLogScope(PlainTextJobLogger parent, IConsoleProcess process)
            {
                this.parent = parent;
                this.process = process;
                this.scopeIndent = parent.indentLevel + 1;

                var stdOutLogger = Observer.Create<string>(l => parent.Write(scopeIndent, "STDOUT", l));
                var stdErrLogger = Observer.Create<string>(l => parent.Write(scopeIndent, "STDERR", l, ConsoleColor.DarkYellow));

                RecordSubscription(process.StdOut.Subscribe(stdOutLogger));
                RecordSubscription(process.StdErr.Subscribe(stdErrLogger));
            }

            public override void IgnoreExitCode()
            {
                highlightErrorCode = false;
            }

            public override void Dispose()
            {
                var exitCode = process.Completed.Result;
                parent.Write(scopeIndent, "SHELL", $"Exit code: {exitCode}", (highlightErrorCode && exitCode != 0) ? ConsoleColor.Yellow : ConsoleColor.White);
                base.Dispose();
            }
        }

        class MinorInvocationLogScope : ConsoleInvocationLogScope
        {
            private readonly PlainTextJobLogger parent;
            private readonly IConsoleProcess process;
            private bool showErrorCode = true;

            public MinorInvocationLogScope(PlainTextJobLogger parent, IConsoleProcess process)
            {
                this.parent = parent;
                this.process = process;
            }

            public override void IgnoreExitCode()
            {
                showErrorCode = false;
            }

            public override void Dispose()
            {
                var exitCode = process.Completed.Result;
                if (showErrorCode && exitCode != 0)
                {
                    parent.Write(parent.indentLevel, "SHELL", $"{process.CommandLine} exited with code {exitCode}", ConsoleColor.Yellow);
                }
                base.Dispose();
            }
        }
    }
}

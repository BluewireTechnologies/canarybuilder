using System;
using Bluewire.Common.GitWrapper;
using CliWrap;

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

        public void Warn(string message, Exception exception)
        {
            if (exception != null) Write(indentLevel, "WARN", exception.Message, ConsoleColor.Yellow);
            Write(indentLevel, "WARN", message, ConsoleColor.Yellow);
        }

        public void Warn(Exception exception)
        {
            Write(indentLevel, "WARN", exception.Message, ConsoleColor.Yellow);
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

            return new Scope(this);
        }

        class Scope : IDisposable
        {
            private PlainTextJobLogger parent;

            public Scope(PlainTextJobLogger parent)
            {
                this.parent = parent;
            }

            public void Dispose()
            {
                if (parent == null) return;
                parent.indentLevel--;
                parent = null;
            }
        }

        public IConsoleInvocationLogScope Create() => new InvocationLogScope(this);

        public IConsoleInvocationLogScope CreateMinor() => new InvocationLogScope(this);

        class InvocationLogScope : IConsoleInvocationLogScope
        {
            private readonly PlainTextJobLogger parent;
            private readonly int scopeIndent;

            public InvocationLogScope(PlainTextJobLogger parent)
            {
                this.parent = parent;
                this.scopeIndent = parent.indentLevel + 1;
            }

            public Command LogOutputs(Command command)
            {
                parent.Write(parent.indentLevel, "SHELL", command.ToString(), ConsoleColor.White);
                return command
                    .TeeStandardOutput(l => parent.Write(scopeIndent, "STDOUT", l))
                    .TeeStandardError(l => parent.Write(scopeIndent, "STDERR", l, ConsoleColor.DarkYellow));
            }

            public void LogResult(CommandResult result, bool ignoreExitCode)
            {
                parent.Write(scopeIndent, "SHELL", $"Exit code: {result.ExitCode}", (ignoreExitCode || result.ExitCode == 0) ? ConsoleColor.White : ConsoleColor.Yellow);
            }
        }

        class MinorInvocationLogScope : IConsoleInvocationLogScope
        {
            private readonly PlainTextJobLogger parent;
            private Command command;

            public MinorInvocationLogScope(PlainTextJobLogger parent)
            {
                this.parent = parent;
            }

            public Command LogOutputs(Command command)
            {
                this.command = command;
                return command;
            }

            public void LogResult(CommandResult result, bool ignoreExitCode)
            {
                if (ignoreExitCode || result.ExitCode == 0) return;
                parent.Write(parent.indentLevel, "SHELL", $"{command} exited with code {result.ExitCode}", ConsoleColor.Yellow);
            }
        }
    }
}

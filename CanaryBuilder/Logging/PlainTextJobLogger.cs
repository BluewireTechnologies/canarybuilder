using System;
using System.IO;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Bluewire.Common.Console.Client.Shell;
using Bluewire.Common.Git;

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

        private void Write(Entry entry)
        {
            if (entry.Type == Type.EndScope) return; // This logger doesn't bother writing these.

            var indent = new String(' ', entry.Level);
            switch (entry.Type)
            {
                case Type.StdOut:
                    output.WriteLine($"{indent} [STDOUT]  {entry.Line}");
                    return;
                case Type.StdErr:
                    output.WriteLine($"{indent} [STDERR]  {entry.Line}", ConsoleColor.Yellow);
                    return;
                case Type.Info:
                    output.WriteLine($"{indent} [INFO]    {entry.Line}");
                    return;
                case Type.Warning:
                    output.WriteLine($"{indent} [WARN]    {entry.Line}", ConsoleColor.Yellow);
                    return;
                case Type.Error:
                    output.WriteLine($"{indent} [ERROR]   {entry.Line}", ConsoleColor.Red);
                    return;
                case Type.Invocation:
                    output.WriteLine($"{indent} [SHELL]   {entry.Line}", ConsoleColor.White);
                    return;
                case Type.ExitCode:
                    output.WriteLine($"{indent} [SHELL] Exit code: {entry.Line}", entry.Line == "0" ? ConsoleColor.White : ConsoleColor.Yellow);
                    return;
                case Type.BeginScope:
                    output.WriteLine($"{indent} [BEGIN]   {entry.Line}", ConsoleColor.Cyan);
                    return;
            }
        }
        
        public void Info(string message)
        {
            Write(new Entry { Line = $"{message}", Type = Type.Info, Level = indentLevel });
        }

        public void Warn(string message)
        {
            Write(new Entry { Line = $"{message}", Type = Type.Warning, Level = indentLevel });
        }

        public void Error(string message)
        {
            Write(new Entry { Line = $"{message}", Type = Type.Error, Level = indentLevel });
        }

        public IDisposable EnterScope(string message)
        {
            var currentIndent = indentLevel++;
            Write(new Entry { Line = $"{message}", Type = Type.BeginScope, Level = currentIndent });

            return Disposable.Create(() =>
            {
                Write(new Entry { Line = $"{message}", Type = Type.EndScope, Level = currentIndent });
                indentLevel--;
            });
        }

        public IDisposable LogInvocation(IConsoleProcess process)
        {
            Write(new Entry { Line = $"{process.CommandLine}", Type = Type.Invocation, Level = indentLevel });
            var scopeIndent = indentLevel + 1;
            var stdout = process.StdOut.Select(l => new Entry { Line = l, Type = Type.StdOut, Level = scopeIndent });
            var stderr = process.StdErr.Select(l => new Entry { Line = l, Type = Type.StdErr, Level = scopeIndent });

            var logger = Observer.Create<Entry>(Write);

            var subscription = stdout.Merge(stderr).Subscribe(logger);
            return Disposable.Create(() => {
                var exitCode = process.Completed.Result;
                Write(new Entry { Line = $"{exitCode}", Type = Type.ExitCode, Level = scopeIndent });
                subscription.Dispose();
            });
        }

        class Entry
        {
            public string Line { get; set; }
            public Type Type { get; set; }
            public int Level { get; set; }
        }

        enum Type
        {
            StdOut,
            StdErr,
            Info,
            Warning,
            Error,
            Invocation,
            ExitCode,
            BeginScope,
            EndScope
        }
    }
}

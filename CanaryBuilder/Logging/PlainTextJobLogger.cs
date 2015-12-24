using System;
using System.IO;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
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
                case Type.BeginScope:
                    output.WriteLine($"{indent} [BEGIN]   {entry.Line}", ConsoleColor.Cyan);
                    return;
            }
        }
        
        private static string FormatEntryType(Entry entry)
        {
            switch (entry.Type)
            {
                case Type.StdOut:    return "[STDOUT]";
                case Type.StdErr:    return "[STDERR]";
                case Type.Info:      return "[INFO]";
                case Type.Warning:   return "[WARN]";
                case Type.Error:     return "[ERROR]";
                case Type.Invocation: return "[SHELL]";
                case Type.BeginScope: return "[BEGIN]";
                case Type.EndScope:  return "[END]";
            }
            return "      ";
        }

        private string FormatEntry(Entry entry)
        {
            return String.Concat(
                new String(' ', entry.Level),
                " ",
                FormatEntryType(entry).PadRight(8, ' '),
                "  ",
                entry.Line);
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

            return stdout.Merge(stderr).Subscribe(logger);
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
            BeginScope,
            EndScope
        }
    }
}

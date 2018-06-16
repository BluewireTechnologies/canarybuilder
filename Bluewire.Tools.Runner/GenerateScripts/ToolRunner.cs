using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Bluewire.Common.Console;
using Bluewire.Common.Console.Logging;
using log4net.Core;

namespace Bluewire.Tools.Runner.GenerateScripts
{
    public class ToolRunner : IToolRunner
    {
        private readonly string[] toolNames;

        public ToolRunner(string[] toolNames)
        {
            this.toolNames = toolNames;
        }

        public string Name => "generate-scripts";

        public void Describe(TextWriter writer)
        {
            writer.WriteLine($"  {Name}: Generate invoker scripts for each known tool.");
        }

        public int RunMain(string[] args, string parentArgs)
        {
            var tool = new Impl(toolNames);
            var consoleSession = new ConsoleSession()
            {
                Options = {
                    { "d=|directory=", "Produce scripts in the specified directory. Default: same directory as this runner.", o => tool.TargetDirectory = o },
                    { "r=|runner=", "Specify the path to the runner, relative to the directory which will contain the scripts.", o => tool.RunnerPath = o }
                }
            };
            consoleSession.Application += parentArgs;
            var logger = consoleSession.Options.AddCollector(new SimpleConsoleLoggingPolicy { Verbosity = { Default = Level.Info } });

            return consoleSession.Run(args, async () => {
                using (LoggingPolicy.Register(consoleSession, logger))
                {
                    return await tool.Run();
                }
            });
        }

        class Impl
        {
            public string TargetDirectory { get; set; }
            public string RunnerPath { get; set; }

            private readonly string[] toolNames;

            public Impl(string[] toolNames)
            {
                this.toolNames = toolNames;

                var applicationLocation = Environment.GetCommandLineArgs().FirstOrDefault();
                TargetDirectory = applicationLocation == null ? AppDomain.CurrentDomain.BaseDirectory : Path.GetDirectoryName(applicationLocation);
            }

            public Task<int> Run()
            {
                var targetDirectory = Path.GetFullPath(TargetDirectory);
                Log.Console.Debug($"Scripts will be produced in: {targetDirectory}");

                var runnerInvocation = GetRunnerInvocation(targetDirectory, RunnerPath);
                var shellRunnerInvocation = ToLinuxPath(runnerInvocation);
                Log.Console.Debug($"Using Linux runner invocation: {shellRunnerInvocation}");

                if (!Directory.Exists(targetDirectory))
                {
                    Log.Console.Debug("Target directory does not exist. It will be created.");
                    Directory.CreateDirectory(targetDirectory);
                }

                foreach (var toolName in toolNames)
                {
                    var nameRoot = Path.Combine(targetDirectory, toolName);
                    Log.Console.Debug($"Writing {nameRoot}...");
                    File.WriteAllText(nameRoot, GetShellScriptContent(shellRunnerInvocation, toolName));
                    Log.Console.Debug($"Writing {nameRoot}.cmd...");
                    File.WriteAllText($"{nameRoot}.cmd", GetCommandFileContent(runnerInvocation, toolName));
                }
                return Task.FromResult(0);
            }

            private string GetShellScriptContent(string shellRunnerInvocation, string toolName)
            {
                return $@"#!/bin/sh
{shellRunnerInvocation} --tool {toolName} ""$@""
";
            }
            private string GetCommandFileContent(string runnerInvocation, string toolName)
            {
                return $@"@echo off
{runnerInvocation} --tool {toolName} %*
";
            }

            private static string ToLinuxPath(string fullPath)
            {
                if (!Path.IsPathRooted(fullPath))
                {
                    return fullPath.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');
                }
                var directory = Path.GetDirectoryName(fullPath)?.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');
                var fileName = Path.GetFileName(fullPath);
                var root = Path.GetPathRoot(fullPath);
                if (root?.EndsWith(":") == true)
                {
                    return $"/{root.TrimEnd(':')}{directory}/{fileName}";
                }
                return $"{directory}/{fileName}";
            }

            private static string GetRunnerInvocation(string targetDirectory, string runnerPath)
            {
                var defaultRunnerExecutable = $"{Assembly.GetExecutingAssembly().GetName().Name}.exe";
                if (String.IsNullOrWhiteSpace(runnerPath))
                {
                    // Default to just using the name of the runner's executable, trusting that it is in the user's PATH.
                    Log.Console.Debug($"Using default runner invocation: {defaultRunnerExecutable}");
                    return defaultRunnerExecutable;
                }
                var absoluteRunnerPath = Path.Combine(targetDirectory, runnerPath);
                if (Directory.Exists(absoluteRunnerPath))
                {
                    // If we determine that the path points to a directory, look for the runner inside it.
                    var possibleRunnerPath = Path.Combine(runnerPath, defaultRunnerExecutable);
                    var possibleAbsoluteRunnerPath = Path.Combine(runnerPath, defaultRunnerExecutable);
                    if (File.Exists(possibleAbsoluteRunnerPath))
                    {
                        Log.Console.Debug($"Using runner invocation: {possibleRunnerPath}");
                        Log.Console.Debug($"(Found at: {possibleAbsoluteRunnerPath})");
                        return possibleRunnerPath;
                    }
                }
                if (File.Exists(absoluteRunnerPath))
                {
                    Log.Console.Debug($"Using runner invocation: {runnerPath}");
                    Log.Console.Debug($"(Found at: {absoluteRunnerPath})");
                    return runnerPath;
                }
                Log.Console.Debug($"Using runner invocation (unverified): {runnerPath}");
                Log.Console.Debug("(Unverified)");
                return runnerPath;
            }
        }

        public enum RequestType
        {
            None,
            PullRequest,
            Commit,
            TicketIdentifier
        }
    }
}

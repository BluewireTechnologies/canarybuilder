using System;
using System.Threading.Tasks;
using Bluewire.Common.Console;
using Bluewire.Common.Console.Logging;
using CanaryBuilder.Logging;
using CanaryBuilder.Merge;
using CanaryBuilder.Parsers;
using log4net.Core;

namespace CanaryBuilder
{
    public class Program
    {
        public static int Main(string[] args)
        {
            var program = new Program();
            var session = new ConsoleSession
            {
                Options = {
                    { "diagnostics", "Show diagnostics and environment information.", o => program.ShowDiagnostics = true }
                },
                ArgumentList = {
                    { "mode", o => program.Mode = o },
                    { "script path", o => program.ScriptPath = o },
                    { "working copy", o => program.WorkingCopy = o }
                },
                ListParameterUsage = "[merge <script path> <working copy>]"
            };
            var logger = session.Options.AddCollector(new SimpleConsoleLoggingPolicy { Verbosity = { Default = Level.Info } });

            return session.Run(args, async () => {
                using (LoggingPolicy.Register(session, logger))
                {
                    return await program.Run();
                }
            });
        }

        public bool ShowDiagnostics { get; set; }

        public string Mode { get; set; }
        public string ScriptPath { get; set; }
        public string WorkingCopy { get; set; }

        public async Task<int> Run()
        {
            if (ShowDiagnostics)
            {
                return await new Diagnostics().Run(Console.Out);
            }
            if (Mode == "merge")
            {
                if (String.IsNullOrWhiteSpace(ScriptPath)) throw new InvalidArgumentsException("No script path specified for 'merge' mode.");
                if (String.IsNullOrWhiteSpace(WorkingCopy)) throw new InvalidArgumentsException("No working copy specified for 'merge' mode.");

                using (var console = new ConsoleLogWriter())
                {
                    var logger = new PlainTextJobLogger(console);
                    return await RunMergeJob(logger);
                }
            }
            throw new InvalidArgumentsException("Nothing to do.");
        }

        private async Task<int> RunMergeJob(IJobLogger logger)
        {
            try
            {
                await new MergeJob(WorkingCopy, ScriptPath).Run(logger);
                return 0;
            }
            catch (JobScriptException ex)
            {
                logger.Error(ex);
                return 4;
            }
            catch (JobRunnerException ex)
            {
                logger.Error(ex);
                return 5;
            }
        }
    }
}

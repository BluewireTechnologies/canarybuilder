using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bluewire.Common.Console;
using Bluewire.Common.Console.Logging;
using Bluewire.Common.Console.ThirdParty;
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
            var arguments = new Arguments();
            var options = new OptionSet
            {
                { "diagnostics", "Show diagnostics and environment information.", o => arguments.ShowDiagnostics = true }
            };

            var session = new ConsoleSession<Arguments>(arguments, options)
            {
                ListParameterUsage = "[merge <script path> <working copy>]"
            };

            return session.Run(args, async a => await new Program(a).Run());
        }

        private readonly Arguments arguments;

        private Program(Arguments arguments)
        {
            this.arguments = arguments;
            Log.Configure();
            Log.SetConsoleVerbosity(arguments.Verbosity);
        }

        private async Task<int> Run()
        {
            if (arguments.ShowDiagnostics)
            {
                return await new Diagnostics().Run(Console.Out);
            }
            if (arguments.Mode == "merge")
            {
                if (String.IsNullOrWhiteSpace(arguments.ScriptPath)) throw new InvalidArgumentsException("No script path specified for 'merge' mode.");
                if (String.IsNullOrWhiteSpace(arguments.WorkingCopy)) throw new InvalidArgumentsException("No working copy specified for 'merge' mode.");
                
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
                await new MergeJob(arguments.WorkingCopy, arguments.ScriptPath).Run(logger);
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

        class Arguments : IVerbosityArgument, IArgumentList
        {
            private readonly IEnumerable<Level> logLevels = new List<Level> { Level.Warn, Level.Info, Level.Debug };
            private int logLevel = 1;
            private readonly List<string> argumentList = new List<string>();

            public Level Verbosity
            {
                get { return logLevels.ElementAtOrDefault(logLevel) ?? Level.All; }
            }
            
            public void Verbose()
            {
                logLevel++;
            }

            public bool ShowDiagnostics { get; set; }
            
            public string Mode => argumentList.ElementAtOrDefault(0);
            public string ScriptPath => argumentList.ElementAtOrDefault(1);
            public string WorkingCopy => argumentList.ElementAtOrDefault(2);

            IList<string> IArgumentList.ArgumentList => argumentList;
        }
    }
}

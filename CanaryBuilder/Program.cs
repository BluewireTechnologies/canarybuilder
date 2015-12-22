using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bluewire.Common.Console;
using Bluewire.Common.Console.Logging;
using Bluewire.Common.Console.ThirdParty;
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

            var session = new ConsoleSession<Arguments>(arguments, options);

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
            return 0;
        }
        
        class Arguments : IVerbosityArgument
        {
            readonly IEnumerable<Level> logLevels = new List<Level> { Level.Warn, Level.Info, Level.Debug };
            int logLevel;

            public Level Verbosity
            {
                get { return logLevels.ElementAtOrDefault(logLevel) ?? Level.All; }
            }
            
            public void Verbose()
            {
                logLevel++;
            }

            public bool ShowDiagnostics { get; set; }
        }
    }
}

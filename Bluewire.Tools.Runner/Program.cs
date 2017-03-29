using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Bluewire.Tools.Runner
{
    class Program
    {
        public static int Main(string[] args)
        {
            var executableName = Environment.GetCommandLineArgs().FirstOrDefault();

            if (!String.IsNullOrWhiteSpace(executableName))
            {
                var fileNamePart = Path.GetFileNameWithoutExtension(executableName);
                var toolFromExeName = GetToolByName(fileNamePart);
                if (toolFromExeName != null) return toolFromExeName.RunMain(args, "");
            }

            var arguments = GetToolNameFromArguments(args);
            if (arguments != null)
            {
                var toolFromArgument = GetToolByName(arguments.ToolName);
                if (toolFromArgument != null) return toolFromArgument.RunMain(arguments.ChildArguments, $" --tool {arguments.ToolName}");

                Console.Error.WriteLine($"Not a recognised tool name: {arguments.ToolName}");
                return 252;
            }

            Console.Error.WriteLine($"The executable name '{executableName ?? "<unknown>"} is not a recognised tool name and no '--tool <name>' option was specified.");
            ListTools(Console.Error);
            return 253;
        }

        class Arguments
        {
            public string[] ChildArguments { get; set; }
            public string ToolName { get; set; }
        }

        private static Arguments GetToolNameFromArguments(string[] args)
        {
            var spare = new List<string>();
            string toolName = null;
            using (var iterator = args.AsEnumerable().GetEnumerator())
            {
                while (iterator.MoveNext())
                {
                    if (iterator.Current == "--tool")
                    {
                        if (!iterator.MoveNext()) return null;
                        toolName = iterator.Current;
                        break;
                    }
                    if (iterator.Current.StartsWith("--tool="))
                    {
                        toolName = iterator.Current.Substring("--tool=".Length).Trim();
                        break;
                    }
                    spare.Add(iterator.Current);
                }
                while (iterator.MoveNext())
                {
                    spare.Add(iterator.Current);
                }
            }
            if (toolName == null) return null;
            return new Arguments
            {
                ChildArguments = spare.ToArray(),
                ToolName = toolName
            };
        }

        private static void ListTools(TextWriter writer)
        {
            writer.WriteLine("Tools:");
            foreach (var tool in AllTools())
            {
                tool.Describe(writer);
            }
            writer.WriteLine("Use '--tool <name> --help' for more detailed information.");
        }

        private static IList<IToolRunner> AllTools()
        {
            var tools = new List<IToolRunner>
            {
                new FindBuild.ToolRunner(),
                new FindCommits.ToolRunner(),
                new FindTickets.ToolRunner()
            };

            var generateScripts = new GenerateScripts.ToolRunner(tools.Select(t => t.Name).ToArray());
            tools.Add(generateScripts);
            return tools;
        }

        private static IToolRunner GetToolByName(string name)
        {
            if (String.IsNullOrWhiteSpace(name)) return null;
            return AllTools().FirstOrDefault(t => StringComparer.OrdinalIgnoreCase.Equals(t.Name, name.Trim()));
        }
    }
}

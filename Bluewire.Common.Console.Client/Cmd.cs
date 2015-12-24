using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bluewire.Common.Console.Client
{
    public static class Cmd
    {
        private static readonly string CMD_EXE = Path.Combine(Environment.SystemDirectory, "cmd.exe");

        public static bool Exists => File.Exists(CMD_EXE);

        public static string GetExecutableFilePath()
        {
            if (!Exists) throw new FileNotFoundException($"System command interpreter was not found in the expected location: {CMD_EXE}", CMD_EXE);
            return CMD_EXE;
        }
    }
}

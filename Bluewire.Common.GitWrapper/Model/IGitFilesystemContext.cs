using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bluewire.Common.Console.Client.Shell;

namespace Bluewire.Common.GitWrapper.Model
{
    public interface IGitFilesystemContext
    {
        IConsoleProcess Invoke(CommandLine cmd);
    }
}

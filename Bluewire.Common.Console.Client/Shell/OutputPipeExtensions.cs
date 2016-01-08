using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;

namespace Bluewire.Common.Console.Client.Shell
{
    public static class OutputPipeExtensions
    {
        public static async Task<string[]> ReadAllLinesAsync(this IOutputPipe pipe)
        {
            return await pipe.ToArray().ToTask();
        }

        public static async Task<string> ToStringAsync(this IOutputPipe pipe)
        {
            return String.Join(Environment.NewLine, await ReadAllLinesAsync(pipe)) + Environment.NewLine;
        }
    }
}
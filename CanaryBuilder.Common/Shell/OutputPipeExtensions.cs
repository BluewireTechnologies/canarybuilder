using System;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace CanaryBuilder.Common.Shell
{
    public static class OutputPipeExtensions
    {
        public static async Task<string[]> ReadAllLinesAsync(this IOutputPipe pipe)
        {
            return await pipe.ToArray().SingleAsync();
        }

        public static async Task<string> ToStringAsync(this IOutputPipe pipe)
        {
            return String.Join(Environment.NewLine, await ReadAllLinesAsync(pipe)) + Environment.NewLine;
        }
    }
}
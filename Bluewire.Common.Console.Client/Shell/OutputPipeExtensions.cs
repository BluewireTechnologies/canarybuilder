using System;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Bluewire.Common.Console.Client.Shell
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

        /// <summary>
        /// Synonym of StopBuffering() which doesn't cause R#/VS to prompt the developer to await the result.
        /// </summary>
        /// <param name="pipe"></param>
        public static void Discard(this IOutputPipe pipe)
        {
            pipe.StopBuffering();
        }
    }
}
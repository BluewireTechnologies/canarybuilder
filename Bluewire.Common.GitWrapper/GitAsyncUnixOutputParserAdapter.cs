using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bluewire.Common.GitWrapper
{
    /// <summary>
    /// Parse output from Git, treating only LF as the line-break character.
    /// </summary>
    public class GitAsyncUnixOutputParserAdapter
    {
        private readonly Encoding encoding;

        public GitAsyncUnixOutputParserAdapter(Encoding encoding)
        {
            this.encoding = encoding;
        }

        public async IAsyncEnumerator<string> Parse(Stream stream, CancellationToken token)
        {
            var buffer = new char[4096];
            var line = new StringBuilder();
            using (var reader = new StreamReader(stream, encoding))
            {
                while (!token.IsCancellationRequested)
                {
                    var count = await reader.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                    if (count == 0) break;  // EOS

                    for (var i = 0; i < count; i++)
                    {
                        var c = buffer[i];
                        // Only treat line feeds as newline characters.
                        if (c == '\n')
                        {
                            yield return line.ToString();
                            line.Clear();
                            continue;
                        }
                        line.Append(c);
                    }
                }
                if (line.Length > 0) yield return line.ToString();
            }
            token.ThrowIfCancellationRequested();
        }

        public async Task CollectLines(Stream stream, Action<string> collectLine, CancellationToken token)
        {
            var enumerator = Parse(stream, token);
            await using (enumerator.ConfigureAwait(false))
            {
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    collectLine(enumerator.Current);
                }
            }
        }
    }
}

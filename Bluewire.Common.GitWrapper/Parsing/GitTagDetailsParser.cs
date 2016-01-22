using System;
using System.Collections.Generic;
using System.Linq;
using Bluewire.Common.GitWrapper.Model;

namespace Bluewire.Common.GitWrapper.Parsing
{
    /// <summary>
    /// Parser for the output of 'git cat-file tag'.
    /// </summary>
    /// <remarks>
    /// Instances of this class are not threadsafe.
    /// </remarks>
    public class GitTagDetailsParser
    {
        private readonly List<UnexpectedGitOutputFormatDetails> errors = new List<UnexpectedGitOutputFormatDetails>();
        public IEnumerable<UnexpectedGitOutputFormatDetails> Errors => errors;

        public TagDetails Parse(IEnumerable<string> lines)
        {
            var details = new TagDetails();
            using (var line = lines.GetEnumerator())
            {
                while (line.MoveNext())
                {
                    string matched;
                    if (TryParseHeader("object ", line.Current, out matched))
                    {
                        if (details.ResolvedRef == null) details.ResolvedRef = new Ref(matched.Trim());
                        else errors.Add(new UnexpectedGitOutputFormatDetails { Line = line.Current, Explanations = { "Duplicate 'object' header." } });
                    }
                    else if (TryParseHeader("tag ", line.Current, out matched))
                    {
                        if (details.Name == null)
                        {
                            details.Name = matched.Trim();
                            details.Ref = RefHelper.PutInHierarchy("tags", new Ref(matched.Trim()));
                        }
                        else errors.Add(new UnexpectedGitOutputFormatDetails { Line = line.Current, Explanations = { "Duplicate 'tag' header." } });
                    }
                    else if (String.IsNullOrWhiteSpace(line.Current))
                    {
                        details.Message = String.Concat(ReadMessage(line).ToArray());
                        break;
                    }
                }
            }
            return details;
        }

        private bool TryParseHeader(string header, string line, out string matched)
        {
            if (line.StartsWith(header))
            {
                matched = line.Substring(header.Length);
                return true;
            }
            matched = null;
            return false;
        }

        private static IEnumerable<string> ReadMessage(IEnumerator<string> line)
        {
            while (line.MoveNext())
            {
                yield return line.Current;
                yield return Environment.NewLine;
            }
        }
    }
}

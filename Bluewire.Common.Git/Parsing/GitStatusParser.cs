using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Bluewire.Common.Git.Model;

namespace Bluewire.Common.Git.Parsing
{
    /// <summary>
    /// Parser for the output of 'git status --porcelain'.
    /// </summary>
    /// <remarks>
    /// Instances of this class are not threadsafe.
    /// </remarks>
    public class GitStatusParser
    {
        private readonly List<UnexpectedGitOutputFormatDetails> errors = new List<UnexpectedGitOutputFormatDetails>();
        private readonly ThreadLocal<List<char>> reusableBuffer = new ThreadLocal<List<char>>(() => new List<char>(50));

        public IEnumerable<UnexpectedGitOutputFormatDetails> Errors => errors;

        public bool Parse(string line, out GitStatusEntry entry)
        {
            if (line == null) throw new ArgumentNullException(nameof(line));
            
            // Two status flags, whitespace, one-character filename: shortest line we expect is 4 characters.
            if (line.Length < 4)
            {
                entry = null;
                return false;
            }

            var lineParser = new LineParser(line, reusableBuffer.Value);
            
            var isSuccess = lineParser.Parse();
            if(lineParser.Error.Explanations.Any())
            {
                errors.Add(lineParser.Error);
            }
            if(isSuccess)
            {
                entry = lineParser.Result;
                return true;
            }
            entry = null;
            return false;
        }

        public GitStatusEntry ParseOrNull(string line)
        {
            GitStatusEntry entry;
            if(Parse(line, out entry)) return entry;
            return null;
        }
        
        class LineParser
        {
            private readonly string line;
            private readonly List<char> buffer;

            public UnexpectedGitOutputFormatDetails Error {get; }
            public GitStatusEntry Result { get; } = new GitStatusEntry();

            public LineParser(string line, List<char> buffer)
            {
                this.line = line;
                this.buffer = buffer;
                Error = new UnexpectedGitOutputFormatDetails { Line = line };
            }

            public bool Parse()
            {
                using (var iterator = line.GetEnumerator())
                {
                    iterator.MoveNext();
                    Result.IndexState = ParseIndexState(iterator.Current);

                    iterator.MoveNext();
                    Result.WorkTreeState = ParseWorkTreeState(iterator.Current);

                    iterator.MoveNext();
                    if(!Char.IsWhiteSpace(iterator.Current)) Error.Explanations.Add("Expected third character to be whitespace.");

                    string path;
                    if(!ReadMaybeQuotedString(iterator, out path)) return FailImmediately();
                    if (String.IsNullOrEmpty(path)) return FailImmediately("No pathname found after state flags.");
                    Result.Path = path;

                    if(ExpectsSecondPath(Result.IndexState))
                    {
                        string separator;
                        if(!DiscardWhitespace(iterator)) return FailImmediately($"Expected second path, for index state '{Result.IndexState}'");
                        if(!ReadUnquotedString(iterator, out separator)) return FailImmediately();
                        if(separator != "->") return FailImmediately("Expected '->' between paths.");

                        string newPath;
                        if(!ReadMaybeQuotedString(iterator, out newPath)) return FailImmediately();
                        if (String.IsNullOrEmpty(newPath)) return FailImmediately("No pathname found after '->'.");
                        Result.NewPath = newPath;
                    }
                    else
                    {
                        var remainder = ConsumeRemainder(iterator);
                        if(!String.IsNullOrWhiteSpace(remainder))
                        {
                            return FailImmediately("Expected pathname to be the last thing on the line, but found: {remainder}");
                        }
                    }

                    // If any errors were recorded, report failure.
                    if(Error.Explanations.Any()) return false;
                }
                AssertValidResult();
                return true;
            }

            private void AssertValidResult()
            {
                Debug.Assert(Result.IndexState != IndexState.Unknown);
                Debug.Assert(Result.WorkTreeState != WorkTreeState.Unknown);
                Debug.Assert(!String.IsNullOrEmpty(Result.Path));
            }

            private bool ReadMaybeQuotedString(IEnumerator<char> iterator, out string value)
            {
                // Expect iterator to be on the character before the first one to scan.
                
                // Skip leading whitespace.
                if (!DiscardWhitespace(iterator))
                {
                    value = null;
                    return true; // Nothing to read.
                }

                // Iterator now points to first non-whitespace character.
                return IsQuote(iterator.Current) ? ReadQuotedString(iterator, out value) : ReadUnquotedString(iterator, out value);
            }

            private static bool DiscardWhitespace(IEnumerator<char> iterator)
            {
                while (iterator.MoveNext())
                {
                    if(!Char.IsWhiteSpace(iterator.Current)) return true;
                }
                return false;
            }

            private string ConsumeRemainder(IEnumerator<char> iterator)
            {
                buffer.Clear();
                while(iterator.MoveNext())
                {
                    buffer.Add(iterator.Current);
                }
                return StringFromBuffer();
            }


            private bool ReadQuotedString(IEnumerator<char> iterator, out string value)
            {
                buffer.Clear();
                // Expect iterator to be on the opening quotes.
                // Leaves iterator on closing quotes, or at EOL.
                var terminator = iterator.Current;
                while (iterator.MoveNext())
                {
                    if(iterator.Current == terminator)
                    {
                        value = StringFromBuffer();
                        return true;
                    }
                    if(iterator.Current == '\\')
                    {
                        if(!iterator.MoveNext())
                        {
                            Error.Explanations.Add($"Incomplete escape sequence after: {new String(buffer.ToArray())}");
                            value = null;
                            return false;
                        }
                        buffer.Add(iterator.Current);
                        continue;
                    }
                    buffer.Add(iterator.Current);
                }
                Error.Explanations.Add($"Missing closing quotes after: {new String(buffer.ToArray())}");
                value = null;
                return false;
            }
            
            private bool ReadUnquotedString(IEnumerator<char> iterator, out string value)
            {
                buffer.Clear();
                // Expect iterator to be on the first character.
                // Leaves iterator on whitespace after string, or at EOL.
                while (!Char.IsWhiteSpace(iterator.Current))
                {
                    buffer.Add(iterator.Current);
                    if(iterator.Current == '\\')
                    {
                        if(!iterator.MoveNext())
                        {
                            Error.Explanations.Add($"Incomplete escape sequence after: {new String(buffer.ToArray())}");
                            value = null;
                            return false;
                        }
                        buffer.Add(iterator.Current);
                    }
                    if(!iterator.MoveNext()) break;
                }
                value = StringFromBuffer();
                return true;
            }

            private bool FailImmediately(string error)
            {
                Error.Explanations.Add(error);
                return false;
            }

            private bool FailImmediately()
            {
                // Verify that we have recorded some errors.
                Debug.Assert(Error.Explanations.Any());
                return false;
            }
            private string StringFromBuffer() => new string(buffer.ToArray());

            private static bool IsQuote(char c) => c == '"';
            

            private static bool ExpectsSecondPath(IndexState indexState) => indexState == IndexState.Copied || indexState == IndexState.Renamed;

            private IndexState ParseIndexState(char flag)
            {
                switch (flag)
                {
                    case ' ': return IndexState.Unmodified;
                    case 'M': return IndexState.Modified;
                    case 'A': return IndexState.Added;
                    case 'D': return IndexState.Deleted;
                    case 'R': return IndexState.Renamed;
                    case 'C': return IndexState.Copied;
                    case 'U': return IndexState.UpdatedButUnmerged;
                    case '?': return IndexState.Untracked;
                    case '!': return IndexState.Ignored;
                }
                Error.Explanations.Add($"Index state '{flag}' was not recognised.");
                return default(IndexState);
            }

        
            private WorkTreeState ParseWorkTreeState(char flag)
            {
                switch (flag)
                {
                    case ' ': return WorkTreeState.Unmodified;
                    case 'M': return WorkTreeState.Modified;
                    case 'D': return WorkTreeState.Deleted;
                    case 'U': return WorkTreeState.UpdatedButUnmerged;
                    case '?': return WorkTreeState.Untracked;
                    case '!': return WorkTreeState.Ignored;
                }
                Error.Explanations.Add($"Worktree state '{flag}' was not recognised.");
                return default(WorkTreeState);
            }

        }
    }
}

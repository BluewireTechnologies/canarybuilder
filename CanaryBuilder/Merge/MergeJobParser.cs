using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Bluewire.Common.GitWrapper.Model;
using CanaryBuilder.Parsers;
using CliWrap;

namespace CanaryBuilder.Merge
{
    public class MergeJobParser
    {
        public MergeJobDefinition ParseAndValidate(TextReader reader)
        {
            var definition = Parse(reader);
            Validate(definition);
            return definition;
        }

        public MergeJobDefinition Parse(TextReader reader)
        {
            var definition = new MergeJobDefinition();
            IWorkingCopyVerifier currentMergeVerifier = null;
            foreach (var line in new ScriptReader().EnumerateLines(reader))
            {
                try
                {
                    ScriptLine matched;
                    if (MatchesDirective(line, "start at", out matched))
                    {
                        if (definition.Base != null) throw new DuplicateDirectiveException(line, "start at");
                        definition.Base = ConsumeLeadingRefName(ref matched);
                        ExpectNoRemainingCharacters(matched);
                    }
                    else if (MatchesDirective(line, "produce branch", out matched))
                    {
                        if (definition.FinalBranch != null) throw new DuplicateDirectiveException(line, "produce branch");
                        definition.FinalBranch = ConsumeLeadingRefName(ref matched);
                        ExpectNoRemainingCharacters(matched);
                    }
                    else if (MatchesDirective(line, "produce tag", out matched))
                    {
                        if (definition.FinalTag != null) throw new DuplicateDirectiveException(line, "produce tag");
                        definition.FinalTag = ConsumeLeadingRefName(ref matched);
                        ExpectNoRemainingCharacters(matched);
                    }
                    else if (MatchesDirective(line, "merge", out matched))
                    {
                        var mergeRef = ConsumeLeadingRefName(ref matched);
                        definition.Merges.Add(new MergeCandidate(mergeRef) { Verifier = currentMergeVerifier });
                        ExpectNoRemainingCharacters(matched);
                    }
                    else if (MatchesDirective(line, "verify with", out matched))
                    {
                        if (definition.Verifier != null) throw new DuplicateDirectiveException(line, "verify with");
                        var commandLine = ConsumeCommandLine(ref matched);
                        if (commandLine == null) throw new MissingParameterException(matched, "No verification command was specified.");
                        definition.Verifier = new CommandLineWorkingCopyVerifier(commandLine);
                        ExpectNoRemainingCharacters(matched);
                    }
                    else if (MatchesDirective(line, "verify merges with", out matched))
                    {
                        var commandLine = ConsumeCommandLine(ref matched);
                        if (commandLine == null) throw new MissingParameterException(matched, "No verification command was specified.");
                        currentMergeVerifier = new CommandLineWorkingCopyVerifier(commandLine);
                        ExpectNoRemainingCharacters(matched);
                    }
                    else
                    {
                        throw new JobScriptSyntaxErrorException(line, "Unrecognised directive.");
                    }
                }
                catch (Exception ex) when(!(ex is JobScriptException))
                {
                    throw new JobScriptSyntaxErrorException(line, ex);
                }
            }
            return definition;
        }

        public void Validate(MergeJobDefinition definition)
        {
            if (definition.Base == null) throw new MissingParameterException("No starting ref was specified.");
        }

        private void ExpectNoRemainingCharacters(ScriptLine line)
        {
            if (String.IsNullOrWhiteSpace(line.Content)) return;

            throw new JobScriptSyntaxErrorException(line, $"Trailing characters on line: '{line.Content}'");
        }

        private Command ConsumeCommandLine(ref ScriptLine line)
        {
            var programPath = ConsumeLeadingMaybeQuotedString(ref line);
            if (String.IsNullOrWhiteSpace(programPath)) return null;

            var arguments = new List<string>();
            while (line.Content.Length > 0)
            {
                arguments.Add(ConsumeLeadingMaybeQuotedString(ref line));
            }
            return new Command(programPath).WithArguments(arguments);
        }


        private string ConsumeLeadingMaybeQuotedString(ref ScriptLine line)
        {
            if (line.Content.StartsWith("\""))
            {
                return ConsumeLeadingQuotedString(ref line);
            }
            return ConsumeLeadingUnquotedString(ref line);
        }

        private string ConsumeLeadingQuotedString(ref ScriptLine line)
        {
            var length = FindLengthOfQuotedString(line.Content);
            if (length < 0)
            {
                throw new JobScriptSyntaxErrorException(line, $"Unterminated quoted string: '{line.Content}'");
            }
            var value = line.Content.Substring(0, length);
            if (value == line.Content)
            {
                line.Content = "";
            }
            else
            {
                var remainder = line.Content.Substring(length);
                if (!char.IsWhiteSpace(remainder[0]))
                {
                    throw new JobScriptSyntaxErrorException(line, $"No whitespace after quoted string: '{line.Content}'");
                }
                line.Content = remainder.TrimStart();
            }
            return Unquote(value);
        }

        private string Unquote(string str)
        {
            return str.Substring(1, str.Length - 2);
        }

        private int FindLengthOfQuotedString(string line)
        {
            for (var pos = 1; pos < line.Length; pos++)
            {
                if (line[pos] == '"')
                {
                    return pos + 1;
                }
            }
            return -1;
        }

        private static string ConsumeLeadingUnquotedString(ref ScriptLine line)
        {
            var m = Regex.Match(line.Content, @"\s");
            if (m.Success)
            {
                var value = line.Content.Substring(0, m.Index);
                line.Content = line.Content.Substring(m.Index).TrimStart();
                return value;
            }
            else
            {
                var value = line.Content;
                line.Content = "";
                return value;
            }
        }

        private Ref ConsumeLeadingRefName(ref ScriptLine line)
        {
            return new Ref(ConsumeLeadingUnquotedString(ref line));
        }


        private bool MatchesDirective(ScriptLine line, string directive, out ScriptLine remainder)
        {
            remainder = line;
            var trimmed = line.Content.TrimStart();
            if (!trimmed.StartsWith(directive, StringComparison.OrdinalIgnoreCase)) return false;

            remainder.Content = trimmed.Substring(directive.Length)
                    .TrimStart(':') // Optional colon following the directive.
                    .TrimStart();   // Remove any additional leading whitespace.
            return true;
        }
    }
}

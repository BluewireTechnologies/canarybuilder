using System;
using System.IO;
using System.Text.RegularExpressions;
using CanaryBuilder.Common.Git.Model;
using CanaryBuilder.Parsers;

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
                        definition.Merges.Add(new MergeCandidate(mergeRef));
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

        private Ref ConsumeLeadingRefName(ref ScriptLine line)
        {
            var m = Regex.Match(line.Content, @"\s");
            if (m.Success)
            {
                var @ref = new Ref(line.Content.Substring(0, m.Index));
                line.Content = line.Content.Substring(m.Index).TrimStart();
                return @ref;
            }
            else
            {
                var @ref = new Ref(line.Content);
                line.Content = "";
                return @ref;
            }
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

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Management.Automation.Language;

namespace Bluewire.RepositoryLinter;

/// <summary>
/// Tries to expand PowerShell string expressions.
/// </summary>
public class StringExpandableTokenEvaluator
{
    private readonly ImmutableDictionary<string, string> variableValues;

    public StringExpandableTokenEvaluator(ImmutableDictionary<string, string> variableValues)
    {
        this.variableValues = variableValues;
    }

    public bool TryEvaluate(StringExpandableToken token, out string text)
    {
        text = token.Value;
        if (token.NestedTokens?.Any() != true) return true;

        var baseOffset = token.Extent.StartOffset;
        // We need to be able to find the start of the value within the extent.
        var valueOffset = token.Text.IndexOf(token.Value, StringComparison.Ordinal);
        if (valueOffset < 0) return false;
        baseOffset += valueOffset;

        var replacements = new List<Replacement>();
        foreach (var nested in token.NestedTokens.OrderByDescending(x => x.Extent.StartOffset))
        {
            if (nested is VariableToken variableToken)
            {
                // If a variable's value is unknown, give up.
                if (!variableValues.TryGetValue(variableToken.Name, out var value)) return false;
                replacements.Add(new Replacement { Extent = variableToken.Extent, Value = value });
            }
            else if (nested is StringExpandableToken stringExpandableToken)
            {
                if (!TryEvaluate(stringExpandableToken, out var value)) return false;
                replacements.Add(new Replacement { Extent = stringExpandableToken.Extent, Value = value });
            }
            else
            {
                return false;
            }
        }

        // Go backwards, so offsets remain valid.
        foreach (var replacement in replacements.OrderByDescending(x => x.Extent.StartOffset))
        {
            var start = replacement.Extent.StartOffset - baseOffset;
            var end = replacement.Extent.EndOffset - baseOffset;
            text = text[..start] + replacement.Value + text[end..];
        }

        return true;
    }

    struct Replacement
    {
        public IScriptExtent Extent { get; init; }
        public string Value { get; init; }
    }
}

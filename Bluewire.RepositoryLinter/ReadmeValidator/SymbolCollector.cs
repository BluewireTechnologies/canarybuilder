using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Octostache.Templates;

namespace Bluewire.RepositoryLinter.ReadmeValidator
{
    /// <summary>
    /// Reflectively collect all symbols in a template token.
    /// </summary>
    /// <remarks>
    /// All of the useful types are internal to Octostache, therefore we must use reflection for this.
    /// </remarks>
    internal class SymbolCollector
    {
        private readonly Type expressionBaseType = ResolveType("Octostache.Templates.ContentExpression");
        private readonly Type symbolExpressionType = ResolveType("Octostache.Templates.SymbolExpression");

        private static Type ResolveType(string name)
        {
            return typeof(TemplateToken).Assembly.GetType(name) ?? throw new InvalidOperationException($"Unable to resolve type: {name}");
        }

        public IEnumerable<string> Collect(TemplateToken token)
        {
            foreach (var property in token.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (expressionBaseType.IsAssignableFrom(property.PropertyType))
                {
                    foreach (var symbol in CollectSymbols(property.GetValue(token)))
                    {
                        yield return symbol;
                    }
                }
                if (typeof(TemplateToken).IsAssignableFrom(property.PropertyType))
                {
                    var childToken = (TemplateToken?)property.GetValue(token);
                    if (childToken == null) continue;
                    foreach (var symbol in Collect(childToken))
                    {
                        yield return symbol;
                    }
                }
                if (typeof(IEnumerable<TemplateToken>).IsAssignableFrom(property.PropertyType))
                {
                    var childTokens = (IEnumerable<TemplateToken>?)property.GetValue(token);
                    if (childTokens == null) continue;
                    foreach (var symbol in childTokens.SelectMany(Collect))
                    {
                        yield return symbol;
                    }
                }
            }
        }

        private IEnumerable<string> CollectSymbols(object? expression)
        {
            if (expression == null) yield break;
            if (symbolExpressionType.IsInstanceOfType(expression))
            {
                var symbol = expression.ToString();
                if (symbol != null) yield return symbol;
            }
        }
    }
}

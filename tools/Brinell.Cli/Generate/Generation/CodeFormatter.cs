using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;

namespace Brinell.Generator.Generation;

/// <summary>
/// Formats generated code using Roslyn's CSharp formatter.
/// </summary>
public class CodeFormatter
{
    /// <summary>
    /// Formats code string using Roslyn's formatting rules.
    /// </summary>
    /// <param name="code">The code string to format.</param>
    /// <returns>Formatted code string.</returns>
    public string Format(string code)
    {
        try
        {
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetRoot();
            var workspace = new AdhocWorkspace();
            var formattedNode = Formatter.Format(root, workspace);
            return formattedNode.ToFullString();
        }
        catch
        {
            // If formatting fails, return the original code
            return code;
        }
    }
}

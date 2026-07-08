using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Brinell.Generator.Analysis;

/// <summary>
/// Analyzes a class for property patterns (Is/Wait/Assert) and returns groups for code generation.
/// </summary>
public class PropertyMethodAnalyzer
{
    private readonly List<IMethodHandler> _handlers;

    public PropertyMethodAnalyzer(IEnumerable<IMethodHandler>? handlers = null)
    {
        _handlers = handlers?.ToList() ?? new List<IMethodHandler>
        {
            new Handlers.IsPropertyHandler()
        };
    }

    /// <summary>
    /// Analyzes a class for property patterns and returns Is/Wait/Assert method groups to generate.
    /// </summary>
    public List<Models.PropertyMethodGroup> Analyze(ClassDeclarationSyntax classDecl)
    {
        var groups = new List<Models.PropertyMethodGroup>();

        foreach (var method in classDecl.Members.OfType<MethodDeclarationSyntax>())
        {
            foreach (var handler in _handlers)
            {
                if (handler.Matches(method))
                {
                    var coreMethod = handler.Extract(method);

                    groups.Add(new Models.PropertyMethodGroup
                    {
                        CoreMethod = coreMethod,
                        Handler = handler
                    });
                    break;
                }
            }
        }

        return groups;
    }
}

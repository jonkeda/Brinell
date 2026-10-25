using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Bruinell.Html.Bunit;

/// <summary>
/// Whether a component serves an address: the <c>@page</c> templates of the assemblies the rendered router reads. A
/// template's constraints are not checked, so an address a constraint refuses counts as served.
/// </summary>
internal static class BunitRoutes
{
    /// <summary>
    /// True when a component in <paramref name="root"/>'s router serves <paramref name="path"/> (relative to the base, its
    /// query and fragment ignored), or when the root has no router and renders the same component at every address.
    /// </summary>
    public static bool Serves(IRenderedComponent<IComponent> root, string path)
    {
        var router = root.FindComponents<Router>().FirstOrDefault()?.Instance;
        if (router is null)
        {
            return true;
        }

        var segments = Segments(path.Split('?', '#')[0]);
        return new[] { router.AppAssembly }
            .Concat(router.AdditionalAssemblies ?? [])
            .Where(assembly => assembly is not null)
            .Distinct()
            .SelectMany(assembly => assembly.ExportedTypes)
            .Where(type => typeof(IComponent).IsAssignableFrom(type))
            .SelectMany(type => type.GetCustomAttributes<RouteAttribute>())
            .Any(route => Matches(Segments(route.Template), segments));
    }

    private static string[] Segments(string path) => path.Split('/', StringSplitOptions.RemoveEmptyEntries);

    private static bool Matches(string[] template, string[] path)
    {
        for (var i = 0; i < template.Length; i++)
        {
            var part = template[i];
            if (part.StartsWith("{*", StringComparison.Ordinal))
            {
                return true;
            }

            if (i >= path.Length)
            {
                // Only optional parameters may be missing, and only at the end.
                return template.Skip(i).All(rest => rest.StartsWith('{') && rest.EndsWith("?}", StringComparison.Ordinal));
            }

            var parameter = part.StartsWith('{') && part.EndsWith('}');
            if (!parameter && !string.Equals(part, Uri.UnescapeDataString(path[i]), StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return template.Length == path.Length;
    }
}

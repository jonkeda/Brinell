namespace Brinell.Uat;

/// <summary>
/// Phrase namespaces: a namespace is a folder under the scenario root, written with <c>/</c>
/// (<c>Invoicing/Credit notes</c>); the root is <c>""</c>. A phrase of a namespace is visible from
/// that folder and every folder below it; a phrase of the root, from everywhere.
/// </summary>
public static class UatNamespaces
{
    /// <summary>A namespace as it is compared: <c>/</c> between folders, no empty parts, <c>""</c> for the root.</summary>
    /// <param name="name">A namespace or a relative folder, with <c>/</c> or <c>\</c>; null for the root.</param>
    public static string Normalize(string? name)
        => string.Join('/', (name ?? string.Empty)
            .Replace('\\', '/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(part => part != "."));

    /// <summary>Whether a phrase of <paramref name="owner"/> is visible from <paramref name="from"/>.</summary>
    /// <param name="owner">The phrase's namespace; null or <c>""</c> for the root.</param>
    /// <param name="from">The namespace of the file that uses it.</param>
    public static bool IsVisible(string? owner, string? from)
    {
        var ownerName = Normalize(owner);
        if (ownerName.Length == 0)
        {
            return true;
        }

        var fromName = Normalize(from);
        return fromName.Equals(ownerName, StringComparison.OrdinalIgnoreCase) ||
               fromName.StartsWith(ownerName + "/", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>The namespace of a scenario or phrase file: its folder, relative to the scenario root.</summary>
    /// <param name="filePath">The file; null or a path without a folder gives the root.</param>
    public static string Of(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return string.Empty;
        }

        var folder = Path.GetDirectoryName(Path.GetFullPath(filePath))!;
        return Normalize(Path.GetRelativePath(UatScenarioFiles.ScenarioRoot(filePath), folder));
    }

    /// <summary>A namespace as a message names it.</summary>
    public static string Describe(string? name)
        => Normalize(name) is { Length: > 0 } normalized ? $"the folder '{normalized}'" : "the scenario root";
}

/// <summary>Where a step is written, for what it can see: a namespace, and the file for its own callables.</summary>
/// <param name="Namespace">The folder under the scenario root.</param>
/// <param name="File">The full path of the scenario file; null inside a phrase file's callable.</param>
internal readonly record struct UatScope(string Namespace, string? File)
{
    public static UatScope Of(UatDocument document)
        => document.Source.FilePath is { } path
            ? new(UatNamespaces.Of(path), Path.GetFullPath(path))
            : new(string.Empty, null);

    // A callable's body binds where it is written (D9): its file's own callables, or its phrase
    // file's namespace.
    public static UatScope Of(UatCallable callable) => new(callable.Namespace, callable.LocalTo);

    public bool Sees(UatCallable callable)
        => callable.LocalTo is { } local
            ? File is not null && string.Equals(local, File, StringComparison.OrdinalIgnoreCase)
            : UatNamespaces.IsVisible(callable.Namespace, Namespace);

    public bool Sees(UatCommandPattern pattern)
        => pattern.Callable is { } callable ? Sees(callable) : UatNamespaces.IsVisible(pattern.Namespace, Namespace);
}

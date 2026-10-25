using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Bruinell.Bdd.Workspace;

/// <summary>
/// Where a suite's C# is written, from its assemblies' portable PDBs (beside the assembly or
/// embedded), and what its XML doc says. Go to definition and hover use it; a build without a PDB or
/// an XML doc file, or a deterministic build whose PDB paths are mapped, gives nothing, not an error.
/// </summary>
public static partial class BddSourceLocator
{
    private static readonly Dictionary<string, (DateTime Written, MetadataReaderProvider? Pdb)> Pdbs = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, (DateTime Written, XDocument? Doc)> Docs = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The line a method is declared on: its first sequence point, then up to its name.</summary>
    /// <param name="method">A method of a loaded suite.</param>
    public static BddSourceLocation? Of(MethodBase method)
    {
        ArgumentNullException.ThrowIfNull(method);
        if (FirstSequencePoint(method) is not { } point)
        {
            return null;
        }

        return Declaration(point.File, point.Line, $@"\b{Regex.Escape(method.Name)}\s*[<(]") ?? new BddSourceLocation(point.File, point.Line);
    }

    /// <summary>The line a type is declared on: the file of any of its methods, then its <c>class</c> line.</summary>
    /// <param name="type">A type of a loaded suite.</param>
    public static BddSourceLocation? Of(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        var point = Members(type).Select(FirstSequencePoint).FirstOrDefault(found => found is not null);
        if (point is not { } found)
        {
            return null;
        }

        var name = type.Name.Split('`')[0];
        return Find(found.File, $@"\b(class|record|struct|interface)\s+{Regex.Escape(name)}\b") ?? new BddSourceLocation(found.File, found.Line);
    }

    /// <summary>The line a member of a type is declared on: a method's own, or a property's, found by its name in the type's file.</summary>
    /// <param name="type">A type of a loaded suite.</param>
    /// <param name="member">The member's name.</param>
    public static BddSourceLocation? Of(Type type, string member)
    {
        ArgumentNullException.ThrowIfNull(type);
        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        if (type.GetProperty(member, flags)?.GetMethod is { } getter && Of(getter) is { } property)
        {
            return property;
        }

        if (type.GetMethods(flags).FirstOrDefault(method => method.Name == member) is { } method && Of(method) is { } declared)
        {
            return declared;
        }

        return Of(type) is { FilePath: { } file } at ? Find(file, $@"\b{Regex.Escape(member)}\b") ?? at : null;
    }

    /// <summary>A member's <c>&lt;summary&gt;</c>, from the XML doc file beside its assembly; null when there is none.</summary>
    /// <param name="member">A method or type of a loaded suite.</param>
    public static string? Summary(MemberInfo member)
    {
        ArgumentNullException.ThrowIfNull(member);
        var type = member as Type ?? member.DeclaringType;
        if (type?.Assembly.Location is not { Length: > 0 } location || Doc(Path.ChangeExtension(location, ".xml")) is not { } doc)
        {
            return null;
        }

        var name = type.FullName!.Replace('+', '.');
        var id = member is Type ? $"T:{name}" : $"M:{name}.{member.Name}";
        var element = doc.Descendants("member").FirstOrDefault(candidate =>
            (string?)candidate.Attribute("name") is { } value && (value == id || value.StartsWith(id + "(", StringComparison.Ordinal)));
        if (element?.Element("summary") is not { } summary)
        {
            return null;
        }

        // <see cref="T:Ns.Type"/> reads as its last name.
        foreach (var see in summary.Descendants().Where(child => child.Name.LocalName is "see" or "paramref" or "typeparamref").ToList())
        {
            var reference = (string?)see.Attribute("cref") ?? (string?)see.Attribute("name") ?? (string?)see.Attribute("langword") ?? string.Empty;
            see.ReplaceWith(reference[(reference.LastIndexOfAny([':', '.']) + 1)..]);
        }

        return Whitespace().Replace(summary.Value, " ").Trim();
    }

    private static IEnumerable<MethodBase> Members(Type type)
    {
        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        return type.GetMethods(flags).Cast<MethodBase>().Concat(type.GetConstructors(flags));
    }

    private static (string File, int Line)? FirstSequencePoint(MethodBase method)
    {
        if (method.Module.Assembly.Location is not { Length: > 0 } location || Pdb(location) is not { } provider)
        {
            return null;
        }

        try
        {
            var reader = provider.GetMetadataReader();
            var handle = MetadataTokens.MethodDefinitionHandle(method.MetadataToken);
            foreach (var point in reader.GetMethodDebugInformation(handle).GetSequencePoints())
            {
                if (!point.IsHidden)
                {
                    // A deterministic (CI) build maps its sources to /_/: no file here to open.
                    var file = reader.GetString(reader.GetDocument(point.Document).Name);
                    return Path.IsPathFullyQualified(file) ? (file, point.StartLine) : null;
                }
            }
        }
        catch (Exception exception) when (exception is BadImageFormatException or ArgumentException or InvalidOperationException)
        {
            // A PDB that does not match the assembly: no source for it.
        }

        return null;
    }

    // Read into memory, so a build never meets the file held open.
    private static MetadataReaderProvider? Pdb(string assemblyPath)
    {
        var written = File.Exists(assemblyPath) ? File.GetLastWriteTimeUtc(assemblyPath) : DateTime.MinValue;
        lock (Pdbs)
        {
            if (Pdbs.TryGetValue(assemblyPath, out var cached) && cached.Written == written)
            {
                return cached.Pdb;
            }

            MetadataReaderProvider? provider = null;
            try
            {
                using var pe = new PEReader(new MemoryStream(File.ReadAllBytes(assemblyPath)));
                pe.TryOpenAssociatedPortablePdb(
                    assemblyPath,
                    path => File.Exists(path) ? new MemoryStream(File.ReadAllBytes(path)) : null,
                    out provider,
                    out _);

                // An assembly renamed after the build: its PDB beside it, by its own name.
                if (provider is null && File.Exists(Path.ChangeExtension(assemblyPath, ".pdb")))
                {
                    provider = MetadataReaderProvider.FromPortablePdbStream(new MemoryStream(File.ReadAllBytes(Path.ChangeExtension(assemblyPath, ".pdb"))));
                }
            }
            catch (Exception exception) when (exception is IOException or BadImageFormatException or InvalidOperationException)
            {
                provider = null;
            }

            Pdbs[assemblyPath] = (written, provider);
            return provider;
        }
    }

    private static XDocument? Doc(string path)
    {
        var written = File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
        lock (Docs)
        {
            if (Docs.TryGetValue(path, out var cached) && cached.Written == written)
            {
                return cached.Doc;
            }

            XDocument? doc = null;
            try
            {
                doc = File.Exists(path) ? XDocument.Load(path) : null;
            }
            catch (Exception exception) when (exception is IOException or System.Xml.XmlException)
            {
                doc = null;
            }

            Docs[path] = (written, doc);
            return doc;
        }
    }

    // A method's first sequence point is in its body: its name is on that line or a few above.
    private static BddSourceLocation? Declaration(string file, int line, string pattern)
    {
        if (!File.Exists(file))
        {
            return null;
        }

        var lines = File.ReadAllLines(file);
        for (var at = Math.Min(line, lines.Length); at >= Math.Max(1, line - 12); at--)
        {
            if (Regex.IsMatch(lines[at - 1], pattern))
            {
                return new BddSourceLocation(file, at);
            }
        }

        return null;
    }

    private static BddSourceLocation? Find(string file, string pattern)
    {
        if (!File.Exists(file))
        {
            return null;
        }

        var lines = File.ReadAllLines(file);
        var index = Array.FindIndex(lines, line => Regex.IsMatch(line, pattern));
        return index < 0 ? null : new BddSourceLocation(file, index + 1);
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}

using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Bruinell.Mocking.Data;

/// <summary>One object of the schema: a table, view, procedure or function, its hash, and what its body references.</summary>
public sealed class SchemaObject
{
    public string Kind { get; set; } = string.Empty;

    public string Hash { get; set; } = string.Empty;

    public List<string> References { get; set; } = [];

    /// <summary>Its body (or a trigger on it) runs SQL built at run time: the references SQL Server reads can't be complete.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Dynamic { get; set; }

    /// <summary>Its references were added by hand (<see cref="SchemaSnapshot.MergeReferences"/>), for dynamic SQL.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Listed { get; set; }
}

/// <summary>What a snapshot reads beyond the schema.</summary>
public sealed class SnapshotOptions
{
    /// <summary>Tables of reference data (<c>dbo.kinds</c>): their rows are hashed with them, so changed rows make stale.</summary>
    public IReadOnlyCollection<string> Data { get; set; } = [];

    /// <summary>The app's user's default schema: a command's unqualified name is taken in it first, then <c>dbo</c>.</summary>
    public string DefaultSchema { get; set; } = "dbo";
}

/// <summary>
/// The schema of a SQL Server database as a file: every table, view, procedure and function by its name
/// (<c>dbo.places</c>), with a hash of its columns, keys and indexes or of its body, and the objects its body references
/// (<c>sys.sql_expression_dependencies</c>). Read-only on the database; the report follows the references to give a test
/// that calls a procedure the tables the procedure reads.
/// </summary>
/// <remarks>
/// A table's hash holds its triggers, defaults and checks, and its references what its triggers' bodies reference. An
/// unqualified name in a body is the object SQL Server bound, else one in the body's own schema, else <c>dbo</c>.
/// Dynamic SQL is marked, its references listed by hand; reference data is hashed for the tables named.
/// </remarks>
public sealed class SchemaSnapshot
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public SortedDictionary<string, SchemaObject> Objects { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The app's user's default schema (<see cref="SnapshotOptions.DefaultSchema"/>).</summary>
    public string DefaultSchema { get; set; } = "dbo";

    public static SchemaSnapshot Load(string path) => JsonSerializer.Deserialize<SchemaSnapshot>(File.ReadAllText(path), Json) ?? new SchemaSnapshot();

    public void Save(string path)
    {
        if (Path.GetDirectoryName(Path.GetFullPath(path)) is { } folder)
        {
            Directory.CreateDirectory(folder);
        }

        File.WriteAllText(path, JsonSerializer.Serialize(this, Json));
    }

    /// <summary>
    /// The snapshot as replay's schema, with the versions that shape the SQL (EF Core's, the provider's): read from the
    /// checked-in file, so a test knows its recordings' staleness without a database.
    /// </summary>
    public Replay.Schema ToSchema(IReadOnlyDictionary<string, string>? versions = null) => new(
        versions ?? new Dictionary<string, string>(StringComparer.Ordinal),
        Objects.ToDictionary(pair => pair.Key, pair => pair.Value.Hash, StringComparer.OrdinalIgnoreCase),
        Objects.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<string>)pair.Value.References, StringComparer.OrdinalIgnoreCase))
    {
        DefaultSchema = DefaultSchema,
    };

    /// <summary>
    /// Adds references kept by hand, for objects whose dynamic SQL SQL Server can't follow: a JSON object of each name and
    /// what it references (<c>{ "dbo.search": ["dbo.places"] }</c>). An object the snapshot doesn't hold is skipped and returned.
    /// </summary>
    public IReadOnlyList<string> MergeReferences(string path)
    {
        var listed = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(File.ReadAllText(path)) ?? [];
        var unknown = new List<string>();
        foreach (var pair in listed)
        {
            if (!Objects.TryGetValue(pair.Key, out var item))
            {
                unknown.Add(pair.Key);
                continue;
            }

            item.References = [.. item.References.Concat(pair.Value.Select(name => name.ToLowerInvariant())).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(name => name, StringComparer.Ordinal)];
            item.Listed = true;
        }

        return unknown;
    }

    /// <summary><paramref name="named"/> and every object their bodies reference, followed through views and procedures.</summary>
    public IReadOnlyCollection<string> Expand(IEnumerable<string> named)
    {
        var touched = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        var waiting = new Queue<string>(named);
        while (waiting.Count > 0)
        {
            var name = waiting.Dequeue();
            if (touched.Add(name) && Objects.TryGetValue(name, out var item))
            {
                foreach (var referenced in item.References)
                {
                    waiting.Enqueue(referenced);
                }
            }
        }

        return touched;
    }

    /// <summary>Reads the schema of the database <paramref name="connection"/> is open on, in one batch.</summary>
    public static Task<SchemaSnapshot> ReadAsync(DbConnection connection, CancellationToken cancellationToken = default)
        => ReadAsync(connection, new SnapshotOptions(), cancellationToken);

    /// <summary>Reads the schema of the database <paramref name="connection"/> is open on, with the reference data <paramref name="options"/> names.</summary>
    public static async Task<SchemaSnapshot> ReadAsync(DbConnection connection, SnapshotOptions options, CancellationToken cancellationToken = default)
    {
        using var command = connection.CreateCommand();
        command.CommandText = Query;
        var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        var kinds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var lines = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var references = new Dictionary<string, SortedSet<string>>(StringComparer.OrdinalIgnoreCase);

        // 1: objects and their kinds. 2 to 7: lines (columns, indexes, foreign keys, bodies, triggers, defaults and checks). 8: references.
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            kinds[reader.GetString(0)] = reader.GetString(1);
        }

        var dynamic = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var set = 2; set <= 7 && await reader.NextResultAsync(cancellationToken).ConfigureAwait(false); set++)
        {
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var name = reader.GetString(0);
                if (!lines.TryGetValue(name, out var list))
                {
                    lines[name] = list = [];
                }

                var line = Statements.Normalize(reader.GetString(1));
                list.Add(line);
                if ((line.StartsWith("body ", StringComparison.Ordinal) || line.StartsWith("trigger ", StringComparison.Ordinal)) && DynamicSql.IsMatch(line))
                {
                    dynamic.Add(name);
                }
            }
        }

        if (await reader.NextResultAsync(cancellationToken).ConfigureAwait(false))
        {
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var name = reader.GetString(0);
                if (!references.TryGetValue(name, out var set))
                {
                    references[name] = set = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
                }

                set.Add(reader.GetString(1));
            }
        }

        reader.Dispose();
        foreach (var table in options.Data)
        {
            var name = table.Contains(".") ? table.ToLowerInvariant() : "dbo." + table.ToLowerInvariant();
            if (!lines.TryGetValue(name, out var list))
            {
                lines[name] = list = [];
            }

            list.Add("data " + await DataHashAsync(connection, name, cancellationToken).ConfigureAwait(false));
        }

        var snapshot = new SchemaSnapshot { DefaultSchema = options.DefaultSchema.ToLowerInvariant() };
        foreach (var pair in kinds)
        {
            snapshot.Objects[pair.Key] = new SchemaObject
            {
                Kind = pair.Value,
                Hash = Hash(lines.TryGetValue(pair.Key, out var list) ? list : []),
                References = references.TryGetValue(pair.Key, out var set) ? [.. set.Where(item => !string.Equals(item, pair.Key, StringComparison.OrdinalIgnoreCase))] : [],
                Dynamic = dynamic.Contains(pair.Key),
            };
        }

        return snapshot;
    }

    private static readonly Regex DynamicSql = new(@"\bsp_executesql\b|\bEXEC(?:UTE)?\s*\(", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // The rows of a reference table: their count and checksum, so a changed lookup row changes the table's hash.
    private static async Task<string> DataHashAsync(DbConnection connection, string table, CancellationToken cancellationToken)
    {
        var quoted = string.Join(".", table.Split('.').Select(part => "[" + part.Replace("]", "]]") + "]"));
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT CAST(COUNT_BIG(*) AS varchar(20)) + ' ' + ISNULL(CAST(CHECKSUM_AGG(BINARY_CHECKSUM(*)) AS varchar(20)), '-') FROM {quoted}";
        return (string)(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false))!;
    }

    private static string Hash(IEnumerable<string> lines)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(string.Join("\n", lines.OrderBy(line => line, StringComparer.Ordinal))));
        return BitConverter.ToString(bytes, 0, 6).Replace("-", string.Empty);
    }

    private const string Query = """
        SELECT LOWER(s.name + '.' + o.name), LOWER(o.type_desc)
        FROM sys.objects o JOIN sys.schemas s ON s.schema_id = o.schema_id
        WHERE o.type IN ('U', 'V', 'P', 'FN', 'IF', 'TF') AND o.is_ms_shipped = 0;

        SELECT LOWER(s.name + '.' + o.name),
            'column ' + c.name + ' ' + t.name + '(' + CAST(c.max_length AS varchar) + ',' + CAST(c.precision AS varchar) + ',' + CAST(c.scale AS varchar) + ')'
            + CASE WHEN c.is_nullable = 1 THEN ' null' ELSE ' not null' END
        FROM sys.objects o JOIN sys.schemas s ON s.schema_id = o.schema_id
            JOIN sys.columns c ON c.object_id = o.object_id JOIN sys.types t ON t.user_type_id = c.user_type_id
        WHERE o.type IN ('U', 'V') AND o.is_ms_shipped = 0;

        SELECT LOWER(s.name + '.' + o.name),
            'index ' + i.name + CASE WHEN i.is_unique = 1 THEN ' unique' ELSE '' END
            + ' (' + STRING_AGG(c.name, ', ') WITHIN GROUP (ORDER BY ic.key_ordinal) + ')'
        FROM sys.indexes i JOIN sys.objects o ON o.object_id = i.object_id JOIN sys.schemas s ON s.schema_id = o.schema_id
            JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
            JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
        WHERE o.type = 'U' AND o.is_ms_shipped = 0 AND i.name IS NOT NULL
        GROUP BY s.name, o.name, i.name, i.is_unique;

        SELECT LOWER(s.name + '.' + o.name), 'fk ' + fk.name + ' -> ' + LOWER(rs.name + '.' + ro.name)
        FROM sys.foreign_keys fk JOIN sys.objects o ON o.object_id = fk.parent_object_id JOIN sys.schemas s ON s.schema_id = o.schema_id
            JOIN sys.objects ro ON ro.object_id = fk.referenced_object_id JOIN sys.schemas rs ON rs.schema_id = ro.schema_id;

        SELECT LOWER(s.name + '.' + o.name), 'body ' + m.definition
        FROM sys.sql_modules m JOIN sys.objects o ON o.object_id = m.object_id JOIN sys.schemas s ON s.schema_id = o.schema_id
        WHERE o.type IN ('V', 'P', 'FN', 'IF', 'TF') AND o.is_ms_shipped = 0;

        SELECT LOWER(s.name + '.' + o.name), 'trigger ' + tr.name + CASE WHEN tr.is_disabled = 1 THEN ' disabled ' ELSE ' ' END + m.definition
        FROM sys.triggers tr JOIN sys.sql_modules m ON m.object_id = tr.object_id
            JOIN sys.objects o ON o.object_id = tr.parent_id JOIN sys.schemas s ON s.schema_id = o.schema_id
        WHERE tr.parent_class = 1 AND o.is_ms_shipped = 0;

        SELECT LOWER(s.name + '.' + o.name), 'default ' + c.name + ' ' + dc.definition
        FROM sys.default_constraints dc JOIN sys.objects o ON o.object_id = dc.parent_object_id JOIN sys.schemas s ON s.schema_id = o.schema_id
            JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
        WHERE o.is_ms_shipped = 0
        UNION ALL
        SELECT LOWER(s.name + '.' + o.name), 'check ' + cc.name + ' ' + cc.definition
        FROM sys.check_constraints cc JOIN sys.objects o ON o.object_id = cc.parent_object_id JOIN sys.schemas s ON s.schema_id = o.schema_id
        WHERE o.is_ms_shipped = 0;

        -- What a module or trigger references: the object SQL Server bound, else one in the referencing object's schema,
        -- else dbo. A trigger's references are its table's.
        SELECT DISTINCT LOWER(s.name + '.' + o.name),
            LOWER(COALESCE(
                rs.name + '.' + ro.name,
                d.referenced_schema_name + '.' + d.referenced_entity_name,
                CASE WHEN OBJECT_ID(QUOTENAME(s.name) + '.' + QUOTENAME(d.referenced_entity_name)) IS NOT NULL THEN s.name ELSE 'dbo' END + '.' + d.referenced_entity_name))
        FROM sys.sql_expression_dependencies d
            JOIN sys.objects r ON r.object_id = d.referencing_id
            JOIN sys.objects o ON o.object_id = CASE WHEN r.type = 'TR' THEN r.parent_object_id ELSE r.object_id END
            JOIN sys.schemas s ON s.schema_id = o.schema_id
            LEFT JOIN sys.objects ro ON ro.object_id = d.referenced_id
            LEFT JOIN sys.schemas rs ON rs.schema_id = ro.schema_id
        WHERE d.referenced_entity_name IS NOT NULL AND d.referenced_server_name IS NULL AND d.referenced_database_name IS NULL
            AND d.referencing_class = 1;
        """;
}

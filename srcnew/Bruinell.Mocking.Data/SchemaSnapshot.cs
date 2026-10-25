using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Bruinell.Mocking.Data;

/// <summary>One object of the schema: a table, view, procedure or function, its hash, and what its body references.</summary>
public sealed class SchemaObject
{
    public string Kind { get; set; } = string.Empty;

    public string Hash { get; set; } = string.Empty;

    public List<string> References { get; set; } = [];
}

/// <summary>
/// The schema of a SQL Server database as a file: every table, view, procedure and function by its name
/// (<c>dbo.places</c>), with a hash of its columns, keys and indexes or of its body, and the objects its body references
/// (<c>sys.sql_expression_dependencies</c>). Read-only on the database; the report follows the references to give a test
/// that calls a procedure the tables the procedure reads.
/// </summary>
public sealed class SchemaSnapshot
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public SortedDictionary<string, SchemaObject> Objects { get; set; } = new(StringComparer.OrdinalIgnoreCase);

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
        Objects.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<string>)pair.Value.References, StringComparer.OrdinalIgnoreCase));

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
    public static async Task<SchemaSnapshot> ReadAsync(DbConnection connection, CancellationToken cancellationToken = default)
    {
        using var command = connection.CreateCommand();
        command.CommandText = Query;
        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        var kinds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var lines = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var references = new Dictionary<string, SortedSet<string>>(StringComparer.OrdinalIgnoreCase);

        // 1: objects and their kinds. 2: columns. 3: indexes. 4: foreign keys. 5: bodies. 6: references.
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            kinds[reader.GetString(0)] = reader.GetString(1);
        }

        for (var set = 2; set <= 5 && await reader.NextResultAsync(cancellationToken).ConfigureAwait(false); set++)
        {
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var name = reader.GetString(0);
                if (!lines.TryGetValue(name, out var list))
                {
                    lines[name] = list = [];
                }

                list.Add(set == 5 ? "body " + Statements.Normalize(reader.GetString(1)) : reader.GetString(1));
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

        var snapshot = new SchemaSnapshot();
        foreach (var pair in kinds)
        {
            snapshot.Objects[pair.Key] = new SchemaObject
            {
                Kind = pair.Value,
                Hash = Hash(lines.TryGetValue(pair.Key, out var list) ? list : []),
                References = references.TryGetValue(pair.Key, out var set) ? [.. set] : [],
            };
        }

        return snapshot;
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

        SELECT LOWER(s.name + '.' + o.name), m.definition
        FROM sys.sql_modules m JOIN sys.objects o ON o.object_id = m.object_id JOIN sys.schemas s ON s.schema_id = o.schema_id
        WHERE o.type IN ('V', 'P', 'FN', 'IF', 'TF') AND o.is_ms_shipped = 0;

        SELECT DISTINCT LOWER(s.name + '.' + o.name), LOWER(COALESCE(d.referenced_schema_name, 'dbo') + '.' + d.referenced_entity_name)
        FROM sys.sql_expression_dependencies d JOIN sys.objects o ON o.object_id = d.referencing_id JOIN sys.schemas s ON s.schema_id = o.schema_id
        WHERE d.referenced_entity_name IS NOT NULL AND d.referenced_server_name IS NULL AND d.referenced_database_name IS NULL;
        """;
}

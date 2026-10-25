using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace Brinell.Sql.Spike.Tests.Spike;

/// <summary>One object of the schema: a table, view, procedure or function, its hash, and what its body references.</summary>
public sealed class SchemaObject
{
    public required string Kind { get; set; }

    public required string Hash { get; set; }

    public List<string> References { get; set; } = [];
}

/// <summary>
/// The schema as a checked-in file (<c>Recordings/schema.json</c>), for a system without an EF model: every table,
/// view, procedure and function by its name (<c>dbo.places</c>), with a hash of its columns, keys and indexes or of its
/// body, and the objects its body references (<c>sys.sql_expression_dependencies</c>). Written from the database when
/// recording; replay reads only the file. A schema change shows as a changed file, and the recordings that touched a
/// changed object are stale.
/// </summary>
public sealed class SchemaSnapshot
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public SortedDictionary<string, SchemaObject> Objects { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public static SchemaSnapshot? Load(string path) => File.Exists(path) ? JsonSerializer.Deserialize<SchemaSnapshot>(File.ReadAllText(path), Json) : null;

    public void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(this, Json));
    }

    /// <summary>The schema for stamps, with the versions that shape the SQL.</summary>
    public Schema ToSchema(string ef, string sqlClient) => new(
        ef,
        sqlClient,
        Objects.ToDictionary(pair => pair.Key, pair => pair.Value.Hash, StringComparer.OrdinalIgnoreCase),
        Objects.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<string>)pair.Value.References, StringComparer.OrdinalIgnoreCase));

    /// <summary>Reads the schema of the database at <paramref name="connection"/>, in one batch.</summary>
    public static async Task<SchemaSnapshot> ReadAsync(string connection)
    {
        await using var sql = new SqlConnection(connection);
        await sql.OpenAsync();
        await using var command = new SqlCommand(Query, sql);
        await using var reader = await command.ExecuteReaderAsync();

        var kinds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var lines = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var references = new Dictionary<string, SortedSet<string>>(StringComparer.OrdinalIgnoreCase);
        void Line(string name, string line) => (lines.TryGetValue(name, out var list) ? list : lines[name] = []).Add(line);

        // 1: objects and their kinds. 2: columns. 3: indexes. 4: foreign keys. 5: bodies. 6: references.
        while (await reader.ReadAsync())
        {
            kinds[reader.GetString(0)] = reader.GetString(1);
        }

        for (var set = 2; set <= 5 && await reader.NextResultAsync(); set++)
        {
            while (await reader.ReadAsync())
            {
                Line(reader.GetString(0), set == 5 ? "body " + string.Join(' ', reader.GetString(1).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)) : reader.GetString(1));
            }
        }

        if (await reader.NextResultAsync())
        {
            while (await reader.ReadAsync())
            {
                (references.TryGetValue(reader.GetString(0), out var set) ? set : references[reader.GetString(0)] = new(StringComparer.OrdinalIgnoreCase)).Add(reader.GetString(1));
            }
        }

        var snapshot = new SchemaSnapshot();
        foreach (var (name, kind) in kinds)
        {
            snapshot.Objects[name] = new SchemaObject
            {
                Kind = kind,
                Hash = Hash(lines.GetValueOrDefault(name) ?? []),
                References = [.. references.GetValueOrDefault(name) ?? []],
            };
        }

        return snapshot;
    }

    private static string Hash(IEnumerable<string> lines)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('\n', lines.Order(StringComparer.Ordinal)))))[..12];

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

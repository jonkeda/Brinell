using System.Data.Common;
using System.Globalization;
using Bruinet.Mocking.Data.Replay;

namespace Bruinet.Mocking.Data;

/// <summary>
/// The schema snapshot of a SQLite database, from <c>sqlite_master</c>: tables and views in <c>main</c>. A table's hash is
/// its <c>CREATE TABLE</c> text (SQLite rewrites it on <c>ALTER TABLE</c>, so it holds the columns, keys, defaults and
/// checks) with its indexes' and triggers' text. SQLite keeps no dependencies: a view's and a trigger's text is parsed for
/// the objects it names, and only names the snapshot holds are kept.
/// </summary>
internal static class SqliteSchema
{
    public const string Schema = "main";

    public static async Task<SchemaSnapshot> ReadAsync(DbConnection connection, SnapshotOptions options, CancellationToken cancellationToken)
    {
        var kinds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var lines = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var bodies = new List<(string Owner, string Sql)>();
        using (var command = connection.CreateCommand())
        {
            // Automatic indexes (a key's, a unique column's) have no sql: their table's text already says them.
            command.CommandText = "SELECT type, name, tbl_name, sql FROM sqlite_master WHERE sql IS NOT NULL AND name NOT LIKE 'sqlite~_%' ESCAPE '~'";
            using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var type = reader.GetString(0);
                var name = Qualified(reader.GetString(1));
                var owner = Qualified(reader.GetString(2));
                var sql = Statements.Normalize(reader.GetString(3));
                if (type is "table" or "view")
                {
                    kinds[name] = type;
                }

                if (!lines.TryGetValue(owner, out var list))
                {
                    lines[owner] = list = [];
                }

                list.Add(type + " " + sql);
                if (type is "view" or "trigger")
                {
                    bodies.Add((owner, sql));
                }
            }
        }

        // A view's references are its own; a trigger's are its table's.
        var references = new Dictionary<string, SortedSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (owner, sql) in bodies)
        {
            foreach (var (schema, name) in Statements.Names(sql))
            {
                var referenced = (schema ?? Schema) + "." + name;
                if (kinds.ContainsKey(referenced) && !string.Equals(referenced, owner, StringComparison.OrdinalIgnoreCase))
                {
                    if (!references.TryGetValue(owner, out var set))
                    {
                        references[owner] = set = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
                    }

                    set.Add(referenced.ToLowerInvariant());
                }
            }
        }

        foreach (var table in options.Data)
        {
            var name = table.Contains(".") ? table.ToLowerInvariant() : Schema + "." + table.ToLowerInvariant();
            if (!lines.TryGetValue(name, out var list))
            {
                lines[name] = list = [];
            }

            list.Add("data " + await DataHashAsync(connection, name, cancellationToken).ConfigureAwait(false));
        }

        var snapshot = new SchemaSnapshot { DefaultSchema = Schema };
        foreach (var pair in kinds)
        {
            snapshot.Objects[pair.Key] = new SchemaObject
            {
                Kind = pair.Value,
                Hash = SchemaSnapshot.Hash(lines.TryGetValue(pair.Key, out var list) ? list : []),
                References = references.TryGetValue(pair.Key, out var set) ? [.. set] : [],
            };
        }

        return snapshot;
    }

    private static string Qualified(string name) => Schema + "." + name.ToLowerInvariant();

    // SQLite has no checksum: the rows read whole, each as one canonical line, in any order the same hash.
    private static async Task<string> DataHashAsync(DbConnection connection, string table, CancellationToken cancellationToken)
    {
        var rows = new List<string>();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM " + string.Join(".", table.Split('.').Select(part => "\"" + part.Replace("\"", "\"\"") + "\""));
        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            rows.Add(string.Join("\u001f", Enumerable.Range(0, reader.FieldCount)
                .Select(i => reader.IsDBNull(i) ? "null" : Values.Canonical(Values.ToJson(reader.GetValue(i))))));
        }

        return rows.Count.ToString(CultureInfo.InvariantCulture) + " " + SchemaSnapshot.Hash(rows);
    }
}

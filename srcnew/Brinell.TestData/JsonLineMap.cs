using System.Text;
using System.Text.Json;

namespace Brinell.TestData;

internal sealed class JsonLineMap
{
    private readonly Dictionary<string, int> _lines;

    private JsonLineMap(Dictionary<string, int> lines) => _lines = lines;

    public static JsonLineMap Build(string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        var lineStarts = new List<long>();
        for (var i = 0; i < bytes.Length; i++)
        {
            if (bytes[i] == (byte)'\n')
            {
                lineStarts.Add(i);
            }
        }

        var lines = new Dictionary<string, int>(StringComparer.Ordinal);
        var reader = new Utf8JsonReader(bytes, new JsonReaderOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        });

        var frames = new Stack<Frame>();
        string? property = null;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                property = reader.GetString();
                continue;
            }

            if (reader.TokenType is JsonTokenType.EndObject or JsonTokenType.EndArray)
            {
                frames.Pop();
                continue;
            }

            var path = PathOf(frames, property);
            property = null;
            lines.TryAdd(path, LineOf(lineStarts, reader.TokenStartIndex));

            if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                frames.Push(new Frame(path, reader.TokenType == JsonTokenType.StartArray));
            }
        }

        return new JsonLineMap(lines);
    }

    public int? LineOf(string path)
    {
        var current = path;
        while (true)
        {
            if (_lines.TryGetValue(current, out var line))
            {
                return line;
            }

            var cut = Math.Max(current.LastIndexOf('.'), current.LastIndexOf('['));
            if (cut <= 0)
            {
                return null;
            }

            current = current[..cut];
        }
    }

    private static string PathOf(Stack<Frame> frames, string? property)
    {
        if (frames.Count == 0)
        {
            return string.Empty;
        }

        var parent = frames.Peek();
        if (parent.IsArray)
        {
            return $"{parent.Path}[{parent.NextIndex++}]";
        }

        return parent.Path.Length == 0 ? property ?? string.Empty : $"{parent.Path}.{property}";
    }

    private static int LineOf(List<long> newlineOffsets, long offset)
    {
        var index = newlineOffsets.BinarySearch(offset);
        return (index >= 0 ? index : ~index) + 1;
    }

    private sealed class Frame(string path, bool isArray)
    {
        public string Path { get; } = path;

        public bool IsArray { get; } = isArray;

        public int NextIndex { get; set; }
    }
}

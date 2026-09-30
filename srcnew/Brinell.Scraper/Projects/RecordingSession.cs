using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Brinell.Core.Scraping;
using Brinell.Scraper.Recordings;

namespace Brinell.Scraper.Projects;

/// <summary>
/// One recording session: a folder with <c>recording.json</c>, the ordered list of what happened, and a
/// snapshot file (and screenshot) per captured page. Today every event is a page capture; recorded
/// actions will be events in the same list.
/// </summary>
public sealed class RecordingSession
{
    /// <summary>The session's event list file.</summary>
    public const string FileName = "recording.json";

    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly List<RecordingEvent> _events;
    private readonly IReadOnlyList<TextMask> _masks;

    private RecordingSession(string folder, string name, List<RecordingEvent> events, IReadOnlyList<TextMask> masks)
    {
        Folder = folder;
        Name = name;
        _events = events;
        _masks = masks;
    }

    /// <summary>The session's folder.</summary>
    public string Folder { get; }

    /// <summary>The session's folder name, which is its id in a project.</summary>
    public string Id => Path.GetFileName(Folder);

    /// <summary>What the user called it.</summary>
    public string Name { get; }

    /// <summary>What happened, in order.</summary>
    public IReadOnlyList<RecordingEvent> Events => _events;

    internal static RecordingSession Create(string folder, string name, IReadOnlyList<TextMask> masks)
    {
        Directory.CreateDirectory(folder);
        var session = new RecordingSession(folder, name, [], masks);
        session.Save();
        return session;
    }

    /// <summary>Opens a session folder.</summary>
    /// <param name="folder">The session's folder.</param>
    /// <param name="masks">The project's masks, applied to pages added from now on.</param>
    /// <exception cref="FormatException">Its <c>recording.json</c> cannot be read.</exception>
    public static RecordingSession Open(string folder, IReadOnlyList<TextMask> masks)
    {
        var path = Path.Combine(folder, FileName);
        RecordingFile file;
        try
        {
            file = JsonSerializer.Deserialize<RecordingFile>(File.ReadAllText(path), Json) ?? new RecordingFile();
        }
        catch (JsonException e)
        {
            throw new FormatException($"{path}: {e.Message}", e);
        }
        return new RecordingSession(folder, file.Name ?? Path.GetFileName(folder), file.Events ?? [], masks);
    }

    /// <summary>
    /// Adds a captured page: masks the snapshot with the project's masks, writes it (and the screenshot),
    /// and appends the event.
    /// </summary>
    /// <param name="snapshot">The page as captured.</param>
    /// <param name="screenshot">A PNG of it, or null.</param>
    /// <param name="pageName">What the user called the page; its title when null.</param>
    public RecordingEvent AddPage(ScrapeSnapshot snapshot, byte[]? screenshot = null, string? pageName = null)
    {
        snapshot = Redactor.Apply(snapshot, _masks);
        var number = _events.Count + 1;
        var baseName = $"{number:000}-{Slug.Of(pageName ?? snapshot.Title, "page")}";
        var snapshotFile = baseName + SnapshotFile.Extension;
        SnapshotFile.Write(snapshot, Path.Combine(Folder, snapshotFile));
        string? screenshotFile = null;
        if (screenshot is not null)
        {
            screenshotFile = baseName + ".png";
            File.WriteAllBytes(Path.Combine(Folder, screenshotFile), screenshot);
        }
        var e = new RecordingEvent
        {
            Number = number,
            Kind = RecordingEvent.PageKind,
            At = snapshot.CapturedAt,
            Location = snapshot.Location,
            Page = PageKey.Of(snapshot.Location),
            Title = snapshot.Title,
            Name = pageName,
            Snapshot = snapshotFile,
            Screenshot = screenshotFile,
            Nodes = snapshot.Nodes.Count,
        };
        _events.Add(e);
        Save();
        return e;
    }

    /// <summary>The snapshot a page event wrote.</summary>
    /// <exception cref="InvalidOperationException">The event has no snapshot.</exception>
    public ScrapeSnapshot ReadSnapshot(RecordingEvent e) =>
        e.Snapshot is null
            ? throw new InvalidOperationException($"Event {e.Number} of {Id} has no snapshot.")
            : SnapshotFile.Read(Path.Combine(Folder, e.Snapshot));

    private void Save()
    {
        var file = new RecordingFile { Name = Name, Events = _events };
        File.WriteAllText(Path.Combine(Folder, FileName), JsonSerializer.Serialize(file, Json).Replace("\r\n", "\n", StringComparison.Ordinal) + "\n");
    }

    private sealed class RecordingFile
    {
        public string? Name { get; set; }

        public List<RecordingEvent>? Events { get; set; }
    }
}

/// <summary>One thing that happened in a recording session.</summary>
public sealed record RecordingEvent
{
    /// <summary>The kind of a page capture.</summary>
    public const string PageKind = "page";

    /// <summary>Its number in the session, from 1.</summary>
    public int Number { get; init; }

    /// <summary>What it is: <see cref="PageKind"/> today.</summary>
    public string Kind { get; init; } = PageKind;

    /// <summary>When it happened.</summary>
    public DateTimeOffset At { get; init; }

    /// <summary>Where the app was.</summary>
    public string Location { get; init; } = "";

    /// <summary>The page's key (<see cref="PageKey"/>).</summary>
    public string Page { get; init; } = "";

    /// <summary>The page's title.</summary>
    public string Title { get; init; } = "";

    /// <summary>What the user called the page, if anything.</summary>
    public string? Name { get; init; }

    /// <summary>The snapshot file, in the session folder.</summary>
    public string? Snapshot { get; init; }

    /// <summary>The screenshot file, in the session folder.</summary>
    public string? Screenshot { get; init; }

    /// <summary>How many nodes the snapshot has.</summary>
    public int Nodes { get; init; }

    /// <summary>The name to show: the user's, else the title, else the page key.</summary>
    public string DisplayName => Name ?? (Title.Length > 0 ? Title : Page);

    /// <inheritdoc/>
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Number} {Kind} {Page}");
}

using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Brinell.Scraper.Projects;

/// <summary>
/// A UI test project that is scraped: the folder with <c>scraper.config.md</c> and the project file. Its
/// recordings are in <c>Recordings/</c>, one folder per session.
/// </summary>
public sealed class ScrapeProject
{
    /// <summary>The folder recordings are kept in, in the project.</summary>
    public const string RecordingsFolderName = "Recordings";

    private ScrapeProject(string folder, string name, ScraperConfig config)
    {
        Folder = folder;
        Name = name;
        Config = config;
    }

    /// <summary>The project's folder.</summary>
    public string Folder { get; }

    /// <summary>The project's name: its project file's name, or the folder's when it has none.</summary>
    public string Name { get; }

    /// <summary>Its settings.</summary>
    public ScraperConfig Config { get; }

    /// <summary>The namespace generated code goes in.</summary>
    public string Namespace => Config.Namespace.Length > 0 ? Config.Namespace : Name;

    /// <summary>Where its recordings are.</summary>
    public string RecordingsFolder => Path.Combine(Folder, RecordingsFolderName);

    /// <summary>Opens the project in <paramref name="folder"/>.</summary>
    /// <exception cref="FileNotFoundException">The folder has no <c>scraper.config.md</c>.</exception>
    public static ScrapeProject Open(string folder)
    {
        folder = Path.GetFullPath(folder);
        var configPath = Path.Combine(folder, ScraperConfig.FileName);
        if (!File.Exists(configPath))
            throw new FileNotFoundException($"{folder} is not a scraped project: it has no {ScraperConfig.FileName}.", configPath);
        var projectFile = Directory.EnumerateFiles(folder, "*.csproj").OrderBy(f => f, StringComparer.Ordinal).FirstOrDefault();
        var name = projectFile is null ? Path.GetFileName(folder) : Path.GetFileNameWithoutExtension(projectFile);
        return new ScrapeProject(folder, name, ScraperConfig.Read(configPath));
    }

    /// <summary>The project that holds <paramref name="path"/> (a file or folder in it), or null.</summary>
    public static ScrapeProject? Find(string path)
    {
        var folder = Directory.Exists(path) ? Path.GetFullPath(path) : Path.GetDirectoryName(Path.GetFullPath(path));
        for (; folder is not null; folder = Path.GetDirectoryName(folder))
            if (File.Exists(Path.Combine(folder, ScraperConfig.FileName)))
                return Open(folder);
        return null;
    }

    /// <summary>The scraped projects under <paramref name="root"/> (a solution's folder), skipping build output.</summary>
    public static IReadOnlyList<ScrapeProject> FindAll(string root)
    {
        var found = new List<ScrapeProject>();
        var pending = new Stack<string>([Path.GetFullPath(root)]);
        while (pending.Count > 0)
        {
            var folder = pending.Pop();
            if (File.Exists(Path.Combine(folder, ScraperConfig.FileName)))
                found.Add(Open(folder));
            foreach (var child in Directory.EnumerateDirectories(folder))
            {
                var name = Path.GetFileName(child);
                if (name is "bin" or "obj" or "node_modules" or RecordingsFolderName || name.StartsWith('.')) continue;
                pending.Push(child);
            }
        }
        return found.OrderBy(p => p.Folder, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    /// <summary>Whether a location is one of the app's own pages: on the start URL's host or an alias's.</summary>
    public bool IsOwnPage(string location)
    {
        if (!Uri.TryCreate(location, UriKind.Absolute, out var uri)) return false;
        return new[] { Config.StartUrl }.Concat(Config.Aliases)
            .Select(u => Uri.TryCreate(u, UriKind.Absolute, out var a) ? a : null)
            .Any(a => a is not null && string.Equals(a.Authority, uri.Authority, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>The recording sessions, oldest first.</summary>
    public IReadOnlyList<RecordingSession> Sessions() =>
        Directory.Exists(RecordingsFolder)
            ? Directory.EnumerateDirectories(RecordingsFolder)
                .Where(d => File.Exists(Path.Combine(d, RecordingSession.FileName)))
                .OrderBy(d => d, StringComparer.Ordinal)
                .Select(d => RecordingSession.Open(d, Config.Masks))
                .ToArray()
            : [];

    /// <summary>The session with this id (its folder name), or null.</summary>
    public RecordingSession? Session(string id)
    {
        var folder = Path.Combine(RecordingsFolder, id);
        return id.Length > 0 && Path.GetFileName(id) == id && File.Exists(Path.Combine(folder, RecordingSession.FileName))
            ? RecordingSession.Open(folder, Config.Masks)
            : null;
    }

    /// <summary>Starts a session: a new folder named after the day and <paramref name="name"/>.</summary>
    public RecordingSession StartSession(string name, DateTimeOffset now)
    {
        var baseName = now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "-" + Slug.Of(name, "recording");
        var folder = Path.Combine(RecordingsFolder, baseName);
        for (var i = 2; Directory.Exists(folder); i++)
            folder = Path.Combine(RecordingsFolder, $"{baseName}-{i}");
        return RecordingSession.Create(folder, name, Config.Masks);
    }

    /// <summary>What is recorded, by page, across all sessions.</summary>
    public RecordingIndex Index() => RecordingIndex.Build(Sessions());

    /// <summary>
    /// Where the recording browser keeps its login and cookies for this project: under the user's local
    /// application data, never in the repository.
    /// </summary>
    public string ProfileFolder
    {
        get
        {
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Folder.ToLowerInvariant())))[..12];
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Brinell", "Scraper", "profiles", $"{Name}-{hash}");
        }
    }
}

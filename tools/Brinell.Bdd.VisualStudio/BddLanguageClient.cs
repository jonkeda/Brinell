using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using Task = System.Threading.Tasks.Task;

namespace Brinell.Bdd.VisualStudio;

/// <summary>
/// The content type of Brinell's Markdown files: feature and step files and the suite configs. It
/// derives from the Markdown editor's own type (<c>vs-markdown</c>), so its colours and outlining
/// stay (its preview is turned off for them: <see cref="BddTextViewListener"/>), and from the
/// remote-content type an LSP client needs (S2).
/// </summary>
internal static class BddContentType
{
    public const string Name = "brinell-bdd";

    [Export]
    [Name(Name)]
    [BaseDefinition("vs-markdown")]
    [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteContentTypeName)]
    internal static ContentTypeDefinition? Definition { get; set; }

    /// <summary>Whether a path is a feature, step, video or config file.</summary>
    public static bool Holds(string path)
        => PreviewController.IsPreviewable(path) ||
           path.EndsWith("uat.config.md", StringComparison.OrdinalIgnoreCase) ||
           path.EndsWith("bdd.config.md", StringComparison.OrdinalIgnoreCase);
}

// Every *.md goes through here: only Brinell's files get the content type; the rest stay Markdown.
[Export(typeof(IFilePathToContentTypeProvider))]
[Name("Brinell BDD file path")]
[FileExtension(".md")]
internal sealed class BddContentTypeProvider : IFilePathToContentTypeProvider
{
    [Import]
    internal IContentTypeRegistryService ContentTypes { get; set; } = null!;

    public bool TryGetContentTypeForFilePath(string filePath, out IContentType? contentType)
    {
        contentType = BddContentType.Holds(filePath) ? ContentTypes.GetContentType(BddContentType.Name) : null;
        return contentType is not null;
    }

    public bool TryGetContentTypeForFileExtension(string fileExtension, out IContentType? contentType)
    {
        contentType = null;
        return false;
    }
}

/// <summary>
/// The editor's LSP client (phase 3): diagnostics into the Error List, and the language features,
/// from <c>Brinell.Bdd.LanguageServer</c>. It starts its own server process; the preview keeps its
/// own (<see cref="BddServerClient"/>), so a failing editor client never takes the preview down.
/// </summary>
[Export(typeof(ILanguageClient))]
[ContentType(BddContentType.Name)]
internal sealed class BddLanguageClient : ILanguageClient
{
    private Process? _process;

    public event AsyncEventHandler<EventArgs>? StartAsync;

    // Visual Studio stops the server itself when the solution closes; the client never asks it to.
#pragma warning disable CS0067
    public event AsyncEventHandler<EventArgs>? StopAsync;
#pragma warning restore CS0067

    public string Name => "Brinell BDD";

    public IEnumerable<string>? ConfigurationSections => null;

    public object? InitializationOptions { get; private set; }

    public IEnumerable<string>? FilesToWatch => null;

    public bool ShowNotificationOnInitializeFailed => true;

    public async Task<Connection?> ActivateAsync(CancellationToken token)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(token);
        InitializationOptions = new { solutionDir = await SolutionDirectoryAsync() };
        await TaskScheduler.Default;

        var server = BddServerClient.DefaultServerPath;
        if (!File.Exists(server))
        {
            return null;
        }

        _process = Process.Start(new ProcessStartInfo("dotnet", $"\"{server}\"")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        });
        if (_process is null)
        {
            return null;
        }

        // The server logs to stderr; left unread, a full pipe would stop it.
        _process.ErrorDataReceived += (_, _) => { };
        _process.BeginErrorReadLine();
        return new Connection(_process.StandardOutput.BaseStream, _process.StandardInput.BaseStream);
    }

    public async Task OnLoadedAsync()
    {
        if (StartAsync is { } start)
        {
            await start.InvokeAsync(this, EventArgs.Empty);
        }
    }

    public Task OnServerInitializedAsync() => Task.CompletedTask;

    public Task<InitializationFailureContext?> OnServerInitializeFailedAsync(ILanguageClientInitializationInfo initializationState)
        => Task.FromResult<InitializationFailureContext?>(new InitializationFailureContext
        {
            FailureMessage = $"The Brinell BDD language server did not start: {initializationState.InitializationException?.Message ?? "no reason given"}. The Scenario Preview still works.",
        });

    private static async Task<string?> SolutionDirectoryAsync()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        if (await AsyncServiceProvider.GlobalProvider.GetServiceAsync(typeof(SVsSolution)) is IVsSolution solution &&
            solution.GetSolutionInfo(out var directory, out _, out _) == Microsoft.VisualStudio.VSConstants.S_OK &&
            !string.IsNullOrEmpty(directory))
        {
            return directory.TrimEnd('\\');
        }

        return null;
    }
}

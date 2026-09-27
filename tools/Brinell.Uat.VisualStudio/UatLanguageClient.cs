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

namespace Brinell.Uat.VisualStudio;

/// <summary>
/// The content type of Brinell's Markdown files: feature and step files and the suite configs. It
/// derives from Markdown, so the Markdown editor keeps its colours and preview, and from the
/// remote-content type an LSP client needs (S2).
/// </summary>
internal static class UatContentType
{
    public const string Name = "brinell-uat";

    [Export]
    [Name(Name)]
    [BaseDefinition("markdown")]
    [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteContentTypeName)]
    internal static ContentTypeDefinition? Definition { get; set; }

    /// <summary>Whether a path is a feature, step or config file.</summary>
    public static bool Holds(string path)
        => PreviewController.IsFeatureOrSteps(path) ||
           path.EndsWith("uat.config.md", StringComparison.OrdinalIgnoreCase) ||
           path.EndsWith("bdd.config.md", StringComparison.OrdinalIgnoreCase);
}

// Every *.md goes through here: only Brinell's files get the content type; the rest stay Markdown.
[Export(typeof(IFilePathToContentTypeProvider))]
[Name("Brinell UAT file path")]
[FileExtension(".md")]
internal sealed class UatContentTypeProvider : IFilePathToContentTypeProvider
{
    [Import]
    internal IContentTypeRegistryService ContentTypes { get; set; } = null!;

    public bool TryGetContentTypeForFilePath(string filePath, out IContentType? contentType)
    {
        contentType = UatContentType.Holds(filePath) ? ContentTypes.GetContentType(UatContentType.Name) : null;
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
/// from <c>Brinell.Uat.LanguageServer</c>. It starts its own server process; the preview keeps its
/// own (<see cref="UatServerClient"/>), so a failing editor client never takes the preview down.
/// </summary>
[Export(typeof(ILanguageClient))]
[ContentType(UatContentType.Name)]
internal sealed class UatLanguageClient : ILanguageClient
{
    private Process? _process;

    public event AsyncEventHandler<EventArgs>? StartAsync;

    // Visual Studio stops the server itself when the solution closes; the client never asks it to.
#pragma warning disable CS0067
    public event AsyncEventHandler<EventArgs>? StopAsync;
#pragma warning restore CS0067

    public string Name => "Brinell UAT";

    public IEnumerable<string>? ConfigurationSections => null;

    public object? InitializationOptions { get; private set; }

    public IEnumerable<string>? FilesToWatch => null;

    public bool ShowNotificationOnInitializeFailed => true;

    public async Task<Connection?> ActivateAsync(CancellationToken token)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(token);
        InitializationOptions = new { solutionDir = await SolutionDirectoryAsync() };
        await TaskScheduler.Default;

        var server = UatServerClient.DefaultServerPath;
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
            FailureMessage = $"The Brinell UAT language server did not start: {initializationState.InitializationException?.Message ?? "no reason given"}. The Scenario Preview still works.",
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

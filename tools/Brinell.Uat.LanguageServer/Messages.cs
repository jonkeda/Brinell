using System.Text.Json;
using System.Text.Json.Serialization;

namespace Brinell.Uat.LanguageServer;

// The messages, as they go over the wire: LSP's shapes where LSP has one, camelCase JSON.

/// <summary>LSP <c>initialize</c>: the solution folder, from <c>initializationOptions.solutionDir</c> or the root URI.</summary>
public sealed record InitializeParams(
    int? ProcessId,
    string? RootUri,
    InitializationOptions? InitializationOptions);

/// <summary>What the client tells the server beyond LSP's own fields.</summary>
/// <param name="SolutionDir">The folder whose suites the server serves.</param>
public sealed record InitializationOptions(string? SolutionDir);

/// <summary>The server's answer to <c>initialize</c>: LSP capabilities, and the suites it found.</summary>
public sealed record InitializeResult(ServerCapabilities Capabilities, ServerInfo ServerInfo, IReadOnlyList<SuiteInfo> Suites);

/// <summary>LSP capabilities: full-text document sync for now.</summary>
/// <param name="TextDocumentSync">1: the client sends the whole text on each change.</param>
public sealed record ServerCapabilities(int TextDocumentSync);

/// <summary>LSP server info.</summary>
public sealed record ServerInfo(string Name, string Version);

/// <summary>A suite, for the client's picker.</summary>
/// <param name="Name">The project folder's name.</param>
/// <param name="Config">The config's full path.</param>
/// <param name="Head">The config's <c>Head</c>, or null.</param>
public sealed record SuiteInfo(string Name, string Config, string? Head);

/// <summary>LSP <c>textDocument/didOpen</c>.</summary>
public sealed record DidOpenTextDocumentParams(TextDocumentItem TextDocument);

/// <summary>LSP text document item.</summary>
public sealed record TextDocumentItem(string Uri, string? LanguageId, int Version, string Text);

/// <summary>LSP <c>textDocument/didChange</c>, full sync: the last change holds the whole text.</summary>
public sealed record DidChangeTextDocumentParams(VersionedTextDocumentIdentifier TextDocument, IReadOnlyList<TextDocumentContentChangeEvent> ContentChanges);

/// <summary>LSP versioned document id.</summary>
public sealed record VersionedTextDocumentIdentifier(string Uri, int Version);

/// <summary>LSP content change; with full sync, the whole text.</summary>
public sealed record TextDocumentContentChangeEvent(string Text);

/// <summary>LSP <c>textDocument/didClose</c>.</summary>
public sealed record DidCloseTextDocumentParams(TextDocumentIdentifier TextDocument);

/// <summary>LSP document id.</summary>
public sealed record TextDocumentIdentifier(string Uri);

/// <summary><c>brinell/preview</c>: a file, and the suite to bind it in when it is linked into several.</summary>
public sealed record PreviewParams(string Uri, string? Suite);

/// <summary>The rendered preview.</summary>
/// <param name="Html">The file, rendered (<c>Brinell.Uat.Preview</c>).</param>
/// <param name="Version">The document version rendered; 0 for a file read from disk.</param>
/// <param name="Suites">The suites the file belongs to, for the picker.</param>
/// <param name="Suite">The suite it was bound in; null when none.</param>
/// <param name="BindState"><c>bound</c> or <c>parse-only</c>.</param>
/// <param name="Reason">Why it is only parsed; null when bound.</param>
public sealed record PreviewResult(string Html, int Version, IReadOnlyList<string> Suites, string? Suite, string BindState, string? Reason)
{
    /// <summary>The folder of the runs, which the client maps to <see cref="ResultsHost"/> to show screenshots.</summary>
    public string? ResultsRoot { get; init; }

    /// <summary>The virtual host the preview's screenshot URLs use.</summary>
    public string? ResultsHost { get; init; }
}

/// <summary>The preview's host page.</summary>
/// <param name="Html">A whole page with the stylesheet and the page script (<c>UatPreview.HostPage</c>).</param>
public sealed record PreviewHostResult(string Html);

internal static class Json
{
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Brinell.Uat.LanguageServer;

// The messages, as they go over the wire: LSP's shapes where LSP has one, camelCase JSON.

/// <summary>LSP <c>initialize</c>: the solution folder, from <c>initializationOptions.solutionDir</c> or the root URI.</summary>
public sealed record InitializeParams(
    int? ProcessId,
    string? RootUri,
    InitializationOptions? InitializationOptions)
{
    /// <summary>What the client can do; the editor's LSP client says it takes diagnostics, the preview's does not.</summary>
    public JsonElement? Capabilities { get; init; }
}

/// <summary>What the client tells the server beyond LSP's own fields.</summary>
/// <param name="SolutionDir">The folder whose suites the server serves.</param>
public sealed record InitializationOptions(string? SolutionDir);

/// <summary>The server's answer to <c>initialize</c>: LSP capabilities, and the suites it found.</summary>
public sealed record InitializeResult(ServerCapabilities Capabilities, ServerInfo ServerInfo, IReadOnlyList<SuiteInfo> Suites);

/// <summary>LSP capabilities.</summary>
/// <param name="TextDocumentSync">Open and close, the whole text on each change, and save.</param>
public sealed record ServerCapabilities(TextDocumentSyncOptions TextDocumentSync)
{
    /// <summary>The outline.</summary>
    public bool DocumentSymbolProvider { get; init; }

    /// <summary>Folding of sections, fences and tables.</summary>
    public bool FoldingRangeProvider { get; init; }

    /// <summary>The colours of keywords, values, placeholders and tags.</summary>
    public SemanticTokensOptions? SemanticTokensProvider { get; init; }

    /// <summary>Completion of keywords, sections, steps and values.</summary>
    public CompletionOptions? CompletionProvider { get; init; }

    /// <summary>What a step binds to, its summary and its arguments as read.</summary>
    public bool HoverProvider { get; init; }

    /// <summary>A step to its step definition, page, control or callable.</summary>
    public bool DefinitionProvider { get; init; }

    /// <summary>Every step across the suite that binds to the same thing.</summary>
    public bool ReferencesProvider { get; init; }

    /// <summary>Quick fixes, create step definition, extract and inline a callable step.</summary>
    public CodeActionOptions? CodeActionProvider { get; init; }

    /// <summary>Renaming a callable step and every call of it.</summary>
    public RenameOptions? RenameProvider { get; init; }
}

/// <summary>LSP code action options: the kinds offered.</summary>
public sealed record CodeActionOptions(IReadOnlyList<string> CodeActionKinds);

/// <summary>LSP rename options: the client asks first what can be renamed.</summary>
public sealed record RenameOptions(bool PrepareProvider);

/// <summary>LSP completion options.</summary>
/// <param name="TriggerCharacters">Typing one of these asks for completion.</param>
public sealed record CompletionOptions(IReadOnlyList<string> TriggerCharacters);

/// <summary>LSP semantic tokens options: the whole file at once.</summary>
public sealed record SemanticTokensOptions(SemanticTokensLegend Legend, bool Full);

/// <summary>LSP semantic tokens legend.</summary>
public sealed record SemanticTokensLegend(IReadOnlyList<string> TokenTypes, IReadOnlyList<string> TokenModifiers);

/// <summary>LSP text document sync options.</summary>
/// <param name="OpenClose">Whether the client sends open and close.</param>
/// <param name="Change">1: the whole text on each change.</param>
/// <param name="Save">Whether the client sends save.</param>
public sealed record TextDocumentSyncOptions(bool OpenClose, int Change, SaveOptions Save);

/// <summary>LSP save options: the server reads a saved file itself.</summary>
public sealed record SaveOptions(bool IncludeText);

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

/// <summary>LSP <c>textDocument/didSave</c>.</summary>
public sealed record DidSaveTextDocumentParams(TextDocumentIdentifier TextDocument);

/// <summary>LSP position: 0-based line and character.</summary>
public sealed record Position(int Line, int Character);

/// <summary>LSP range, end exclusive.</summary>
public sealed record Range(Position Start, Position End);

/// <summary>LSP location.</summary>
public sealed record Location(string Uri, Range Range);

/// <summary>LSP diagnostic.</summary>
/// <param name="Range">Where: the line's text.</param>
/// <param name="Severity">1 error, 2 warning, 3 information.</param>
/// <param name="Code">The gate's code (<c>UATB010</c>), or <c>UATRUN</c> for a step that failed in the last run.</param>
/// <param name="Source">Always <c>Brinell UAT</c>.</param>
/// <param name="Message">The gate's text, word for word.</param>
public sealed record Diagnostic(Range Range, int Severity, string Code, string Source, string Message);

/// <summary>A request about a position: <c>completion</c>, <c>hover</c>, <c>definition</c>, <c>references</c>.</summary>
public sealed record PositionParams(TextDocumentIdentifier TextDocument, Position Position);

/// <summary>LSP completion list; incomplete, so the client asks again as the words change.</summary>
public sealed record CompletionList(bool IsIncomplete, IReadOnlyList<CompletionItem> Items);

/// <summary>LSP completion item.</summary>
/// <param name="Label">What the list shows.</param>
/// <param name="Kind">LSP completion item kind.</param>
public sealed record CompletionItem(string Label, int Kind)
{
    /// <summary>What it binds to, or where it comes from.</summary>
    public string? Detail { get; init; }

    /// <summary>The text put in, replacing <see cref="TextEdit"/>'s range.</summary>
    public TextEdit? TextEdit { get; init; }

    /// <summary>1 plain text, 2 a snippet with <c>${1:x}</c> placeholders.</summary>
    public int? InsertTextFormat { get; init; }

    /// <summary>What the client filters on: the typed words, so a match by words is not dropped for not being a prefix.</summary>
    public string? FilterText { get; init; }

    /// <summary>The order in the list.</summary>
    public string? SortText { get; init; }
}

/// <summary>LSP hover.</summary>
public sealed record Hover(MarkupContent Contents, Range? Range);

/// <summary>LSP markup: <c>markdown</c>.</summary>
public sealed record MarkupContent(string Kind, string Value);

/// <summary>LSP <c>textDocument/references</c>.</summary>
public sealed record ReferenceParams(TextDocumentIdentifier TextDocument, Position Position, ReferenceContext? Context);

/// <summary>Whether the definition is one of the references.</summary>
public sealed record ReferenceContext(bool IncludeDeclaration);

/// <summary>LSP <c>textDocument/codeAction</c>.</summary>
public sealed record CodeActionParams(TextDocumentIdentifier TextDocument, Range Range, CodeActionContext Context);

/// <summary>The diagnostics at the range.</summary>
public sealed record CodeActionContext(IReadOnlyList<Diagnostic> Diagnostics);

/// <summary>LSP code action.</summary>
/// <param name="Title">What the light bulb shows.</param>
/// <param name="Kind"><c>quickfix</c>, <c>refactor.extract</c>, <c>refactor.inline</c>.</param>
/// <param name="Edit">The change.</param>
public sealed record CodeAction(string Title, string Kind, WorkspaceEdit Edit)
{
    /// <summary>The diagnostics it fixes.</summary>
    public IReadOnlyList<Diagnostic>? Diagnostics { get; init; }

    /// <summary>Whether it is the one right answer.</summary>
    public bool? IsPreferred { get; init; }
}

/// <summary>LSP workspace edit: text edits by file, or document changes with a file rename.</summary>
public sealed record WorkspaceEdit
{
    /// <summary>Text edits, by file URI.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<TextEdit>>? Changes { get; init; }

    /// <summary>Renames and text edits, in order.</summary>
    public IReadOnlyList<object>? DocumentChanges { get; init; }
}

/// <summary>LSP rename-file operation.</summary>
public sealed record RenameFile(string OldUri, string NewUri)
{
    /// <summary>Always <c>rename</c>.</summary>
    public string Kind => "rename";
}

/// <summary>LSP text document edit.</summary>
public sealed record TextDocumentEdit(VersionedTextDocumentIdentifierOrNull TextDocument, IReadOnlyList<TextEdit> Edits);

/// <summary>LSP optional versioned document id: a null version for a file edited as it is on disk.</summary>
public sealed record VersionedTextDocumentIdentifierOrNull(string Uri, int? Version);

/// <summary>LSP <c>textDocument/rename</c>.</summary>
public sealed record RenameParams(TextDocumentIdentifier TextDocument, Position Position, string NewName);

/// <summary>LSP prepare-rename answer: the phrase's range and its text.</summary>
public sealed record PrepareRenameResult(Range Range, string Placeholder);

/// <summary>LSP text edit.</summary>
public sealed record TextEdit(Range Range, string NewText);

/// <summary>A request about a whole file: <c>documentSymbol</c>, <c>foldingRange</c>, <c>semanticTokens/full</c>.</summary>
public sealed record DocumentParams(TextDocumentIdentifier TextDocument);

/// <summary>LSP document symbol: a heading, with the headings under it.</summary>
/// <param name="Name">The heading's name.</param>
/// <param name="Detail">Its kind: <c>Scenario</c>, <c>Step</c>.</param>
/// <param name="Kind">LSP symbol kind.</param>
/// <param name="Range">The whole section.</param>
/// <param name="SelectionRange">The heading line.</param>
/// <param name="Children">The headings under it.</param>
public sealed record DocumentSymbol(string Name, string? Detail, int Kind, Range Range, Range SelectionRange, IReadOnlyList<DocumentSymbol> Children);

/// <summary>LSP folding range, 0-based lines.</summary>
public sealed record FoldingRange(int StartLine, int EndLine, string? Kind = null);

/// <summary>LSP semantic tokens: five numbers per token, each relative to the one before.</summary>
public sealed record SemanticTokens(IReadOnlyList<int> Data);

/// <summary>LSP <c>textDocument/publishDiagnostics</c>: every diagnostic of a file, replacing the last.</summary>
public sealed record PublishDiagnosticsParams(string Uri, int? Version, IReadOnlyList<Diagnostic> Diagnostics);

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

/// <summary><c>brinell/runTarget</c>: what to run for a scenario heading's line, or the whole file when there is none.</summary>
public sealed record RunTargetParams(string Uri, int? Line, string? Suite);

/// <summary>How to run it: <c>dotnet test &lt;Project&gt; --no-build --filter &lt;Filter&gt;</c>.</summary>
/// <param name="CanRun">Whether it can be run by address.</param>
/// <param name="Reason">Why not; null when it can.</param>
/// <param name="Project">The suite's project file.</param>
/// <param name="Filter">The <c>dotnet test</c> filter on the <c>UatScenario</c> trait.</param>
/// <param name="Scenarios">The scenarios it runs; empty for the whole file.</param>
/// <param name="Suite">The suite it runs in.</param>
public sealed record RunTargetResult(bool CanRun, string? Reason, string? Project, string? Filter, IReadOnlyList<string> Scenarios, string? Suite);

/// <summary><c>brinell/usages</c>: a C# member, as CodeLens names it (<c>Ns.Type.Method(int)</c>).</summary>
public sealed record UsagesParams(string Symbol);

/// <summary>The steps across every suite that use a C# member.</summary>
/// <param name="Steps">How many steps.</param>
/// <param name="Scenarios">How many scenarios they are in.</param>
/// <param name="Usages">Each step.</param>
public sealed record UsagesResult(int Steps, int Scenarios, IReadOnlyList<Usage> Usages);

/// <summary>A step that uses a C# member.</summary>
/// <param name="Uri">Its file.</param>
/// <param name="Line">Its 0-based line.</param>
/// <param name="Text">The line's text.</param>
/// <param name="Scenario">The scenario it is in.</param>
public sealed record Usage(string Uri, int Line, string Text, string Scenario);

/// <summary><c>brinell/explorer</c>: a view (<c>steps</c>, <c>pages</c>, <c>results</c>) of a suite.</summary>
public sealed record ExplorerParams(string? View, string? Suite);

/// <summary>A view of a suite, as HTML for the explorer's page.</summary>
/// <param name="View">The view shown.</param>
/// <param name="Html">The view.</param>
/// <param name="Suites">Every suite, for the picker.</param>
/// <param name="Suite">The suite shown; null when there is none.</param>
public sealed record ExplorerResult(string View, string Html, IReadOnlyList<string> Suites, string? Suite);

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

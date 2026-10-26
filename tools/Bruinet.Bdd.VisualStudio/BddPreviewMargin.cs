using System.ComponentModel.Composition;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Bruinet.Bdd.VisualStudio;

/// <summary>Whether a feature or step file's editor shows the BDD preview beside the text.</summary>
internal static class BddPreviewOptions
{
    public const string ShowPreviewName = "BruinetBdd/ShowPreview";

    public static readonly EditorOptionKey<bool> ShowPreview = new(ShowPreviewName);

    // Visual Studio's own Markdown preview margin is created only while this view option is true
    // (DefaultMarkdownOptions.ShowMarkdownPreviewOptionName).
    public const string ShowMarkdownPreviewName = "ShowMarkdownPreview";
}

[Export(typeof(EditorOptionDefinition))]
[Name(BddPreviewOptions.ShowPreviewName)]
internal sealed class BddShowPreviewOption : EditorOptionDefinition<bool>
{
    public override bool Default => true;

    public override EditorOptionKey<bool> Key => BddPreviewOptions.ShowPreview;
}

// A feature or step file's editor: the Bruinet content type (the Markdown editor may have given the
// buffer its own), and Visual Studio's Markdown preview off, since the BDD preview takes its place.
[Export(typeof(IWpfTextViewCreationListener))]
[ContentType("text")]
[TextViewRole(PredefinedTextViewRoles.PrimaryDocument)]
internal sealed class BddTextViewListener : IWpfTextViewCreationListener
{
    [Import]
    internal ITextDocumentFactoryService Documents { get; set; } = null!;

    [Import]
    internal IContentTypeRegistryService ContentTypes { get; set; } = null!;

    public void TextViewCreated(IWpfTextView textView)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        var buffer = textView.TextDataModel.DocumentBuffer;
        if (!Documents.TryGetTextDocument(buffer, out var document) || !BddContentType.Holds(document.FilePath))
        {
            return;
        }

        var before = buffer.ContentType.TypeName;
        if (!buffer.ContentType.IsOfType(BddContentType.Name) && ContentTypes.GetContentType(BddContentType.Name) is { } bdd)
        {
            buffer.ChangeContentType(bdd, null);
        }

        var hidden = PreviewController.IsPreviewable(document.FilePath) && TrySet(textView.Options, BddPreviewOptions.ShowMarkdownPreviewName, false);
        ActivityLog.LogInformation("Bruinet BDD", $"{Path.GetFileName(document.FilePath)}: content type {before} -> {buffer.ContentType.TypeName}; Visual Studio's Markdown preview hidden: {hidden}");
    }

    // An option another extension defines: absent in a Visual Studio without the Markdown editor.
    private static bool TrySet(IEditorOptions options, string name, object value)
    {
        if (!options.IsOptionDefined(name, localScopeOnly: false))
        {
            return false;
        }

        options.SetOptionValue(name, value);
        return true;
    }
}

[Export(typeof(IWpfTextViewMarginProvider))]
[Name(BddPreviewMargin.MarginName)]
[Order(After = PredefinedMarginNames.RightControl)]
[MarginContainer(PredefinedMarginNames.Right)]
[ContentType("text")]
[TextViewRole(PredefinedTextViewRoles.PrimaryDocument)]
[DeferCreation(OptionName = BddPreviewOptions.ShowPreviewName)]
internal sealed class BddPreviewMarginProvider : IWpfTextViewMarginProvider
{
    [Import]
    internal ITextDocumentFactoryService Documents { get; set; } = null!;

    public IWpfTextViewMargin? CreateMargin(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer)
        => Documents.TryGetTextDocument(wpfTextViewHost.TextView.TextDataModel.DocumentBuffer, out var document) && PreviewController.IsPreviewable(document.FilePath)
            ? new BddPreviewMargin(document.FilePath)
            : null;
}

/// <summary>
/// The BDD preview beside a feature or step file's text, where Visual Studio shows its Markdown
/// preview: a grip to resize it, and the same page the Scenario Preview window shows.
/// </summary>
internal sealed class BddPreviewMargin : DockPanel, IWpfTextViewMargin
{
    public const string MarginName = "BruinetBddPreviewMargin";

    // The width the last preview was dragged to, for the next one.
    private static double _width = 520;

    private readonly ScenarioPreviewControl _preview = new("Scenario Preview (margin)");
    private bool _disposed;

    public BddPreviewMargin(string path)
    {
        var grip = new Thumb { Width = 5, Cursor = Cursors.SizeWE, Background = SystemColors.ControlBrush };
        grip.DragDelta += (_, e) => Width = _width = Math.Max(200, Math.Min(2000, Width - e.HorizontalChange));
        SetDock(grip, Dock.Left);
        Children.Add(grip);
        Children.Add(_preview);
        Width = _width;

        Attaching.RunAsync(() => AttachAsync(path)).FileAndForget("Bruinet/PreviewMargin");
    }

    // The margins' own factory: the package's may not exist yet when an editor opens.
    private static readonly Microsoft.VisualStudio.Threading.JoinableTaskFactory Attaching = new(ThreadHelper.JoinableTaskContext);

    private async System.Threading.Tasks.Task AttachAsync(string path)
    {
        try
        {
            if (await BruinetBddPackage.LoadAsync() is { Controller: { } controller } && !_disposed)
            {
                await controller.AttachAsync(_preview, path);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            ActivityLog.LogError("Bruinet BDD", $"The preview beside {Path.GetFileName(path)} failed to start: {exception}");
        }
    }

    public FrameworkElement VisualElement => this;

    public double MarginSize => ActualWidth;

    public bool Enabled => true;

    public ITextViewMargin? GetTextViewMargin(string marginName)
        => string.Equals(marginName, MarginName, StringComparison.OrdinalIgnoreCase) ? this : null;

    public void Dispose()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        BruinetBddPackage.Instance?.Controller?.Release(_preview);
        _preview.Close();
    }
}

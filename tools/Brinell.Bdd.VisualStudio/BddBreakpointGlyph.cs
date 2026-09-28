using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Brinell.Bdd.VisualStudio;

/// <summary>A step breakpoint in a feature or step file, drawn in the glyph margin.</summary>
internal sealed class BddBreakpointTag : IGlyphTag
{
    public static readonly BddBreakpointTag Instance = new();
}

// Tags the lines BddBreakpointStore lists for the buffer's file; only feature and step files get a tagger.
[Export(typeof(ITaggerProvider))]
[ContentType("text")]
[TagType(typeof(BddBreakpointTag))]
internal sealed class BddBreakpointTaggerProvider : ITaggerProvider
{
    [Import]
    internal ITextDocumentFactoryService Documents { get; set; } = null!;

    public ITagger<T>? CreateTagger<T>(ITextBuffer buffer)
        where T : ITag
        => Documents.TryGetTextDocument(buffer, out var document) && PreviewController.IsFeatureOrSteps(document.FilePath)
            ? new BddBreakpointTagger(buffer, document) as ITagger<T>
            : null;
}

internal sealed class BddBreakpointTagger : ITagger<BddBreakpointTag>, IDisposable
{
    private readonly ITextBuffer _buffer;
    private readonly ITextDocument _document;

    public BddBreakpointTagger(ITextBuffer buffer, ITextDocument document)
    {
        _buffer = buffer;
        _document = document;
        BddBreakpointStore.Instance.Changed += OnChanged;
    }

    public event EventHandler<SnapshotSpanEventArgs>? TagsChanged;

    public IEnumerable<ITagSpan<BddBreakpointTag>> GetTags(NormalizedSnapshotSpanCollection spans)
    {
        if (spans.Count == 0)
        {
            yield break;
        }

        var snapshot = spans[0].Snapshot;
        foreach (var line in BddBreakpointStore.Instance.LinesOf(_document.FilePath))
        {
            if (line < 1 || line > snapshot.LineCount)
            {
                continue;
            }

            var extent = snapshot.GetLineFromLineNumber(line - 1).Extent;
            if (spans.IntersectsWith(extent))
            {
                yield return new TagSpan<BddBreakpointTag>(extent, BddBreakpointTag.Instance);
            }
        }
    }

    public void Dispose() => BddBreakpointStore.Instance.Changed -= OnChanged;

    private void OnChanged(object sender, string file)
    {
        if (string.Equals(file, _document.FilePath, StringComparison.OrdinalIgnoreCase))
        {
            var snapshot = _buffer.CurrentSnapshot;
            TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(new SnapshotSpan(snapshot, 0, snapshot.Length)));
        }
    }
}

[Export(typeof(IGlyphFactoryProvider))]
[Name("BrinellUatStepBreakpointGlyph")]
[Order(After = "VsTextMarker")]
[ContentType("text")]
[TagType(typeof(BddBreakpointTag))]
internal sealed class BddBreakpointGlyphFactoryProvider : IGlyphFactoryProvider
{
    public IGlyphFactory GetGlyphFactory(IWpfTextView view, IWpfTextViewMargin margin) => new BddBreakpointGlyphFactory();
}

// The same red dot as the preview's gutter, and as a C# breakpoint.
internal sealed class BddBreakpointGlyphFactory : IGlyphFactory
{
    private static readonly Brush Fill = Frozen(new SolidColorBrush(Color.FromRgb(0xE5, 0x14, 0x00)));

    public UIElement? GenerateGlyph(IWpfTextViewLine line, IGlyphTag tag)
        => tag is BddBreakpointTag
            ? new Ellipse { Width = 11, Height = 11, Fill = Fill, ToolTip = "BDD step breakpoint" }
            : null;

    private static Brush Frozen(Brush brush)
    {
        brush.Freeze();
        return brush;
    }
}

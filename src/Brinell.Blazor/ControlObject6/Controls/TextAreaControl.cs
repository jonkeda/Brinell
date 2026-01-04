using Brinell.Blazor.ControlObject6.Context;
using Brinell.Blazor.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;

namespace Brinell.Blazor.ControlObject6.Controls;

/// <summary>
/// TextArea control for Blazor.
/// Wraps &lt;textarea&gt; elements.
/// </summary>
public class TextAreaControl : AsyncTextControlBase
{
    /// <summary>
    /// Creates a new TextArea control.
    /// </summary>
    public TextAreaControl(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new TextArea control using TestId.
    /// </summary>
    public TextAreaControl(BlazorTestContext context, string testId, IAsyncPageObject? page = null)
        : base(context, testId, page)
    {
    }

    /// <summary>
    /// Gets the number of rows in the textarea.
    /// </summary>
    public virtual async Task<int?> GetRowsAsync(CancellationToken ct = default)
    {
        var rows = await GetLocator().GetAttributeAsync("rows");
        return int.TryParse(rows, out var value) ? value : null;
    }

    /// <summary>
    /// Gets the number of columns in the textarea.
    /// </summary>
    public virtual async Task<int?> GetColsAsync(CancellationToken ct = default)
    {
        var cols = await GetLocator().GetAttributeAsync("cols");
        return int.TryParse(cols, out var value) ? value : null;
    }

    /// <summary>
    /// Gets the max length of the textarea.
    /// </summary>
    public virtual async Task<int?> GetMaxLengthAsync(CancellationToken ct = default)
    {
        var maxLength = await GetLocator().GetAttributeAsync("maxlength");
        return int.TryParse(maxLength, out var value) ? value : null;
    }
}

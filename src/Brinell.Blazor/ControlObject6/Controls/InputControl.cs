using Brinell.Blazor.ControlObject6.Context;
using Brinell.Blazor.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;

namespace Brinell.Blazor.ControlObject6.Controls;

/// <summary>
/// Input control implementation for Blazor.
/// Inherits virtual async text input capabilities from AsyncTextControlBase.
/// </summary>
public class InputControl : AsyncTextControlBase
{
    /// <summary>
    /// Creates a new input control.
    /// </summary>
    public InputControl(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new input control using TestId.
    /// </summary>
    public InputControl(BlazorTestContext context, string testId, IAsyncPageObject? page = null)
        : base(context, testId, page)
    {
    }

    // All text input methods are inherited from AsyncTextControlBase as virtual methods
    // Override if Blazor-specific behavior is needed
}

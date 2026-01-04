using Brinell.Blazor.ControlObject6.Context;
using Brinell.Blazor.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;

namespace Brinell.Blazor.ControlObject6.Controls;

/// <summary>
/// Button control implementation for Blazor.
/// Inherits virtual async click capabilities from AsyncClickableControlBase.
/// </summary>
public class ButtonControl : AsyncClickableControlBase
{
    /// <summary>
    /// Creates a new button control.
    /// </summary>
    public ButtonControl(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new button control using TestId.
    /// </summary>
    public ButtonControl(BlazorTestContext context, string testId, IAsyncPageObject? page = null)
        : base(context, testId, page)
    {
    }

    // All click methods are inherited from AsyncClickableControlBase as virtual methods
    // Override if Blazor-specific behavior is needed
}

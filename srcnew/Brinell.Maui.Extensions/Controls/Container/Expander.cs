using Brinell.Core.Interfaces;
using Brinell.Maui.Controls.Internal;

namespace Brinell.Maui.Extensions.Controls.Container;

/// <summary>
/// MAUI Expander control: a disclosure container that shows or hides its content.
/// </summary>
/// <remarks>
/// Derives from <c>Base.ClickableControlBase</c> because an expander is fundamentally
/// clickable — clicking the header is how it toggles. Expanding itself is declared as a
/// capability (<see cref="IExpandableControlObject{TScope}"/>) and delegated to
/// <see cref="ExpandHelper"/> rather than inherited, since C# allows one base class and
/// expanding composes with clicking rather than replacing it.
/// </remarks>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public class Expander<TScope> : Brinell.Maui.Controls.Base.ClickableControlBase<TScope>,
    IExpandableControlObject<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a new expander control within the specified scope.
    /// </summary>
    public Expander(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a new expander control using the scope's default locator strategy.
    /// </summary>
    public Expander(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }

    /// <inheritdoc />
    public TScope Expand(int? timeoutMs = null)
        => RunDoWithElement(ExpandHelper.Expand, timeoutMs);

    /// <inheritdoc />
    public TScope Collapse(int? timeoutMs = null)
        => RunDoWithElement(ExpandHelper.Collapse, timeoutMs);

    /// <inheritdoc />
    public TScope ToggleExpanded(int? timeoutMs = null)
        => RunDoWithElement(ExpandHelper.Toggle, timeoutMs);

    /// <inheritdoc />
    public bool? IsExpanded() => ExpandHelper.IsExpanded(TryFindElement());

    /// <inheritdoc />
    public bool WaitExpanded(bool? expected, int? timeoutMs = null)
    {
        if (expected == null) return true;

        return RunWaitWithOptionalElement(expected,
            element => ExpandHelper.IsExpanded(element) == expected.Value,
            timeoutMs);
    }

    /// <summary>
    /// Asserts the expander is expanded.
    /// </summary>
    public TScope AssertExpanded(string? message = null, int? timeoutMs = null)
        => AssertExpanded(true, message, timeoutMs);

    /// <inheritdoc />
    public TScope AssertExpanded(bool? expected, string? message = null, int? timeoutMs = null)
        => RunAssertWithOptionalElement(expected,
            ExpandHelper.IsExpanded, (actual, expected1) => actual == expected1,
            message ?? $"Expected Expanded to be '{expected}'. Locator: {Locator}",
            timeoutMs);
}

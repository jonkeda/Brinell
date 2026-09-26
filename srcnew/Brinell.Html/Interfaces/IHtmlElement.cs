using System.Drawing;
using Brinell.Core;
using Brinell.Core.Locators;

namespace Brinell.Html.Interfaces;

// R9: Html owns this contract and does not derive from Core's IElement<T>; lookups make one
// attempt and never wait. See .my/html/design.md 4.2 and .docs/decisions/ad-010-maui-ahead-of-core.md.

/// <summary>
/// A DOM element, as the Html stack drives it.
/// </summary>
public interface IHtmlElement
{
    #region Identity

    /// <summary>
    /// Identifies this DOM node: two reads give the same key only for the same node. Read live, so
    /// the read doubles as the "still there" check (R6); a removed element throws
    /// <c>StaleElementException</c>.
    /// </summary>
    string InstanceKey { get; }

    #endregion

    #region State

    /// <summary>Whether the element is visible.</summary>
    bool Visible { get; }

    /// <summary>Whether the element is enabled.</summary>
    bool Enabled { get; }

    /// <summary>Whether a checkbox, radio button or option is selected.</summary>
    bool Selected { get; }

    /// <summary>The element's rendered text.</summary>
    string? Text { get; }

    /// <summary>The lower-case tag name.</summary>
    string? TagName { get; }

    /// <summary>The top-left corner, in page pixels.</summary>
    Point Location { get; }

    /// <summary>The size, in pixels.</summary>
    Size Size { get; }

    /// <summary>The bounding box, in page pixels.</summary>
    Rectangle Rect { get; }

    /// <summary>An attribute's value, or <c>null</c> when it is not set.</summary>
    string? GetAttribute(string name);

    /// <summary>An attribute's value as the DOM holds it.</summary>
    string? GetDomAttribute(string attributeName);

    /// <summary>A DOM property's value, as a string.</summary>
    string? GetDomProperty(string propertyName);

    /// <summary>A computed CSS value.</summary>
    string? GetCssValue(string propertyName);

    /// <summary>The element's inner HTML.</summary>
    string InnerHtml { get; }

    /// <summary>The element's outer HTML.</summary>
    string OuterHtml { get; }

    /// <summary>Whether a checkbox or radio button is checked.</summary>
    bool IsChecked { get; }

    /// <summary>An input's current value.</summary>
    string InputValue { get; }

    #endregion

    #region Actions

    /// <summary>Clicks the element.</summary>
    void Click();

    /// <summary>Types or sets text.</summary>
    void SendKeys(string text, TextInputMethod method = TextInputMethod.Keys);

    /// <summary>Clears an input.</summary>
    void Clear();

    /// <summary>Double-clicks the element.</summary>
    void DoubleClick();

    /// <summary>Right-clicks the element.</summary>
    void RightClick();

    /// <summary>Moves the pointer over the element.</summary>
    void Hover();

    /// <summary>Presses and holds.</summary>
    void LongPress(int durationMs = 1000);

    /// <summary>Scrolls the element into view, waiting at most <paramref name="timeoutMs"/>.</summary>
    void ScrollIntoView(int timeoutMs = 5000);

    /// <summary>Drags from one page point to another.</summary>
    void Swipe(int startX, int startY, int endX, int endY, int durationMs = 500);

    /// <summary>Submits the element's form.</summary>
    void Submit();

    /// <summary>Sets an input's value in one step.</summary>
    void Fill(string value);

    /// <summary>Selects an option by value.</summary>
    void SelectOption(string value);

    /// <summary>Selects several options by value.</summary>
    void SelectOption(string[] values);

    /// <summary>Checks a checkbox or radio button.</summary>
    void Check();

    /// <summary>Unchecks a checkbox.</summary>
    void Uncheck();

    /// <summary>Gives the element focus.</summary>
    void Focus();

    /// <summary>Takes focus away from the element.</summary>
    void Blur();

    /// <summary>Runs a script against the element.</summary>
    void Evaluate(string expression);

    /// <summary>Runs a script against the element and returns its result.</summary>
    T Evaluate<T>(string expression);

    #endregion

    #region Child lookup

    /// <summary>The first matching descendant, or <c>null</c>. One attempt; never waits.</summary>
    IHtmlElement? TryFindElement(Locator locator);

    /// <summary>Every matching descendant. One attempt; never waits.</summary>
    IReadOnlyList<IHtmlElement> FindElements(Locator locator);

    #endregion
}

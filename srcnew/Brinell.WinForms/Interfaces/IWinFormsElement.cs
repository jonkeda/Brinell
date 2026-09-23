using System.Drawing;
using Brinell.Core;
using Brinell.Core.Interfaces;
using Brinell.Core.Locators;

namespace Brinell.WinForms.Interfaces;

/// <summary>
/// WinForms-specific element interface extending <see cref="IElement{TSelf}"/>.
/// Stubs DOM access methods (not applicable for desktop WinForms).
/// </summary>
public interface IWinFormsElement : IElement<IWinFormsElement>
{
    #region DOM Access (Not Applicable)

    /// <summary>
    /// Gets a DOM attribute value. Always returns null for WinForms.
    /// </summary>
    string? GetDomAttribute(string attributeName);

    /// <summary>
    /// Gets a DOM property value. Always returns null for WinForms.
    /// </summary>
    string? GetDomProperty(string propertyName);

    /// <summary>
    /// Gets a computed CSS value. Always returns null for WinForms.
    /// </summary>
    string? GetCssValue(string propertyName);

    #endregion
}

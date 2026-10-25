using Brinell.Core.Exceptions;

namespace Brinell.Html.Bunit;

/// <summary>
/// A member that only a browser can answer (layout, style sheets, script, pixels), asked on the bUnit
/// head: the test needs the browser head, which it says rather than passing wrongly. It ends the call
/// at once (<see cref="IEndsCall"/>): no wait makes a browser appear.
/// </summary>
public sealed class BrowserOnlyException : BrinellException, IEndsCall
{
    public BrowserOnlyException(string member)
        : base($"'{member}' needs a browser: the bUnit head has no layout, style sheets or script. Run this test on the browser head.")
    {
        Member = member;
    }

    /// <summary>The member that was asked.</summary>
    public string Member { get; }
}

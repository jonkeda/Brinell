using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Brinell.Maui.FlaUI;

/// <summary>
/// How UI Automation reports an element that is no longer in the tree.
/// </summary>
/// <remarks>
/// <para>
/// UIA answers a call on a removed element with <c>UIA_E_ELEMENTNOTAVAILABLE</c>
/// (<c>0x80040201</c>). FlaUI surfaces it as its own <c>ElementNotAvailableException</c> when the
/// call went through its COM wrapper, as a <see cref="COMException"/> when it did not, and as a
/// <see cref="Win32Exception"/> ("Unexpected HRESULT") from some window calls.
/// </para>
/// <para>
/// This is the one place the driver recognizes it. <see cref="FlaUIMauiElement"/> turns it into
/// <c>StaleElementException</c>, so nothing above the driver names a platform type
/// (<c>.my/stale-readiness/design.md</c>, R5).
/// </para>
/// </remarks>
internal static class FlaUIErrors
{
    /// <summary>UIA_E_ELEMENTNOTAVAILABLE.</summary>
    internal const int ElementNotAvailable = unchecked((int)0x80040201);

    /// <summary>Whether <paramref name="error"/> says the element is no longer in the tree.</summary>
    internal static bool IsElementGone(Exception error) => error switch
    {
        global::FlaUI.Core.Exceptions.ElementNotAvailableException => true,
        COMException com => com.HResult == ElementNotAvailable,
        Win32Exception win32 => win32.NativeErrorCode == ElementNotAvailable || win32.HResult == ElementNotAvailable,
        _ => false
    };
}

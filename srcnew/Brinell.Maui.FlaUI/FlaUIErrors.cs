using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Brinell.Maui.FlaUI;

/// <summary>
/// How UI Automation reports an element that is no longer in the tree.
/// </summary>
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

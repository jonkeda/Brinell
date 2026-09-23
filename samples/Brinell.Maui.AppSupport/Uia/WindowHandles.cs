using Microsoft.Maui.Controls;

#if WINDOWS
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
#endif

namespace Brinell.Maui.AppSupport.Uia;

/// <summary>
/// Finds the top-level window behind a MAUI element.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why by handle and not by MAUI's window object.</b> The bridge needs a native window to
/// parent itself to, and there is exactly one per top-level window no matter how the app is
/// structured. Keying everything on that handle means the bridge does not care whether the app
/// uses <c>Shell</c>, a <c>NavigationPage</c>, several windows, or none of the above - and it
/// keeps the code that manages the bridge free of MAUI's window plumbing, which is the part
/// most likely to change between MAUI versions.
/// </para>
/// <para>
/// A WinUI 3 XAML element has no window handle of its own; only the top-level window does, with
/// the XAML content living in a child window inside it. So the route is: element to its
/// <c>XamlRoot</c>, root to its window handle.
/// </para>
/// <para>
/// <b>Off Windows this returns zero, and that is the answer, not a stub.</b> There is no
/// UI Automation and no bridge on Android or iOS - gestures there are real touch input, which
/// needs none of this. Reporting no window is what makes the rest of the bridge inert without
/// a single platform check anywhere else.
/// </para>
/// </remarks>
internal static class WindowHandles
{
    /// <summary>The top-level window containing a MAUI element, or zero.</summary>
    /// <param name="element">The element to locate.</param>
    /// <returns>The window handle, or <see cref="IntPtr.Zero"/> if there is none.</returns>
    internal static IntPtr RootOf(VisualElement element)
    {
#if WINDOWS
        return element.Handler?.PlatformView is FrameworkElement platformView
            ? RootOf(platformView)
            : IntPtr.Zero;
#else
        _ = element;
        return IntPtr.Zero;
#endif
    }

#if WINDOWS
    private const uint GA_ROOT = 2;

    /// <summary>The top-level window containing a WinUI element, or zero.</summary>
    /// <param name="platformView">The realised WinUI element.</param>
    /// <returns>The window handle, or <see cref="IntPtr.Zero"/>.</returns>
    internal static IntPtr RootOf(FrameworkElement platformView)
    {
        try
        {
            var xamlRoot = platformView.XamlRoot;
            if (xamlRoot is null)
            {
                return IntPtr.Zero;
            }

            var contentWindow = Microsoft.UI.Win32Interop.GetWindowFromWindowId(
                xamlRoot.ContentIslandEnvironment.AppWindowId);

            // The island's window is a child of the frame window the user sees. UI Automation
            // is happy with either, but the bridge must be parented consistently or two windows
            // of the same app would each get their own bridge and neither would have all the
            // targets.
            return contentWindow == IntPtr.Zero ? IntPtr.Zero : GetAncestor(contentWindow, GA_ROOT);
        }
        catch (Exception)
        {
            return IntPtr.Zero;
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr hwnd, uint flags);
#endif
}

using Brinell.Uia.Provider;
using Microsoft.Maui.Controls;

#if WINDOWS
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
#endif

namespace Brinell.Maui.AppSupport.Uia;

/// <summary>
/// Where a MAUI element is on the screen, in the physical pixels UI Automation speaks.
/// </summary>
/// <remarks>
/// <para>
/// Reported so a bridge element sits over the element it stands for. Nothing depends on it
/// being exact - the bridge refuses hit tests and never takes focus - but a bridge element with
/// no bounds reports itself offscreen, and a raw-tree dump with every element at the origin is
/// much harder to read than one that lines up.
/// </para>
/// <para>
/// <b>Three coordinate systems.</b> A WinUI element knows its position in device-independent
/// pixels relative to the XAML root; the XAML root sits at the window's client origin; and UI
/// Automation wants physical pixels relative to the desktop. Missing the rasterization scale is
/// invisible at 100% display scaling and wrong by a quarter at 125%, which is the default on
/// most laptops.
/// </para>
/// </remarks>
internal static class ElementBounds
{
    /// <summary>Reads an element's screen rectangle, if it has one.</summary>
    /// <param name="element">The MAUI element.</param>
    /// <param name="bounds">Its screen rectangle, when this returns true.</param>
    /// <returns>Whether the element is laid out and its position could be read.</returns>
    internal static bool TryGetScreenBounds(VisualElement element, out BrinellBounds bounds)
    {
        bounds = BrinellBounds.Empty;

#if WINDOWS
        if (element.Handler?.PlatformView is not FrameworkElement platformView)
        {
            return false;
        }

        try
        {
            if (platformView.XamlRoot is null || platformView.ActualWidth <= 0)
            {
                // Constructed but not laid out. Not an error: it is what an element on a page
                // that has not been shown yet looks like.
                return false;
            }

            var scale = platformView.XamlRoot.RasterizationScale;
            var origin = platformView
                .TransformToVisual(null)
                .TransformPoint(new Windows.Foundation.Point(0, 0));

            var hwnd = WindowHandles.RootOf(platformView);
            if (hwnd == IntPtr.Zero
                || !NativeMethods.GetClientOrigin(hwnd, out var clientX, out var clientY))
            {
                return false;
            }

            bounds = new BrinellBounds(
                clientX + (origin.X * scale),
                clientY + (origin.Y * scale),
                platformView.ActualWidth * scale,
                platformView.ActualHeight * scale);

            return true;
        }
        catch (Exception)
        {
            // Reading layout from a torn-down element throws in more ways than it is worth
            // enumerating, and none of them should fail a verb. No bounds is a valid answer.
            return false;
        }
#else
        _ = element;
        return false;
#endif
    }

#if WINDOWS
    private static class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ClientToScreen(IntPtr hwnd, ref POINT point);

        /// <summary>The screen position of a window's client area origin.</summary>
        internal static bool GetClientOrigin(IntPtr hwnd, out double x, out double y)
        {
            var point = new POINT { X = 0, Y = 0 };

            if (!ClientToScreen(hwnd, ref point))
            {
                x = 0;
                y = 0;
                return false;
            }

            x = point.X;
            y = point.Y;
            return true;
        }
    }
#endif
}

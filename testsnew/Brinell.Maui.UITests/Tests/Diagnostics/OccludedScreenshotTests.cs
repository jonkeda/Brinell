using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Runtime.InteropServices;
using Xunit.Abstractions;

// Brinell.Maui's global usings bring in Microsoft.Maui.Graphics, which has its own Color.
using DrawingColor = System.Drawing.Color;

namespace Brinell.Maui.UITests.Tests.Diagnostics;

/// <summary>
/// Proves a screenshot is of the app under test even when the app is behind another window.
/// </summary>
/// <remarks>
/// <para>
/// <b>What this guards.</b> FlaUI's <c>Capture.Element</c> reads the screen at the element's
/// bounding rectangle, so an occluded app screenshots as whatever is on top. Brinell's own
/// guidance is to inspect screenshots first when a UI test fails, which makes a
/// confidently-wrong screenshot worse than a missing one. The driver now asks the window to
/// render itself instead; this test is the evidence that it works.
/// </para>
/// <para>
/// <b>Windows only, and excluded from the mobile head.</b> Every other file under
/// <c>Brinell.Maui.UITests</c> is linked into <c>Brinell.Maui.UITests.Mobile</c> and compiled
/// against Appium on plain <c>net10.0</c>. This one needs <c>System.Drawing</c> and user32, so
/// it is excluded by name in that project rather than being quietly platform-guarded.
/// </para>
/// <para>
/// <b>The occluder is checked, not assumed.</b> If the covering window failed to appear, every
/// other assertion here would pass while proving nothing, so
/// <see cref="Screenshot_OfOccludedWindow_ShowsTheApp"/> asserts the screen really did go dark
/// before it draws any conclusion from the capture.
/// </para>
/// </remarks>
[Collection("Maui")]
[Trait("Category", "UITest")]
[Trait("Pattern", "Diagnostics")]
public class OccludedScreenshotTests
{
    /// <summary>Fraction of sampled pixels that must match for two captures to be the same image.</summary>
    private const double SameImageThreshold = 0.95;

    /// <summary>Per-channel tolerance, absorbing compression and sub-pixel rendering noise.</summary>
    private const int ChannelTolerance = 8;

    /// <summary>Samples per axis used to compare two captures.</summary>
    private const int ComparisonGrid = 64;

    private readonly MauiFixture _fixture;
    private readonly ITestOutputHelper _output;

    public OccludedScreenshotTests(MauiFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    /// <summary>
    /// A screenshot taken while the app is fully covered still shows the app.
    /// </summary>
    [Fact]
    public void Screenshot_OfOccludedWindow_ShowsTheApp()
    {
        _fixture.NavigateToMain();

        using var frontmost = Decode(_fixture.Context.TakeScreenshot());
        ReportScreenAgreement(frontmost);

        using (ScreenOccluder.CoverPrimaryScreen())
        {
            // The instrument check. Without this, a covering window that never appeared would
            // make every assertion below pass for the wrong reason.
            Assert.True(
                PrimaryScreenIsMostlyDark(),
                "The occluding window did not cover the screen, so this test proves nothing.");

            using var occluded = Decode(_fixture.Context.TakeScreenshot());

            Assert.Equal(frontmost.Size, occluded.Size);

            Assert.False(
                IsUniformlyDark(occluded),
                "The occluded capture came back black: the window declined to render itself and " +
                "the screen-reading fallback captured the occluder instead.");

            var similarity = Similarity(frontmost, occluded);
            Assert.True(
                similarity >= SameImageThreshold,
                $"Occluded capture matched the frontmost capture only {similarity:P1} " +
                $"(needed {SameImageThreshold:P0}). The screenshot is of something other than the app.");
        }
    }

    /// <summary>
    /// Reports how closely the rendered capture matches what is physically on screen.
    /// </summary>
    /// <remarks>
    /// Reported rather than asserted, in the same spirit as the layout probes: a low reading
    /// here means <c>PrintWindow</c> rendered at an unexpected size or offset — worth knowing,
    /// and the first thing to look at if the occluded assertions start failing — but
    /// <c>GetWindowRect</c> includes the DWM resize border while the rendered content does not,
    /// so a benign offset is expected and there is no calibrated threshold to gate on yet.
    /// </remarks>
    private void ReportScreenAgreement(Bitmap rendered)
    {
        var handle = ParseWindowHandle(_fixture.Context.Driver.CurrentWindowHandle);
        if (handle == 0 || !GetWindowRect(handle, out var bounds))
        {
            _output.WriteLine("Screen agreement: window rectangle unavailable.");
            return;
        }

        var width = bounds.Right - bounds.Left;
        var height = bounds.Bottom - bounds.Top;
        if (width <= 0 || height <= 0)
        {
            _output.WriteLine("Screen agreement: window rectangle is empty.");
            return;
        }

        using var fromScreen = new Bitmap(width, height, PixelFormat.Format32bppRgb);
        using (var graphics = Graphics.FromImage(fromScreen))
        {
            graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, new System.Drawing.Size(width, height));
        }

        _output.WriteLine(
            $"Screen agreement: rendered {rendered.Width}x{rendered.Height}, " +
            $"screen {width}x{height}, similarity {Similarity(rendered, fromScreen):P1}.");
    }

    private static Bitmap Decode(byte[] png)
    {
        using var stream = new MemoryStream(png);

        // Copy out: a Bitmap built over a stream keeps the stream alive for its lifetime.
        using var decoded = new Bitmap(stream);
        return new Bitmap(decoded);
    }

    /// <summary>Fraction of sampled positions where two images agree within tolerance.</summary>
    private static double Similarity(Bitmap left, Bitmap right)
    {
        var matched = 0;
        var sampled = 0;

        for (var row = 0; row < ComparisonGrid; row++)
        {
            for (var column = 0; column < ComparisonGrid; column++)
            {
                var leftPixel = SampleAt(left, row, column);
                var rightPixel = SampleAt(right, row, column);
                sampled++;

                if (Math.Abs(leftPixel.R - rightPixel.R) <= ChannelTolerance &&
                    Math.Abs(leftPixel.G - rightPixel.G) <= ChannelTolerance &&
                    Math.Abs(leftPixel.B - rightPixel.B) <= ChannelTolerance)
                {
                    matched++;
                }
            }
        }

        return sampled == 0 ? 0d : (double)matched / sampled;
    }

    private static DrawingColor SampleAt(Bitmap bitmap, int row, int column)
        => bitmap.GetPixel(
            Math.Min(bitmap.Width - 1, bitmap.Width * column / ComparisonGrid),
            Math.Min(bitmap.Height - 1, bitmap.Height * row / ComparisonGrid));

    private static bool IsUniformlyDark(Bitmap bitmap)
    {
        for (var row = 0; row < ComparisonGrid; row++)
        {
            for (var column = 0; column < ComparisonGrid; column++)
            {
                var pixel = SampleAt(bitmap, row, column);
                if (pixel.R > ChannelTolerance || pixel.G > ChannelTolerance || pixel.B > ChannelTolerance)
                    return false;
            }
        }

        return true;
    }

    private static bool PrimaryScreenIsMostlyDark()
    {
        var width = GetSystemMetrics(SmCxScreen);
        var height = GetSystemMetrics(SmCyScreen);
        if (width <= 0 || height <= 0)
            return false;

        using var screen = new Bitmap(width, height, PixelFormat.Format32bppRgb);
        using (var graphics = Graphics.FromImage(screen))
        {
            graphics.CopyFromScreen(0, 0, 0, 0, new System.Drawing.Size(width, height));
        }

        return IsUniformlyDark(screen);
    }

    private static nint ParseWindowHandle(string handle)
        => long.TryParse(handle, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? (nint)value
            : 0;

    private const int SmCxScreen = 0;
    private const int SmCyScreen = 1;

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(nint hwnd, out ScreenRect rect);

    [StructLayout(LayoutKind.Sequential)]
    private struct ScreenRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    /// <summary>
    /// A topmost black window covering the primary screen, for as long as it is held.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Uses the predefined <c>STATIC</c> class with <c>SS_BLACKRECT</c>, so there is no window
    /// class to register and no message loop to run — the window paints itself black on the
    /// <c>WM_PAINT</c> that <c>UpdateWindow</c> sends directly.
    /// </para>
    /// <para>
    /// Shown with <c>SW_SHOWNOACTIVATE</c>: taking the foreground would be an odd thing for a
    /// test whose subject is not needing the foreground.
    /// </para>
    /// </remarks>
    private sealed class ScreenOccluder : IDisposable
    {
        private const int WsPopup = unchecked((int)0x80000000);
        private const int WsVisible = 0x10000000;
        private const int SsBlackRect = 0x00000004;
        private const int WsExTopmost = 0x00000008;
        private const int WsExNoActivate = 0x08000000;
        private const int SwShowNoActivate = 4;

        private nint _handle;

        private ScreenOccluder(nint handle) => _handle = handle;

        internal static ScreenOccluder CoverPrimaryScreen()
        {
            var handle = CreateWindowEx(
                WsExTopmost | WsExNoActivate,
                "STATIC",
                "Brinell screenshot occluder",
                WsPopup | WsVisible | SsBlackRect,
                0, 0,
                GetSystemMetrics(SmCxScreen),
                GetSystemMetrics(SmCyScreen),
                0, 0, 0, 0);

            if (handle == 0)
                throw new InvalidOperationException("Could not create the occluding window.");

            ShowWindow(handle, SwShowNoActivate);
            UpdateWindow(handle);

            return new ScreenOccluder(handle);
        }

        public void Dispose()
        {
            if (_handle == 0)
                return;

            DestroyWindow(_handle);
            _handle = 0;
        }

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern nint CreateWindowEx(
            int exStyle, string className, string windowName, int style,
            int x, int y, int width, int height,
            nint parent, nint menu, nint instance, nint param);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowWindow(nint hwnd, int command);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UpdateWindow(nint hwnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyWindow(nint hwnd);
    }
}

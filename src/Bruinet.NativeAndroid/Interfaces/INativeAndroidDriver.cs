using Bruinet.Core.Interfaces;
using Bruinet.Core.Recording;

namespace Bruinet.NativeAndroid.Interfaces;

/// <summary>
/// The Appium session on one Android device: the app under test, the screen, and the keys and
/// links a test gives it. Lookups make one attempt; the call around them is what waits.
/// </summary>
public interface INativeAndroidDriver : IDiagnosticDriver, IDisposable
{
    /// <summary>The app under test's package; null when the session names none.</summary>
    string? AppPackage { get; }

    /// <summary>The package of the app in front now.</summary>
    string? CurrentPackage { get; }

    /// <summary>The activity in front now (<c>.MainActivity</c>).</summary>
    string? CurrentActivity { get; }

    /// <summary>Whether the soft keyboard is shown.</summary>
    bool IsKeyboardShown { get; }

    /// <summary>The device, for the screen recorder; null once the session is gone.</summary>
    RecordingTarget? RecordingTarget { get; }

    /// <summary>Whether the app under test is running in the foreground.</summary>
    bool IsAppInForeground();

    /// <summary>The views that match now, on the whole screen. One attempt.</summary>
    IReadOnlyList<INativeAndroidElement> FindElements(Locator locator);

    /// <summary>
    /// Scrolls the first scrollable view until a view matching <paramref name="locator"/> is shown.
    /// One attempt; false when nothing scrolls or the view did not come into view.
    /// </summary>
    bool TryScrollIntoView(Locator locator);

    /// <summary>
    /// Scrolls <paramref name="scrollable"/> from its start until a view matching
    /// <paramref name="locator"/> is shown in it (<c>UiScrollable.scrollIntoView</c>). One attempt;
    /// false when either cannot be expressed as a <c>UiSelector</c> or the view did not come into view.
    /// </summary>
    bool TryScrollIntoView(Locator locator, Locator scrollable);

    /// <summary>Opens <paramref name="uri"/> in the app under test.</summary>
    void OpenDeepLink(string uri);

    /// <summary>Presses the system Back key.</summary>
    void Back();

    /// <summary>Presses an Android key (<c>66</c> is Enter).</summary>
    void PressKeyCode(int keyCode);

    /// <summary>Hides the soft keyboard when it is shown.</summary>
    void HideKeyboard();

    /// <summary>Brings the app under test to the front, starting it when it is not running.</summary>
    void ActivateApp();

    /// <summary>Stops the app under test.</summary>
    void TerminateApp();

    /// <summary>Stops and restarts the app under test.</summary>
    void ResetAppState();

    /// <summary>The screen as PNG bytes.</summary>
    byte[] TakeScreenshot();

    /// <summary>Saves the screen as a PNG file.</summary>
    void SaveScreenshot(string path);
}

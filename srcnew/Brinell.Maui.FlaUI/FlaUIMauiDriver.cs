using System.Diagnostics;
using System.Runtime.InteropServices;
using Brinell.Core;
using Brinell.Core.Exceptions;
using Brinell.Core.Utilities;
using Brinell.Maui.Configuration;
using Brinell.Maui.FlaUI.Bridge;
using Brinell.Maui.FlaUI.Windowing;
using Brinell.Uia;
using Brinell.Maui.Enums;
using FlaUI.Core.Capturing;
using FlaUI.Core.Definitions;

namespace Brinell.Maui.FlaUI;

/// <summary>
/// FlaUI-based implementation of <see cref="IMauiDriver"/> for Windows platform.
/// Provides native Windows UI Automation support for MAUI desktop apps.
/// </summary>
/// <remarks>
/// What the window needs - staying attached, staying behind, being placed, the real mouse - lives
/// in <c>Windowing/</c> (step 104). This class is launch, lookup, and the app-level verbs.
/// </remarks>
public sealed class FlaUIMauiDriver : IMauiDriver, IDisposable
{
    private readonly UIA3Automation _automation;
    private readonly Application? _application;
    private readonly AppWindow _window;
    private readonly QuietWindow? _quiet;
    private readonly ConditionFactory _conditionFactory;
    private bool _disposed;

    /// <summary>
    /// Creates a new FlaUIMauiDriver for an existing window.
    /// </summary>
    /// <param name="windowHandle">The window handle to attach to.</param>
    public FlaUIMauiDriver(IntPtr windowHandle)
    {
        _automation = new UIA3Automation();
        _window = AppWindow.FromHandle(_automation, windowHandle);
        _conditionFactory = new ConditionFactory(_automation.PropertyLibrary);
    }

    /// <summary>
    /// Creates a new FlaUIMauiDriver by launching an application.
    /// </summary>
    /// <param name="executablePath">Path to the application executable.</param>
    /// <param name="arguments">Optional command line arguments.</param>
    public FlaUIMauiDriver(string executablePath, string? arguments = null)
        : this(executablePath, arguments, environment: null)
    {
    }

    /// <summary>
    /// Creates a new FlaUIMauiDriver by launching an application with extra environment variables.
    /// </summary>
    /// <param name="executablePath">Path to the application executable.</param>
    /// <param name="arguments">Optional command line arguments.</param>
    /// <param name="environment">
    /// Variables set on the launched process only (<c>MauiDriverOptions.LaunchSettings</c>): the
    /// test process's own environment is not changed, so each launch carries its own settings.
    /// </param>
    public FlaUIMauiDriver(string executablePath, string? arguments, IReadOnlyDictionary<string, string>? environment)
    {
        _automation = new UIA3Automation();

        var processStartInfo = new ProcessStartInfo(executablePath)
        {
            Arguments = arguments ?? string.Empty,

            // FALSE, AND THIS MATTERS MORE THAN IT LOOKS.
            //
            // With UseShellExecute the launch goes through the shell, and what the child
            // inherits is not reliably this process's environment. Measured: roughly one run in
            // six came up with no bridge at all - every test failing with "no element published
            // on the app's bridge", for seventeen seconds at a time, against an app that was
            // running perfectly well and had simply never been told to instrument itself.
            UseShellExecute = false,
        };

        // Ask the app to turn its gesture bridge on. Asking is all this is: an app that was not
        // built with a bridge has nothing to turn on - see BrinellBridgeGate.
        processStartInfo.Environment[BrinellBridgeGate.EnableVariable] = "1";

        foreach (var (name, value) in environment ?? new Dictionary<string, string>())
        {
            processStartInfo.Environment[name] = value;
        }

        // Whoever the user was working in before the run. Windows hands a freshly launched
        // process the foreground; QuietWindow hands it back.
        var previousForeground = NativeMethods.GetForegroundWindow();

        var process = Process.Start(processStartInfo)
            ?? throw new InvalidOperationException($"Failed to start process: {executablePath}");

        // Before anything waits on the app - see QuietWindow.ForLaunch.
        _quiet = QuietWindow.ForLaunch(process.Id, previousForeground);

        process.WaitForInputIdle();

        _application = Application.Attach(process);

        var window = _application.GetMainWindow(_automation, TimeSpan.FromSeconds(30))
            ?? throw new InvalidOperationException("Failed to get main window");
        _window = new AppWindow(_automation, window);
        _conditionFactory = new ConditionFactory(_automation.PropertyLibrary);

        WindowPlacement.ApplyRequested(_window);
        _quiet.Settle(_window.Handle);
    }

    /// <summary>
    /// Creates a new FlaUIMauiDriver by attaching to a running process.
    /// </summary>
    /// <param name="process">The process to attach to.</param>
    public FlaUIMauiDriver(Process process)
    {
        _automation = new UIA3Automation();
        _application = Application.Attach(process);

        var window = _application.GetMainWindow(_automation, TimeSpan.FromSeconds(30))
            ?? throw new InvalidOperationException("Failed to get main window");
        _window = new AppWindow(_automation, window);
        _conditionFactory = new ConditionFactory(_automation.PropertyLibrary);
    }

    #region Platform

    /// <inheritdoc />
    public MauiPlatform Platform => MauiPlatform.Windows;

    #endregion

    #region The app

    /// <inheritdoc />
    /// <remarks>
    /// Stands for the application window, read afresh on every use - the window element can be
    /// retired by UI Automation and attached again. <c>TryFindByScrolling</c> keeps the interface
    /// default, null: UIA keeps scrolled-off-screen elements in the tree with
    /// <c>IsOffscreen=true</c>, so scrolling reveals nothing a plain lookup missed.
    /// </remarks>
    public IMauiElement AppElement => FlaUIMauiElement.ForApp(this);

    /// <inheritdoc />
    /// <remarks>
    /// Read from a tree dump, not reasoned about: Windows reports WinUI's own <c>navViewItem</c>s in
    /// place of anything the app wrote, inside hosts WinUI names.
    /// </remarks>
    public ShellChromeLocators ShellChrome { get; } = new(
        TabHost: Locator.ByAutomationId("TopNavMenuItemsHost"),
        Tab: Locator.ByControlType("TabItem"),
        FlyoutHost: Locator.ByAutomationId("MenuItemsHost"),
        FlyoutItem: Locator.ByControlType("ListItem"));

    #endregion

    #region Internal

    /// <summary>
    /// Gets the condition factory for building search conditions.
    /// </summary>
    internal ConditionFactory ConditionFactory => _conditionFactory;

    /// <summary>
    /// Gets the underlying automation instance.
    /// </summary>
    internal UIA3Automation Automation => _automation;

    /// <summary>The app's top-level window. Where a bridge lookup starts.</summary>
    /// <remarks>
    /// Re-attached when UI Automation has retired it - see <see cref="AppWindow"/> and
    /// <c>.my/fix/rca-app-freeze-was-a-stale-root.md</c>.
    /// </remarks>
    internal AutomationElement RootElement => _window.Element;

    /// <summary>
    /// Whether the app this driver launched has exited.
    /// </summary>
    /// <remarks>
    /// Tells an element that is gone because the app is gone from one that was only replaced: the
    /// first is <c>AppUnavailableException</c>, the second <c>StaleElementException</c>. Always
    /// false for a driver that attached rather than launched, since it has no process to watch.
    /// </remarks>
    internal bool AppHasExited
    {
        get
        {
            try
            {
                return _application?.HasExited == true;
            }
            catch (InvalidOperationException)
            {
                // The process object no longer answers; it is gone.
                return true;
            }
        }
    }

    /// <summary>How many times the root element had gone stale and was attached again.</summary>
    /// <remarks>Diagnostics. A number that grows during a run is the invalidation happening.</remarks>
    public int RootReattachments => _window.Reattachments;

    /// <summary>
    /// How many times the app under test took the foreground and had to be put back.
    /// </summary>
    /// <remarks>
    /// <b>Zero is the claim this framework makes.</b> Always zero for a driver that attached rather
    /// than launched, since only a launch is watched. See <see cref="QuietWindow"/>.
    /// </remarks>
    public int ForegroundGrabs => _quiet?.ForegroundGrabs ?? 0;

    #endregion

    #region Element Finding

    /// <summary>
    /// Asks the app's bridge: the first published target that answers <paramref name="verb"/>.
    /// </summary>
    /// <remarks>
    /// The bridge lookup walks the tree and treats a target it cannot read as "not there". When
    /// nothing answered because the launched app has exited, that is not "not ready yet", which a
    /// call would wait out; it is <see cref="AppUnavailableException"/>, which ends the call at once
    /// (R0).
    /// </remarks>
    private Bridge.BridgeVerbResult Exchange(BrinellVerb verb, string argument = "")
    {
        var answer = Bridge.BridgeVerbRunner.ExchangeAnywhere(RootElement, Automation, verb, argument);
        if (!answer.Delivered && AppHasExited)
        {
            throw new AppUnavailableException("the application process has exited.");
        }

        return answer;
    }

    /// <inheritdoc />
    public IReadOnlyList<IMauiElement> FindElements(Locator locator)
    {
        var condition = locator.ToCondition(_conditionFactory);

        try
        {
            return RootElement.FindAllDescendants(condition)
                .Select(e => new FlaUIMauiElement(e, this))
                .ToList();
        }
        catch (Exception error) when (FlaUIErrors.IsElementGone(error) && AppHasExited)
        {
            throw new AppUnavailableException("the application process has exited.", error);
        }
    }

    /// <summary>
    /// Finds a piece of window chrome the driver acts on itself, waiting for it to appear.
    /// </summary>
    /// <remarks>
    /// Only for the driver's own actions (opening the navigation pane, dismissing a flyout), which
    /// run inside a control's action rather than inside its poll. Everything a control looks up
    /// goes through <see cref="FindElements"/>, which never waits.
    /// </remarks>
    /// <param name="locator">The chrome element's locator.</param>
    /// <param name="timeoutMs">How long to wait for it to appear.</param>
    /// <returns>The element.</returns>
    /// <exception cref="ElementNotFoundException">It did not appear in time.</exception>
    internal IMauiElement FindChrome(Locator locator, int timeoutMs)
    {
        var condition = locator.ToCondition(_conditionFactory);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        do
        {
            var found = RootElement.FindFirstDescendant(condition);
            if (found != null)
            {
                return new FlaUIMauiElement(found, this);
            }

            WaitHelper.Pause(100);
        }
        while (stopwatch.ElapsedMilliseconds < timeoutMs);

        throw new ElementNotFoundException(locator);
    }

    #endregion
    
    #region Window Management
    
    /// <inheritdoc />
    public string CurrentWindowHandle => RootElement.Properties.NativeWindowHandle.Value.ToString();
    
    /// <inheritdoc />
    public IReadOnlyCollection<string> WindowHandles
    {
        get
        {
            if (_application != null)
            {
                return _application.GetAllTopLevelWindows(_automation)
                    .Select(w => w.Properties.NativeWindowHandle.Value.ToString())
                    .ToList();
            }
            return new[] { CurrentWindowHandle };
        }
    }
    
    #endregion
    
    #region Session Management
    
    /// <inheritdoc />
    public void Quit()
    {
        _application?.Close();
    }
    
    /// <inheritdoc />
    public void Close()
    {
        if (RootElement.Patterns.Window.IsSupported)
        {
            RootElement.Patterns.Window.Pattern.Close();
        }
    }
    
    #endregion

    #region Screenshots
    
    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Asks the window to render itself first, so a screenshot is of the app even when the app
    /// is behind something else. The fallback — FlaUI's <c>Capture.Element</c> — reads the
    /// screen at the element's bounding rectangle, and with the app occluded that is a picture
    /// of whatever is on top. A failure diagnostic showing the wrong application is worse than
    /// none, because nothing about it looks wrong.
    /// </para>
    /// <para>
    /// The fallback is kept rather than replaced: <c>PW_RENDERFULLCONTENT</c> returns black for
    /// some GPU-composed content, and a minimized window has nothing to render. Reading the
    /// screen is wrong only when the window is covered, and right the rest of the time.
    /// </para>
    /// </remarks>
    public byte[] GetScreenshot()
    {
        var windowContent = TryCaptureWindowContent();
        if (windowContent != null)
        {
            using (windowContent)
                return ToPng(windowContent);
        }

        using var capture = Capture.Element(RootElement);
        return ToPng(capture.Bitmap);
    }

    /// <summary>
    /// Captures the app window's own content, or null when it declined to render.
    /// </summary>
    private System.Drawing.Bitmap? TryCaptureWindowContent()
    {
        var bitmap = WindowCapture.TryCapture(_window.Handle);
        if (bitmap == null)
            return null;

        if (!WindowCapture.LooksBlank(bitmap))
            return bitmap;

        // Rendered, but empty: the GPU-composition failure mode. Reading the screen is the
        // better answer even though it may catch an overlapping window.
        bitmap.Dispose();
        return null;
    }

    private static byte[] ToPng(System.Drawing.Bitmap bitmap)
    {
        using var stream = new MemoryStream();
        bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        return stream.ToArray();
    }
    
    #endregion
    
    #region Context Switching (Not applicable for FlaUI)
    
    /// <inheritdoc />
    public string Context
    {
        get => "NATIVE_APP";
        set { } // No-op for FlaUI
    }
    
    /// <inheritdoc />
    public IReadOnlyCollection<string> Contexts => new[] { "NATIVE_APP" };
    
    #endregion
    
    #region Script Execution (Not applicable for FlaUI)
    
    /// <inheritdoc />
    public object? ExecuteScript(string script, params object[] args)
    {
        // FlaUI doesn't support script execution
        throw new NotSupportedException("Script execution is not supported by FlaUI driver");
    }
    
    #endregion
    
    #region IDiagnosticDriver
    
    /// <inheritdoc />
    public string GetPageSource()
    {
        // Build an XML representation of the automation tree
        return BuildAutomationTree(RootElement);
    }
    
    /// <inheritdoc />
    public string GetAutomationTree()
    {
        return BuildAutomationTree(RootElement);
    }
    
    private static string BuildAutomationTree(AutomationElement element, int depth = 0)
    {
        var indent = new string(' ', depth * 2);
        var sb = new System.Text.StringBuilder();
        
        // Use safe property access - some elements don't support all properties
        string automationId = "";
        string name = "";
        string className = "";
        string controlType = "Unknown";
        
        try { automationId = element.Properties.AutomationId.ValueOrDefault ?? ""; } catch { }
        try { name = element.Properties.Name.ValueOrDefault ?? ""; } catch { }
        try { className = element.Properties.ClassName.ValueOrDefault ?? ""; } catch { }
        try { controlType = element.ControlType.ToString(); } catch { }
        
        sb.AppendLine($"{indent}<{controlType} AutomationId=\"{automationId}\" Name=\"{name}\" ClassName=\"{className}\">");
        
        try
        {
            foreach (var child in element.FindAllChildren())
            {
                sb.Append(BuildAutomationTree(child, depth + 1));
            }
        }
        catch
        {
            // Ignore errors traversing children
        }
        
        sb.AppendLine($"{indent}</{controlType}>");
        return sb.ToString();
    }

    #endregion

    #region Gestures (Brinell UI Automation bridge)

    /// <inheritdoc />
    /// <remarks>
    /// Answered by asking the app under test what it declared, not by inspecting the control.
    /// False for a control that could obviously be swiped means the app has not opted that
    /// element in, which is a change to the app's markup rather than to the test.
    /// </remarks>
    public bool SupportsGesture(string automationId, MauiGesture gesture)
        => GestureRunner.Supports(RootElement, Automation, automationId, gesture);

    /// <inheritdoc />
    /// <remarks>
    /// Works on elements this driver cannot find at all. A MAUI <c>SwipeView</c> publishes no
    /// <c>AutomationId</c> on Windows, so <c>FindElement</c> will never return it - but its
    /// bridge element is addressable, and that is what carries the verb.
    /// </remarks>
    /// <exception cref="Bridge.GestureUnavailableException">
    /// The app publishes no bridge, the element was not declared, or the verb was refused.
    /// </exception>
    public void PerformGesture(string automationId, MauiGesture gesture)
        => WhileTheAppRuns(() => GestureRunner.Perform(RootElement, Automation, automationId, gesture));

    /// <inheritdoc />
    /// <exception cref="Bridge.GestureUnavailableException">
    /// The app publishes no bridge, the element was not declared, or the verb was refused.
    /// </exception>
    public void PerformGesture(string automationId, MauiGesture gesture, int arg1, int arg2)
        => WhileTheAppRuns(() => GestureRunner.Perform(RootElement, Automation, automationId, gesture, arg1, arg2));

    /// <summary>
    /// Runs a bridge gesture; a gesture that found no target because the app has exited is
    /// <see cref="AppUnavailableException"/>, not "unavailable" (see <see cref="Exchange"/>).
    /// </summary>
    private void WhileTheAppRuns(Action gesture)
    {
        try
        {
            gesture();
        }
        catch (Bridge.GestureUnavailableException error) when (AppHasExited)
        {
            throw new AppUnavailableException("the application process has exited.", error);
        }
    }

    /// <summary>Whether the bridge target with this id declares GetState.</summary>
    internal bool SupportsStateReads(string automationId)
        => BridgeVerbRunner.Supports(
            RootElement, Automation, automationId, BrinellVerb.GetState);

    /// <summary>Reads app-published state for a bridge target, which may have no node in the tree.</summary>
    internal string ReadState(string automationId, string property)
    {
        var answer = BridgeVerbRunner.Send(
            RootElement, Automation, automationId, BrinellVerb.GetState, property);

        if (!answer.Delivered && AppHasExited)
        {
            throw new AppUnavailableException("the application process has exited.");
        }

        if (!answer.Delivered)
        {
            throw new NotSupportedException(
                $"'{automationId}' does not answer GetState('{property}'). The bridge said: "
                + answer.Reason);
        }

        return answer.Value;
    }

    /// <summary>
    /// Whether the app under test publishes a Brinell bridge at all.
    /// </summary>
    /// <remarks>
    /// The one call that distinguishes "this app has no instrumentation" from "this element was
    /// not declared". Worth checking once in a fixture rather than inferring it from a failure.
    /// </remarks>
    /// <returns>Whether a bridge window is present.</returns>
    public bool HasGestureBridge()
        => Bridge.BrinellBridgeLookup.HasBridge(RootElement, Automation);

    /// <summary>
    /// Describes the raw automation tree just below the app window.
    /// </summary>
    /// <remarks>
    /// A diagnostic, for when a gesture is not found. The three causes present identically -
    /// no bridge window, a bridge window whose provider never answered, or a fragment root with
    /// nothing registered on it - and this is what tells them apart.
    /// </remarks>
    /// <param name="maxDepth">How far below the window to walk.</param>
    /// <returns>One line per element, indented by depth.</returns>
    public string DescribeGestureBridge(int maxDepth = 3)
        => Bridge.BrinellBridgeLookup.Describe(RootElement, Automation, maxDepth);

    /// <summary>
    /// Reports what each instrumented element offers somebody who is not using a pointer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>An accessibility backlog, not a diagnostic.</b> Instrumenting an element for gestures
    /// is the moment to ask whether it is gesture-<i>only</i>, and this is what asks: for every
    /// element the app published, it finds the real control in the accessibility tree and reports
    /// whether a keyboard reaches it and whether it carries <c>InvokePattern</c>. The bridge does
    /// not fix a gesture-only control; it only lets Brinell drive one. This list is what keeps
    /// that honest.
    /// </para>
    /// <para>
    /// <b>Covers what is published now</b>, which is the pages that are open - elements publish
    /// on load and withdraw on unload. Call it as the app is walked and merge the passes.
    /// </para>
    /// </remarks>
    /// <param name="page">Where the app is, so a merged report says where each element was seen.</param>
    /// <returns>One finding per published element.</returns>
    public Bridge.AccessibilityAuditReport AuditGestureAccessibility(string page = "")
        => Bridge.AccessibilityAudit.Run(RootElement, Automation, page);

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Asks whichever element the app published for the job, without naming one, because there
    /// is nothing to name: going back is about the app rather than about a control, and on
    /// Windows the affordance that would carry an <c>AutomationId</c> for it is a
    /// <c>ToolbarItem</c> - drawn into native chrome, and measured four separate ways not to be
    /// activatable through any automation pattern at all. That measurement is what makes this
    /// method necessary rather than convenient: without it, returning to a previous page is the
    /// one thing in the suite that has to be a real mouse click.
    /// </para>
    /// <para>
    /// Never falls back to real input; see the interface.
    /// </para>
    /// </remarks>
    public bool IsAtNavigationRoot() => NavigationDepth() <= 1;

    /// <inheritdoc />
    public bool IsIdle(int timeoutMs = 2000)
    {
        var answer = Exchange(BrinellVerb.IsIdle,
            timeoutMs.ToString(System.Globalization.CultureInfo.InvariantCulture));

        if (!answer.Delivered)
        {
            throw new NotSupportedException(
                "The app under test cannot say whether it is idle, so there is nothing to wait on "
                + "and every such wait has to go back to being a sleep. Declare IsIdle on its "
                + $"pages - see GestureAutomation.Verbs. The bridge said: {answer.Reason}");
        }

        return bool.TryParse(answer.Value, out var idle) && idle;
    }

    /// <inheritdoc />
    /// <remarks>
    /// <b>Retries briefly, because every way of getting no answer here is also a moment in a page
    /// transition.</b> The obvious reading - nobody declares <c>GetState</c>, so waiting cannot
    /// help - was tried and is wrong: between one page withdrawing and the next publishing, the
    /// bridge is briefly empty and there is nobody to ask at all. An app that genuinely declares
    /// none pays the budget once and then gets a message naming the fix.
    /// <para>
    /// Neither case existed until step 43 stopped a page answering for a stack it is no longer
    /// part of. Before that a page mid-teardown answered with a depth of its own, which is how a
    /// suite came to be told it was at the hub while looking at another page.
    /// </para>
    /// </remarks>
    public int NavigationDepth()
    {
        const int transitionBudgetMs = 2_000;
        const int pollMs = 25;

        var clock = System.Diagnostics.Stopwatch.StartNew();
        Bridge.BridgeVerbResult answer;
        int depth;

        while (true)
        {
            answer = Exchange(BrinellVerb.GetState, "NavigationDepth");

            if (answer.Delivered
                && int.TryParse(
                    answer.Value, System.Globalization.CultureInfo.InvariantCulture, out depth))
            {
                break;
            }

            // Any non-delivery is retried, including "nothing published answers GetState at all".
            //
            // That case looks like a configuration error and usually is one - but measured, it is
            // also what a page transition produces: the outgoing page has withdrawn and the
            // incoming one has not published yet, so for a few milliseconds the bridge is empty
            // and there is nobody to ask. Distinguishing them by the count was tried and is
            // wrong. An app that really declares no GetState pays the budget once and then gets
            // the message below, which names the fix.
            if (clock.ElapsedMilliseconds >= transitionBudgetMs)
            {
                break;
            }

            Thread.Sleep(pollMs);
        }

        if (!answer.Delivered
            || !int.TryParse(answer.Value, System.Globalization.CultureInfo.InvariantCulture, out depth))
        {
            throw new NotSupportedException(
                "The app under test cannot say how deep its navigation stack is, so whether it is "
                + "at the root is not knowable. Declare GetState on the app's pages - see "
                + "GestureAutomation.Verbs - or drive the back affordance as a control instead. "
                + $"The bridge said: {answer.Reason}" + Environment.NewLine
                + "What is below the app window, so an empty bridge can be told from no bridge:"
                + Environment.NewLine
                + DescribeBridgeSafely());
        }

        return depth;
    }

    /// <summary>The raw tree under the window, or why it could not be read.</summary>
    /// <remarks>
    /// For failure messages only. "Nothing answered" has three causes that read identically -
    /// no bridge window, a bridge window whose provider is gone, a bridge with nobody published -
    /// and the tree is what separates them. It must never throw on the way to describing a failure.
    /// </remarks>
    private string DescribeBridgeSafely()
    {
        try
        {
            return DescribeGestureBridge(maxDepth: 2);
        }
        catch (Exception describing)
        {
            return $"(the tree could not be read: {describing.Message})";
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// <b>No grace period, and that is the fix.</b> This used to poll for two seconds whenever
    /// the verb did not answer <c>S_OK</c>, guarded on "does this app have a bridge" - which is
    /// always true for the app under test. So it fired on the commonest answer of all, *we are
    /// already at the root*, and every fixture reset that started at the hub paid two seconds to
    /// be told something it had been told immediately. See
    /// <c>.my/fix/rca-navigation-tests-stall.md</c>.
    /// </para>
    /// <para>
    /// <b>The race the grace period was added for is real, and the answers now say which is
    /// which.</b> A page publishes its bridge target on <c>Loaded</c>, later than its root
    /// appearing in the automation tree, so there is a window in which no live page answers -
    /// milliseconds after any navigation. Since step 43 the app distinguishes the three:
    /// <c>UIA_E_ELEMENTNOTAVAILABLE</c> for a stale target, <c>BRINELL_E_DECLINED</c> for the
    /// root with nothing to pop, and <c>S_OK</c> for a pop. All three cross the wire, which the
    /// old <c>S_FALSE</c> did not.
    /// </para>
    /// <para>
    /// <b>So the wait is back, bounded and only for the window it was meant for.</b> Not a grace
    /// period on every call - the two answers that mean something is genuinely wrong return at
    /// once, and only "nobody is published yet" is retried. A caller that commands without
    /// asking pays it once at the root, which is what <see cref="IsAtNavigationRoot"/> is for.
    /// </para>
    /// </remarks>
    public void NavigateBack()
    {
        const int publishWindowMs = 1_000;
        const int pollMs = 25;

        var clock = System.Diagnostics.Stopwatch.StartNew();
        Bridge.BridgeVerbResult result;

        while (true)
        {
            result = Bridge.BridgeVerbRunner.InvokeAnywhere(
                RootElement, Automation, BrinellVerb.NavigateBack);

            if (result.Delivered)
            {
                return;
            }

            // Something was asked and refused: the app has answered, and the answer is no.
            // Nothing was asked at all means no page has published yet, which a page transition
            // produces for a few milliseconds and a misconfigured app produces forever - the
            // budget tells them apart without needing to know which.
            if (result.Declined > 0 || clock.ElapsedMilliseconds >= publishWindowMs)
            {
                break;
            }

            Thread.Sleep(pollMs);
        }

        throw new BrinellException(
            "The app under test did not go back. This is a navigation failure with a specific "
            + $"cause, not something to retry: {result.Reason}");
    }


    #endregion

    #region Navigation
    
    /// <inheritdoc />
    /// <remarks>
    /// A Shell route, handed to the app's own <c>GoToAsync</c>. It used to say desktop apps have
    /// no URLs, which is true of desktop apps in general and not of this one: a MAUI Shell app
    /// navigates by route on every platform it runs on, and the bridge is how that route reaches
    /// it without a pointer.
    /// </remarks>
    public void NavigateTo(string destination)
    {
        var result = Exchange(BrinellVerb.NavigateTo, destination);

        if (!result.Delivered)
        {
            throw new BrinellException(
                $"The app under test did not navigate to '{destination}'. Routes are a Shell "
                + "concept, so an app using a NavigationPage has none to go to. "
                + $"The bridge said: {result.Reason}");
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Shell answers with its route. An app built on <c>NavigationPage</c> has no such thing, so
    /// it answers with the identity of the page on top - which is what a test asserting "we are
    /// on the hub" means anyway. Inventing a route for the second case would make two different
    /// navigation models look alike.
    /// </remarks>
    public string CurrentRoute()
    {
        var result = Exchange(BrinellVerb.CurrentRoute);

        if (!result.Delivered)
        {
            throw new NotSupportedException(
                "The app under test cannot say where it is. Declare CurrentRoute on its pages - "
                + $"see GestureAutomation.Verbs. The bridge said: {result.Reason}");
        }

        return result.Value;
    }
    
    /// <inheritdoc />
    /// <remarks>
    /// <b>Re-navigates to where the app already is</b>, rather than sending F5. A MAUI app has no
    /// refresh key: F5 was desktop-wide keyboard input landing wherever the foreground happened
    /// to be, swallowing its own failure, and doing nothing at all in the common case. Asking the
    /// app to go to its current route is the nearest thing that is actually defined - and it
    /// fails loudly on an app that has no routes, rather than silently on every app.
    /// </remarks>
    public void Refresh() => NavigateTo(CurrentRoute());
    
    /// <inheritdoc />
    public byte[] TakeScreenshot() => GetScreenshot();
    
    /// <inheritdoc />
    public void ResetAppState()
    {
        // For desktop apps, close and relaunch
        if (_application != null)
        {
            // Note: This doesn't fully reset - caller may need to recreate the driver
            _application.Close();
        }
    }
    
    #endregion

    #region Dialogs

    /// <inheritdoc />
    public void InvokeMenuItem(string automationId)
    {
        var answer = Exchange(BrinellVerb.InvokeMenuItem, automationId);

        // The outcome is in the payload, not in the HRESULT, and that is not a style choice.
        // A success HRESULT does not survive UI Automation's custom-pattern marshalling: the app
        // returning S_FALSE - "I found it and did nothing" - reaches this side as S_OK, measured
        // both ways round. Anything a caller must be able to tell apart from success therefore
        // has to travel as a value or as a failure HRESULT.
        if (answer.Delivered)
        {
            if (answer.Value != MenuItemDisabled)
            {
                return;
            }

            // Nothing was picked: waited for within the call's budget, as for toolbar items.
            throw new ElementNotReadyException(
                Locator.ByAutomationId(automationId),
                NotReadyReason.Disabled,
                "the menu item is disabled, so a user could not have picked it either");
        }

        if (answer.Declined == 0)
        {
            // Nothing on the app's bridge answered, so nothing was picked: as for toolbar items.
            throw new ElementNotReadyException(
                Locator.ByAutomationId(automationId),
                NotReadyReason.Other,
                answer.Reason);
        }

        var reason = answer.HResult switch
        {
            HResults.E_INVALIDARG => "no AutomationId was given",

            HResults.UIA_E_ELEMENTNOTAVAILABLE =>
                "no menu item on any open page carries that AutomationId. Menu items are matched "
                + "by the id in the app's markup, not by their text",

            _ => answer.Reason,
        };

        throw new BrinellException($"Could not invoke menu item '{automationId}': {reason}.");
    }

    /// <inheritdoc />
    /// <remarks>
    /// Answered only by the page on screen - the app enforces that - so the walk passes over pages
    /// that have been popped and still answer, rather than raising their items against the live
    /// navigation stack.
    /// </remarks>
    internal void InvokeToolbarItem(string automationId)
    {
        var answer = Exchange(BrinellVerb.InvokeToolbarItem, automationId);

        // In the payload, as for menu items: a success HRESULT cannot say "found it, declined".
        if (answer.Delivered)
        {
            if (answer.Value != MenuItemDisabled)
            {
                return;
            }

            // Nothing was pressed: a command enabled a moment late is waited for within the call's
            // budget, and one that never re-enables fails as "disabled" when it runs out.
            throw new ElementNotReadyException(
                Locator.ByAutomationId(automationId),
                NotReadyReason.Disabled,
                "the toolbar item is disabled, so a user could not have pressed it either");
        }

        if (answer.Declined == 0)
        {
            // Nothing on the app's bridge answered, so nothing was pressed: the page is still
            // publishing its bridge (seen right after navigating back to it). Waited for like any
            // other not-ready element; a bridge that never answers fails when the budget runs out.
            throw new ElementNotReadyException(
                Locator.ByAutomationId(automationId),
                NotReadyReason.Other,
                answer.Reason);
        }

        var reason = answer.HResult switch
        {
            HResults.E_INVALIDARG => "no AutomationId was given",

            HResults.UIA_E_ELEMENTNOTAVAILABLE =>
                "no toolbar item on the page on screen carries that AutomationId. Toolbar items "
                + "are matched by the id in the app's markup, not by their text",

            _ => answer.Reason,
        };

        throw new BrinellException($"Could not invoke toolbar item '{automationId}': {reason}.");
    }

    /// <summary>
    /// What <c>InvokeMenuItem</c> and <c>InvokeToolbarItem</c> answer with when they declined to
    /// raise the item.
    /// </summary>
    /// <remarks>
    /// Duplicated from the provider rather than shared, like the date formats above and for the
    /// same reason: the app under test is not always one Brinell can add a reference to. A
    /// mismatch is caught by <c>MenuVerbTests</c>, which drives a real disabled entry.
    /// </remarks>
    private const string MenuItemDisabled = "disabled";

    /// <summary>Opens the Shell's flyout through the app's verb.</summary>
    internal void OpenFlyout() => PresentFlyout(BrinellVerb.OpenFlyout, "open");

    /// <summary>Whether the app declares the flyout verbs.</summary>
    /// <remarks>
    /// All three - open, close, and the read that says which - because a caller that can open
    /// through the app but has to check through the chrome has not escaped the chrome.
    /// </remarks>
    internal bool SupportsFlyoutVerbs
        => Bridge.BrinellBridgeLookup.Targets(RootElement, Automation).Any(target =>
        {
            var verbs = target.SupportedVerbs();
            return verbs.Contains(BrinellVerb.OpenFlyout)
                   && verbs.Contains(BrinellVerb.CloseFlyout)
                   && verbs.Contains(BrinellVerb.GetState);
        });

    /// <summary>Closes the Shell's flyout through the app's verb.</summary>
    internal void CloseFlyout() => PresentFlyout(BrinellVerb.CloseFlyout, "close");

    /// <summary>
    /// Sends one of the two flyout verbs, and reports what came back.
    /// </summary>
    /// <remarks>
    /// <c>S_FALSE</c> - the flyout was already in the state asked for - is success here rather
    /// than a failure. The caller asked for a state, not for a transition, and a test that had
    /// to know which of the two happened would be asserting the order of the tests before it.
    /// </remarks>
    private void PresentFlyout(BrinellVerb verb, string what)
    {
        var answer = Bridge.BridgeVerbRunner.InvokeAnywhere(RootElement, Automation, verb);

        // A flyout already in the state asked for is success: the caller asked for a state, not
        // for a transition. The app does answer S_FALSE for it, which shows in the bridge log,
        // but that never reaches here - UI Automation reports every success HRESULT as S_OK - so
        // this deliberately does not pretend to tell the two apart.
        if (answer.Delivered)
        {
            return;
        }

        throw new BrinellException(
            $"Could not {what} the app's flyout. Either the app has no Shell - only a Shell has a "
            + "flyout - or no element declares the verb. The bridge said: " + answer.Reason);
    }

    /// <summary>Whether the Shell's flyout is showing, as the app reports it.</summary>
    internal bool IsFlyoutOpen()
    {
        var answer = Exchange(BrinellVerb.GetState, "FlyoutIsPresented");

        if (!answer.Delivered)
        {
            throw new NotSupportedException(
                "The app under test cannot say whether its flyout is open. Declare GetState on "
                + "its Shell - see GestureAutomation.Verbs - or ask the flyout's own elements, "
                + "remembering that Windows leaves them in the tree once it has been opened. "
                + $"The bridge said: {answer.Reason}");
        }

        return bool.TryParse(answer.Value, out var presented) && presented;
    }

    /// <summary>What the alert on screen asks, or null.</summary>
    /// <remarks>
    /// <b>Null covers two different things on purpose here</b>, and that is unusual enough to
    /// say: no dialog is on screen, and no app-side declaration. Both mean "there is no question
    /// to read", both are ordinary rather than exceptional, and a test that wants to distinguish
    /// them asks <see cref="TryFindActiveDialogRoot"/> - a dialog on screen with no readable
    /// question is exactly the app that has not declared <c>CurrentAlert</c>.
    /// </remarks>
    internal AlertContents? CurrentAlert()
    {
        // The screen decides whether there is an alert; the app only says what it asks.
        //
        // Without this the two could disagree, and did: the app clears its record when
        // DisplayAlert's await resumes, which is a continuation queued on the UI thread and
        // therefore some moments after the dialog has already left the tree. A test that
        // dismissed a prompt and immediately asked was told about the prompt it had just closed.
        // Each end answering only what it can see removes the window rather than narrowing it.
        if (TryFindActiveDialogRoot() is null)
        {
            return null;
        }

        var answer = Exchange(BrinellVerb.CurrentAlert);

        if (!answer.Delivered
            || !AlertPayload.TryParse(
                answer.Value, out var title, out var message, out var accept, out var cancel))
        {
            return null;
        }

        return new AlertContents(title, message, accept, cancel);
    }

    /// <summary>The dialog on screen, or null.</summary>
    internal IMauiElement? TryFindActiveDialogRoot()
    {
        var popupCondition = _conditionFactory.ByControlType(ControlType.Window)
            .And(_conditionFactory.ByClassName("Popup"));
        var contentDialogCondition = _conditionFactory.ByClassName("ContentDialog");
        var buttonCondition = _conditionFactory.ByControlType(ControlType.Button);

        // Searched inside the app's own window: a WinUI ContentDialog renders as a Popup
        // descendant of it, not as the sibling top-level window one might expect. Enumerating
        // top-level windows instead costs about 8 s a call — it walks the desktop and filters by
        // process, so it grows with whatever else the machine has open — against about 15 ms
        // here.
        var inRootWindow = TryFindDialogRoot(
            RootElement, popupCondition, contentDialogCondition, buttonCondition);
        if (inRootWindow != null && !ReferenceEquals(inRootWindow, RootElement))
        {
            return new FlaUIMauiElement(inRootWindow, this);
        }

        return null;
    }

    private AutomationElement? TryFindDialogRoot(
        AutomationElement window,
        ConditionBase popupCondition,
        ConditionBase contentDialogCondition,
        ConditionBase buttonCondition)
    {
        try
        {
            var popup = window.FindAllDescendants(popupCondition)
                .LastOrDefault(candidate =>
                    !candidate.Properties.IsOffscreen.ValueOrDefault
                    && candidate.FindFirstDescendant(buttonCondition) != null);

            var dialog = popup
                ?? window.FindFirst(TreeScope.Element, contentDialogCondition)
                ?? window.FindFirstDescendant(contentDialogCondition);
            if (dialog != null)
                return dialog;

            return window.Properties.NativeWindowHandle.ValueOrDefault == _window.Handle
                ? null
                : window;
        }
        catch (COMException)
        {
            return null;
        }
    }

    #endregion
    
    #region IDisposable
    
    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            _quiet?.Dispose();
            _application?.Close();
            _automation.Dispose();
            _disposed = true;
        }
    }
    
    #endregion
}

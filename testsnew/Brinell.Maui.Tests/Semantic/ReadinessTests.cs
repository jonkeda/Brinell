namespace Brinell.Maui.Tests.Semantic;

/// <summary>
/// Covers the readiness ladder an action climbs before it acts: scope ready, element present,
/// visible, enabled.
/// </summary>
/// <remarks>
/// These exist because the ordering is easy to break and expensive to notice — a missing rung
/// shows up as a timeout against an element rather than a statement about what was not ready,
/// and only on the slower platform. Mocking the element makes each rung testable in
/// milliseconds, so the guarantee is pinned here rather than inferred from a device run.
/// See <c>.my/maui/plan-wait-for-readiness.md</c>.
/// </remarks>
public class ReadinessTests
{
    private readonly Mock<IMauiTestContext> _context = new();

    public ReadinessTests()
    {
        _context.Setup(c => c.Timeouts).Returns(new TimeoutSettings
        {
            DefaultWait = 1000,
            PageLoad = 1000,
            ElementFind = 1000,
            PollingInterval = 1
        });
        _context.Setup(c => c.DefaultLocatorStrategy).Returns(LocatorStrategy.AutomationId);
    }

    /// <summary>
    /// A page whose loaded state is driven by the test.
    /// </summary>
    private sealed class GatedPage : PageObjectBase<GatedPage>
    {
        private readonly Func<bool> _isLoaded;

        public GatedPage(IMauiTestContext context, Func<bool> isLoaded)
            : base(context)
        {
            _isLoaded = isLoaded;
        }

        public override string Name => "GatedPage";

        public override bool IsLoaded(int? timeoutMs = null) => _isLoaded();

        public Button<GatedPage> Target => new(this, "Target");
    }

    private Mock<IMauiElement> GivenElement()
    {
        var element = new Mock<IMauiElement>();
        element.Setup(e => e.Visible).Returns(true);
        element.Setup(e => e.Enabled).Returns(true);
        element.Setup(e => e.Rect).Returns(new System.Drawing.Rectangle(0, 0, 40, 20));

        _context.Setup(c => c.FindElement(It.IsAny<Locator>())).Returns(element.Object);
        _context.Setup(c => c.TryFindElement(It.IsAny<Locator>())).Returns(element.Object);

        return element;
    }

    #region Scope readiness

    /// <summary>
    /// A page that becomes ready late is waited for, not failed against.
    /// </summary>
    /// <remarks>
    /// This is the rung the whole plan is about. The wait is not a sleep and not a retry the
    /// caller wrote: <c>FindElement</c> throws while the page is not loaded, and the action's
    /// own <c>RunPoll</c> treats that as the transient condition it is.
    /// </remarks>
    [Fact]
    public void Click_WaitsForThePage_WhenItBecomesLoadedLate()
    {
        var checks = 0;
        var page = new GatedPage(_context.Object, () => ++checks >= 3);
        var element = GivenElement();

        page.Target.Click();

        element.Verify(e => e.Click(), Times.Once);
        Assert.True(checks >= 3, "the page should have been re-checked until it was loaded");
    }

    /// <summary>
    /// A page that never loads fails naming the page, not the element.
    /// </summary>
    /// <remarks>
    /// The message is the point. Before
    /// <c>rca-002-page-precondition-discarded-slow-failures.md</c> this surfaced as "element
    /// not found", which describes a symptom of being on the wrong page rather than the cause.
    /// </remarks>
    [Fact]
    public void Click_FailsNamingThePage_WhenItNeverLoads()
    {
        var page = new GatedPage(_context.Object, () => false);
        GivenElement();

        var ex = Assert.Throws<ElementNotFoundException>(() => page.Target.Click());

        Assert.Contains("GatedPage", ex.Message);
        Assert.Contains("not loaded", ex.Message);
    }

    #endregion

    #region Element readiness

    /// <summary>
    /// A control enabled late is waited for.
    /// </summary>
    /// <remarks>
    /// A button enabled by a binding that resolves a frame after the page appears is the
    /// ordinary case here, not an exotic one.
    /// </remarks>
    [Fact]
    public void Click_WaitsForTheControl_WhenItBecomesEnabledLate()
    {
        var page = new GatedPage(_context.Object, () => true);
        var element = GivenElement();

        var reads = 0;
        element.Setup(e => e.Enabled).Returns(() => ++reads >= 3);

        page.Target.Click();

        element.Verify(e => e.Click(), Times.Once);
    }

    /// <summary>
    /// A control that never becomes enabled fails naming the locator.
    /// </summary>
    [Fact]
    public void Click_FailsNamingTheLocator_WhenTheControlNeverEnables()
    {
        var page = new GatedPage(_context.Object, () => true);
        var element = GivenElement();
        element.Setup(e => e.Enabled).Returns(false);

        var ex = Assert.Throws<TimeoutException>(() => page.Target.Click());

        Assert.Contains("Target", ex.Message);
        element.Verify(e => e.Click(), Times.Never);
    }

    #endregion

    #region Queries stay instantaneous

    /// <summary>
    /// <c>IsExists()</c> answers immediately when the page is not loaded.
    /// </summary>
    /// <remarks>
    /// The constraint that keeps this design from becoming "wait everywhere". <c>IsExists</c>
    /// is a question, not an action: if it waited for the page, then <c>IsExists() == false</c>
    /// — and every <c>AssertExists(false)</c> built on it — would cost a full page timeout to
    /// establish.
    /// </remarks>
    [Fact]
    public void IsExists_ReturnsFalseImmediately_WhenThePageIsNotLoaded()
    {
        var page = new GatedPage(_context.Object, () => false);
        GivenElement();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var exists = page.Target.IsExists();
        stopwatch.Stop();

        Assert.False(exists);
        Assert.True(stopwatch.ElapsedMilliseconds < 500,
            $"IsExists should not wait for the page; took {stopwatch.ElapsedMilliseconds}ms");
    }

    #endregion

    #region Retry safety

    /// <summary>
    /// An action that throws *after* taking effect is performed twice.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This documents a known limitation, not desired behaviour.</b> <c>RunPoll</c> retries
    /// its whole body on any exception, and the body of <c>RunDoWithElement</c> ends with the
    /// action itself. So a driver that acts and then throws — a click that navigates away and
    /// leaves the element stale is the realistic case — gets its action replayed.
    /// </para>
    /// <para>
    /// The failure mode is a silent double action: a counter incremented twice, an item added
    /// twice, a form submitted twice. It is the same shape as the Android double-tap traced in
    /// <c>plan-fix-hub-navigation.md</c>, reached by a different route.
    /// </para>
    /// <para>
    /// Pinned here so the behaviour is visible and a fix has a test to turn green. See
    /// <c>.my/maui/plan-wait-for-readiness.md</c> §0.1.
    /// </para>
    /// </remarks>
    [Fact]
    public void Click_IsPerformedTwice_WhenTheDriverThrowsAfterActing()
    {
        var page = new GatedPage(_context.Object, () => true);
        var element = GivenElement();

        var clicks = 0;
        element.Setup(e => e.Click()).Callback(() =>
        {
            clicks++;
            if (clicks == 1) throw new InvalidOperationException("stale after navigation");
        });

        page.Target.Click();

        // Current behaviour, asserted so a fix breaks this test deliberately rather than
        // silently changing it.
        Assert.Equal(2, clicks);
    }

    #endregion
}

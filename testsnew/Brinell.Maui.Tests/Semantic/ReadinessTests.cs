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
    /// <c>IsExists()</c> answers immediately when the page is loaded and the element is not.
    /// </summary>
    /// <remarks>
    /// The constraint that keeps this design from becoming "wait everywhere". The query waits
    /// for the <em>page</em>, because being on the page is a precondition for the question
    /// meaning anything — but never for the <em>element</em>, because the element's absence is
    /// the answer. If it waited for the element, every <c>AssertExists(false)</c> would cost a
    /// full timeout.
    /// </remarks>
    [Fact]
    public void IsExists_ReturnsFalseImmediately_WhenThePageIsLoadedAndTheElementIsNot()
    {
        var page = new GatedPage(_context.Object, () => true);
        _context.Setup(c => c.TryFindElement(It.IsAny<Locator>())).Returns((IMauiElement?)null);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var exists = page.Target.IsExists();
        stopwatch.Stop();

        Assert.False(exists);
        Assert.True(stopwatch.ElapsedMilliseconds < 500,
            $"IsExists must not wait for the element; took {stopwatch.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// <c>IsExists()</c> waits for a page that arrives late, then reports the element.
    /// </summary>
    /// <remarks>
    /// A query issued straight after navigation is the ordinary case. Nothing polls above a
    /// query — <c>IsExists</c> asks once and returns — so the wait has to happen inside the
    /// scope resolution or not at all.
    /// </remarks>
    [Fact]
    public void IsExists_WaitsForThePage_WhenItArrivesLate()
    {
        var checks = 0;
        var page = new GatedPage(_context.Object, () => ++checks >= 3);
        GivenElement();

        Assert.True(page.Target.IsExists());
    }

    #endregion

    #region Retry safety

    /// <summary>
    /// An action that throws after taking effect is performed exactly once.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Poll to get ready, then act once.</b> Resolution — finding the element, checking
    /// visible and enabled — is safe to repeat, so the retry loop covers it. The action is not
    /// safe to repeat, so it runs after the loop.
    /// </para>
    /// <para>
    /// Before that split, <c>RunPoll</c> retried its whole body including the action, so a
    /// driver that acted and then threw had its action replayed. A click that navigates away
    /// leaves the element stale and some drivers raise on the response, which made this an
    /// ordinary case rather than an exotic one. The symptom was a silent double action: a
    /// counter incremented twice, an item added twice, a form submitted twice.
    /// </para>
    /// <para>
    /// The exception now propagates rather than being retried. That is the point: once an
    /// action has been attempted, repeating it can only compound the damage.
    /// </para>
    /// </remarks>
    [Fact]
    public void Click_IsPerformedOnce_WhenTheDriverThrowsAfterActing()
    {
        var page = new GatedPage(_context.Object, () => true);
        var element = GivenElement();

        var clicks = 0;
        element.Setup(e => e.Click()).Callback(() =>
        {
            clicks++;
            throw new InvalidOperationException("stale after navigation");
        });

        Assert.Throws<InvalidOperationException>(() => page.Target.Click());

        Assert.Equal(1, clicks);
    }

    #endregion
}

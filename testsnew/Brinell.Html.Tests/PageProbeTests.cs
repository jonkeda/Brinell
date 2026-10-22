using Brinell.Core.Scopes;
using Brinell.Html.Controls.Buttons;
using Brinell.Html.Scopes;

namespace Brinell.Html.Tests;

/// <summary>
/// A context that answers <see cref="IHtmlPageProbe"/> gives a page's probe in one round trip:
/// load state and busy attribute together, with no idle wait and no lookup of the body.
/// </summary>
public class PageProbeTests
{
    private readonly Mock<IHtmlTestContext> _context = new();
    private readonly Mock<IHtmlElement> _target = new();
    private readonly Queue<PageProbe> _probes = new();
    private int _probeCalls;

    public PageProbeTests()
    {
        _context.Setup(c => c.Timeouts).Returns(new TimeoutSettings
        {
            DefaultWait = 1000,
            PageLoad = 1000,
            PollingInterval = 1,
        });
        _context.Setup(c => c.Logger).Returns(new Mock<ITestLogger>().Object);
        _context.Setup(c => c.Context).Returns(() => _context.Object);
        _context.Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == "Target"))).Returns(_target.Object);
        _context.As<IHtmlPageProbe>().Setup(p => p.ProbePage("data-busy")).Returns(() =>
        {
            _probeCalls++;
            return _probes.Count > 1 ? _probes.Dequeue() : _probes.Peek();
        });
        _target.Setup(e => e.Visible).Returns(true);
        _target.Setup(e => e.Enabled).Returns(true);
    }

    private sealed class Page(IHtmlTestContext context, BusySignal busySignal) : HtmlPageObjectBase<Page>(context)
    {
        protected override BusySignal BusySignal => busySignal;

        public ButtonControl<Page> Target => new(this, Locator.ByAutomationId("Target"));
    }

    [Fact]
    public void AReadyPage_IsOneProbe_WithNoIdleWaitAndNoBodyLookup()
    {
        _probes.Enqueue(new PageProbe("complete", true, "false"));

        Assert.True(new Page(_context.Object, BusySignal.Required).ProbeReadiness().IsReady);

        Assert.Equal(1, _probeCalls);
        _context.Verify(c => c.IsIdle(), Times.Never);
        _context.Verify(c => c.TryFindElement(It.Is<Locator>(l => l.Value == "body")), Times.Never);
    }

    [Fact]
    public void ABusyPage_DefersTheCall_UntilTheProbeSaysIdle_OneProbePerAttempt()
    {
        _probes.Enqueue(new PageProbe("loading", false, null));
        _probes.Enqueue(new PageProbe("complete", true, null));
        _probes.Enqueue(new PageProbe("complete", true, "true"));
        _probes.Enqueue(new PageProbe("complete", true, "false"));

        new Page(_context.Object, BusySignal.Required).Target.Click();

        _target.Verify(e => e.Click(), Times.Once);
        Assert.Equal(4, _probeCalls);
        _context.Verify(c => c.IsIdle(), Times.Never);
    }

    [Theory]
    [InlineData("loading", true, "false", ScopeReadinessState.NotLoaded)]
    [InlineData("complete", true, null, ScopeReadinessState.NotLoaded)]
    [InlineData("complete", true, "true", ScopeReadinessState.Busy)]
    [InlineData("complete", true, "maybe", ScopeReadinessState.InvalidBusySignal)]
    public void TheProbe_ReadsAsBefore(string readyState, bool hasBody, string? busy, ScopeReadinessState expected)
    {
        _probes.Enqueue(new PageProbe(readyState, hasBody, busy));

        Assert.Equal(expected, new Page(_context.Object, BusySignal.Required).ProbeReadiness().State);
    }
}

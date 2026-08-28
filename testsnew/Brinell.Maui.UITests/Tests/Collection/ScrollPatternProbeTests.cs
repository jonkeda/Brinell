using Brinell.Maui.UITests.Containers;
using Brinell.Maui.UITests.Pages;
using Xunit.Abstractions;

namespace Brinell.Maui.UITests.Tests.Collection;

/// <summary>
/// Probe: does the UIA Scroll pattern reach a MAUI CollectionView on Windows?
/// </summary>
/// <remarks>
/// Diagnostic only — reports, never fails. Deep virtualized scrolling was blocked because
/// the only primitives available were <c>ScrollIntoView</c> (a no-op for an already-visible
/// item) and <c>Swipe</c> (policy-gated). <c>TryScrollContent</c> was added to drive the
/// scroll pattern instead; this measures whether that pattern is actually present on the
/// element, on its wrapper, or nowhere.
/// </remarks>
[Collection("Maui")]
[Trait("Category", "UITest")]
[Trait("Pattern", "ScrollProbe")]
public class ScrollPatternProbeTests
{
    private readonly MauiFixture _fixture;
    private readonly ITestOutputHelper _output;

    public ScrollPatternProbeTests(MauiFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
        _fixture.NavigateToGridCollectionDemo();
    }

    private GridCollectionDemoPage Page => _fixture.GridCollectionDemoPage;

    [Fact(Timeout = TestConstants.LongTestTimeoutMs)]
    public Task ScrollPattern_ReportsReachability()
    {
        Page.Products.BulkAddButton.Click();
        Page.Products.WaitLogicalCount(
            ProductCollection.SeedCount + 60, TestConstants.LongTestTimeoutMs);

        var report = new System.Text.StringBuilder();
        report.AppendLine();

        var container = Page.Products.TryFindElement(Locator.ByAutomationId("ProductListContainer"));
        var view = Page.Products.TryFindElement(Locator.ByAutomationId("ProductCollectionView"));

        report.AppendLine($"container element resolved: {container != null}");
        report.AppendLine($"collection view resolved:   {view != null}");
        report.AppendLine($"realized rows before:       {Page.Products.GetItemCount()}");

        foreach (var (label, element) in new[]
                 {
                     ("ProductListContainer", container),
                     ("ProductCollectionView", view),
                 })
        {
            if (element == null)
            {
                report.AppendLine($"{label}: not resolved");
                continue;
            }

            bool scrolled;
            try
            {
                scrolled = element.TryScrollContent(1);
            }
            catch (Exception ex)
            {
                report.AppendLine($"{label}: TryScrollContent threw {ex.GetType().Name}");
                continue;
            }

            report.AppendLine($"{label}: TryScrollContent(1) -> {scrolled}");

            // Distinguish "pattern absent" from "pattern present but already at the end".
            var vertical = element.GetAttribute("Scroll.VerticalScrollPercent");
            var scrollable = element.GetAttribute("Scroll.VerticallyScrollable");
            report.AppendLine($"{label}: VerticalScrollPercent={vertical ?? "(null)"} VerticallyScrollable={scrollable ?? "(null)"}");
        }

        report.AppendLine($"realized rows after 1 step: {Page.Products.GetItemCount()}");

        // How far can repeated scrolling actually get? This is the number that decides
        // whether deep virtualized scrolling is achievable at all.
        if (view != null)
        {
            var steps = 0;
            var stalled = 0;
            while (steps < 60 && stalled < 3)
            {
                var seen = Page.Products.GetItemCount();
                if (!view.TryScrollContent(1)) stalled++; else stalled = 0;
                steps++;
                if (Page.Products.GetItemCount() == seen && stalled == 0) stalled++;
            }

            report.AppendLine($"after {steps} scroll steps:       {Page.Products.GetItemCount()} realized");
            report.AppendLine($"final VerticalScrollPercent: {view.GetAttribute("Scroll.VerticalScrollPercent")}");
        }

        _output.WriteLine(report.ToString());
        return Task.CompletedTask;
    }
}

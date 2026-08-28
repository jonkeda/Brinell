using Xunit.Abstractions;

namespace Brinell.Maui.UITests.Tests.Container;

/// <summary>
/// Probe: how many Shell tabs are actually reachable on Windows?
/// </summary>
/// <remarks>
/// Diagnostic only. <c>ShellContent</c> locates a tab by control type <c>TabItem</c> plus
/// its Title, so a tab that WinUI has pushed into the overflow "More" menu cannot be
/// clicked. This reports which titles are present, which is the difference between "the
/// tab is missing" and "the tab exists but is not reachable".
/// </remarks>
[Collection("Maui")]
[Trait("Category", "UITest")]
[Trait("Pattern", "TabBarProbe")]
public class TabBarCapacityProbeTests
{
    private static readonly string[] TabTitles =
    [
        "Buttons", "DateTime", "Display", "Range", "Selection", "Text", "Toggle",
        "Containers", "Navigation", "Probe",
    ];

    private readonly MauiFixture _fixture;
    private readonly ITestOutputHelper _output;

    public TabBarCapacityProbeTests(MauiFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
        _fixture.NavigateToMain();
    }

    /// <summary>Reports whether route-based navigation to the module pages works.</summary>
    [Fact(Timeout = TestConstants.LongTestTimeoutMs)]
    public Task ModuleRoutes_ReportNavigability()
    {
        var probe = _fixture.NavigateToAutomationProbe();
        var report = new System.Text.StringBuilder();
        report.AppendLine();

        probe.GoToContainerButton.Click();

        foreach (var id in new[] { "ContainerPage", "PageTitle", "TestBoxView", "TestGrid" })
        {
            IMauiElement? el = null;
            try { el = _fixture.Context.TryFindElement(Locator.ByAutomationId(id)); }
            catch (ElementNotFoundException) { }
            report.AppendLine($"after GoToAsync(ContainerPage): {id} -> {(el != null ? "found" : "NOT found")}");
        }

        _output.WriteLine(report.ToString());
        return Task.CompletedTask;
    }

    [Fact(Timeout = TestConstants.LongTestTimeoutMs)]
    public Task TabBar_ReportsReachableTabs()
    {
        var report = new System.Text.StringBuilder();
        report.AppendLine();
        report.AppendLine("| # | Tab title | Reachable |");
        report.AppendLine("|---|---|---|");

        var reachable = 0;
        for (var i = 0; i < TabTitles.Length; i++)
        {
            var title = TabTitles[i];
            IMauiElement? element = null;
            try
            {
                element = _fixture.Context.TryFindElement(new Locator("TabItem", title));
            }
            catch (ElementNotFoundException)
            {
                // Reported as unreachable below.
            }

            if (element != null) reachable++;
            report.AppendLine($"| {i + 1} | {title} | {(element != null ? "yes" : "NO")} |");
        }

        report.AppendLine();
        report.AppendLine($"reachable: {reachable} of {TabTitles.Length}");

        _output.WriteLine(report.ToString());
        return Task.CompletedTask;
    }
}

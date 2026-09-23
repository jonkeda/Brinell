using Xunit.Abstractions;

namespace Brinell.Maui.UITests.Tests.Collection;

/// <summary>
/// Measures whether realized MAUI rows report a logical collection index.
/// This reports platform behavior; it does not assert which route the framework must use.
/// </summary>
/// <remarks>
/// <para>
/// Measured on Windows 2026-09-14 (<c>.my/bridge/fix-remaining-skips.md</c> step C0): the row
/// itself publishes no set metadata, its <c>ListViewItem</c> ancestor does, and
/// <see cref="IMauiElement.PositionInSet"/> reads the nearest ancestor that publishes it. This used
/// to walk the ancestors through FlaUI by reflection to find that out; it now reads the interface,
/// so it compiles on the mobile head too, where both answers are null.
/// </para>
/// </remarks>
[Collection("Maui")]
[Trait("Category", "UITest")]
[Trait("Pattern", "Probe")]
public class CollectionIndexProbeTests
{
    private const int BulkTotal = Containers.ProductCollection.SeedCount + 60;

    private readonly MauiFixture _fixture;
    private readonly ITestOutputHelper _output;

    public CollectionIndexProbeTests(MauiFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
        _fixture.NavigateToGridCollectionDemo();
    }

    [Fact(Timeout = TestConstants.LongTestTimeoutMs)]
    public Task RealizedRows_ReportLogicalPosition()
    {
        var products = _fixture.GridCollectionDemoPage.Products;
        products.BulkAddButton.Click();
        Assert.True(products.WaitLogicalCount(BulkTotal, TestConstants.LongTestTimeoutMs));

        var collection = _fixture.Context.TryFindElement(
                             Locator.ByAutomationId("ProductCollectionView"))
                         ?? throw new InvalidOperationException("ProductCollectionView was not found.");

        Report("top", collection.FindElements(Locator.ByAutomationId("ProductRow")));

        collection.ScrollToIndex(60);
        Report("index 60", collection.FindElements(Locator.ByAutomationId("ProductRow")));

        return Task.CompletedTask;
    }

    private void Report(string position, IReadOnlyList<IMauiElement> rows)
    {
        _output.WriteLine($"{position}: realized={rows.Count}");
        if (rows.Count == 0)
        {
            return;
        }

        ReportRow("first", rows[0]);
        ReportRow("last", rows[^1]);
        _output.WriteLine(
            $"ordered names: {string.Join(" | ", rows.Select(ReadName))}");
    }

    private void ReportRow(string label, IMauiElement row)
        => _output.WriteLine(
            $"{label}: name='{ReadName(row)}', position={row.PositionInSet?.ToString() ?? "(none)"}, "
            + $"size={row.SizeOfSet?.ToString() ?? "(none)"}");

    private static string ReadName(IMauiElement row)
        => row.FindElement(Locator.ByAutomationId("ProductNameLabel")).Text ?? "(no text)";
}

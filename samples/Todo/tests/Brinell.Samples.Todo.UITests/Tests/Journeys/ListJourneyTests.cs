using Brinell.Samples.Todo.UITests.Containers;
using Brinell.Samples.Todo.UITests.Controls;

namespace Brinell.Samples.Todo.UITests.Tests.Journeys;

/// <summary>TOD.01 and TOD.03, hermetic: what the list shows, and moving between list and detail.</summary>
/// <remarks>
/// One example per behaviour. The sort order, the filter logic and the state table are unit tests;
/// here the question is only whether the screen shows what they decide.
/// </remarks>
[Collection(ThreeTodosCollection.Name)]
[Trait("Category", "UITest")]
[Trait("Module", "Todo")]
[Trait("Pyramid", "UiHermetic")]
public sealed class ListJourneyTests
{
    private readonly ThreeTodosFixture _fixture;

    public ListJourneyTests(ThreeTodosFixture fixture)
    {
        _fixture = fixture;
        fixture.StartTest();
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Journey", "TOD.01.2")]
    public Task Row_ShowsTitleDueDateAndStatus()
    {
        var milk = _fixture.List.Todos.Row("Buy milk");

        milk.Title.AssertText("Buy milk");
        milk.Due.AssertTextStartsWith("Due ");
        milk.Status.AssertStatus(StatusText.Open).AssertGlyph("○");

        _fixture.List.Todos.Row("Book dentist").Due.AssertShown(false);
        return Task.CompletedTask;
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Journey", "TOD.05.4")]
    public Task RowStatus_IsReadOnly()
    {
        _fixture.List.Todos.Row("Buy milk").Status.AssertChangeable(false);
        return Task.CompletedTask;
    }

    [WindowsOnlyFact(WindowsOnlyFactAttribute.PickerGap)]
    [Trait("Journey", "TOD.01.4")]
    public Task Filter_ShowsOnlyMatchingRows()
    {
        var list = _fixture.List;

        list.Filter.SelectByText("Done");

        list.Todos
            .AssertRow("File taxes")
            .AssertRow("Buy milk", present: false)
            .AssertRow("Book dentist", present: false);
        return Task.CompletedTask;
    }

    [WindowsOnlyFact(WindowsOnlyFactAttribute.PickerGap)]
    [Trait("Journey", "TOD.03.4")]
    public Task Back_ReturnsToTheListAtTheSameFilter()
    {
        _fixture.List.Filter.SelectByText("In progress");

        var list = _fixture.List.Open("Book dentist").Back();

        list.Filter.AssertSelectedText("In progress");
        list.Todos.AssertRow("Book dentist").AssertRow("Buy milk", present: false);
        return Task.CompletedTask;
    }
}

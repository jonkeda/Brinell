using Xunit;

namespace Bruinell.Bdd.Tests;

public sealed class BddSetsTests
{
    [Fact]
    public void AssertSet_SameItemsInAnotherOrder_Passes()
        => BddSets.AssertSet([new Request("GET", "users"), new Request("PUT", "todos")], [new Request("PUT", "todos"), new Request("GET", "users")]);

    [Fact]
    public void AssertSet_ShowsTheMissingAndTheExtra_WithTheClosestDifference()
    {
        var error = Assert.Throws<InvalidOperationException>(() => BddSets.AssertSet(
            [new Request("GET", "users"), new Request("PUT", "todos")],
            [new Request("GET", "users"), new Request("POST", "todos")],
            what: "requests"));

        Assert.StartsWith("The requests differ: 1 missing, 1 extra.", error.Message, StringComparison.Ordinal);
        Assert.Contains("<- missing (closest differs in Method: POST)", error.Message, StringComparison.Ordinal);
        Assert.Contains("<- extra", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AssertSet_Ordered_FailsOnTheSameItemsInAnotherOrder()
    {
        var error = Assert.Throws<InvalidOperationException>(() => BddSets.AssertSet(
            [new Request("GET", "users"), new Request("PUT", "todos")],
            [new Request("PUT", "todos"), new Request("GET", "users")],
            ordered: true));

        Assert.StartsWith("The Request differ (in order): 2 missing, 2 extra.", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AssertSet_OfStrings_ComparesTheValues()
    {
        var error = Assert.Throws<InvalidOperationException>(() => BddSets.AssertSet(["a", "b"], ["a"]));

        Assert.Contains("1 missing, 0 extra", error.Message, StringComparison.Ordinal);
    }

    private sealed record Request(string Method, string Resource);
}

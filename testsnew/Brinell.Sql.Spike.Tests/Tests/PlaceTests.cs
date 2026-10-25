using Brinell.Sql.Spike.Tests.TestBase;
using Xunit.Abstractions;

namespace Brinell.Sql.Spike.Tests.Tests;

// The spike's five scenarios (.my/sql-replay/plan.md, 0.2), on either head; a class each, so xUnit runs them in parallel
// (each class is its own collection), as P5 needs.

[Trait("Category", "UITest")]
public sealed class PlaceListTests(ITestOutputHelper output) : SpikeTestBase(output)
{
    [Fact]
    public void ListShowsSeededPlaces()
    {
        var places = OpenPlaces();

        places.Count.AssertText("5");
        places.Names.AssertText("Amsterdam, Enkhuizen, Hoorn, Medemblik, Purmerend");
        places.KindCounts.AssertText("city 2, town 3");
    }
}

[Trait("Category", "UITest")]
public sealed class PlaceFilterTests(ITestOutputHelper output) : SpikeTestBase(output)
{
    [Fact]
    public void FilterByKind()
    {
        var places = OpenPlaces();

        places.Cities.SetChecked(false);

        places.Count.AssertText("3");
        places.Names.AssertText("Enkhuizen, Hoorn, Medemblik");
    }
}

[Trait("Category", "UITest")]
public sealed class AddPlaceTests(ITestOutputHelper output) : SpikeTestBase(output)
{
    [Fact]
    public void AddPlaceShowsIt()
    {
        var place = OpenPlaces().Add()
            .Enter("Schagen", "town", "52.787", "4.799", "28000")
            .Save();

        place.PlaceName.AssertText("Schagen");
        place.Kind.AssertText("town");
        place.Population.AssertText("28,000");
    }
}

[Trait("Category", "UITest")]
public sealed class DuplicatePlaceTests(ITestOutputHelper output) : SpikeTestBase(output)
{
    [Fact]
    public void AddDuplicateNameIsRefused()
    {
        var form = OpenPlaces().Add()
            .Enter("Hoorn", "town", "52.64", "5.06", "1")
            .SaveExpectingError();

        form.Error.AssertText("That name is taken");
    }
}

[Trait("Category", "UITest")]
public sealed class PlaceDistanceTests(ITestOutputHelper output) : SpikeTestBase(output)
{
    [Fact]
    public void DistanceIsShown()
    {
        var place = OpenPlace("Enkhuizen");

        place.Distance.AssertTextStartsWith("17.");
        place.Visits.AssertText("0");
    }
}

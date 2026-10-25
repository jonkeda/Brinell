using Brinell.Sql.Spike.Tests.Spike;
using Brinell.Sql.Spike.Tests.TestBase;

namespace Brinell.Sql.Spike.Tests.Tests;

/// <summary>The spike's five scenarios (.my/sql-replay/plan.md, 0.2), on either head.</summary>
[Trait("Category", "UITest")]
[MeasureDatabaseShare]
public sealed class PlaceTests : SpikeTestBase
{
    [Fact]
    public void ListShowsSeededPlaces()
    {
        var places = OpenPlaces();

        places.Count.AssertText("5");
        places.Names.AssertText("Amsterdam, Enkhuizen, Hoorn, Medemblik, Purmerend");
    }

    [Fact]
    public void FilterByKind()
    {
        var places = OpenPlaces();

        places.Cities.SetChecked(false);

        places.Count.AssertText("3");
        places.Names.AssertText("Enkhuizen, Hoorn, Medemblik");
    }

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

    [Fact]
    public void AddDuplicateNameIsRefused()
    {
        var form = OpenPlaces().Add()
            .Enter("Hoorn", "town", "52.64", "5.06", "1")
            .SaveExpectingError();

        form.Error.AssertText("That name is taken");
    }

    [Fact]
    public void DistanceIsShown()
    {
        var place = OpenPlace("Enkhuizen");

        place.Distance.AssertTextStartsWith("17.");
    }
}

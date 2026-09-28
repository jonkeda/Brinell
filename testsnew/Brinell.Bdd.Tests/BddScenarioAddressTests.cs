using Xunit;

namespace Brinell.Bdd.Tests;

/// <summary>A scenario's address: its file and a slug a <c>dotnet test</c> filter takes as it is.</summary>
public sealed class BddScenarioAddressTests
{
    [Theory]
    [InlineData("TOD.11.1 Users lists available users first, each with its role and state", "TOD.11.1-Users-lists-available-users-first-each-with-its-role-and-state")]
    [InlineData("Shelve a book [2]", "Shelve-a-book-2")]
    [InlineData("Finn's browser, offline — gets it", "Finn-s-browser-offline-gets-it")]
    [InlineData("  spaced  ", "spaced")]
    public void Slug_KeepsOnlyWhatAFilterTakes(string name, string slug)
        => Assert.Equal(slug, BddScenarioAddress.Slug(name));

    [Fact]
    public void Of_IsTheFileAndTheSlug_WithForwardSlashes()
        => Assert.Equal(
            "Features/Users/TOD.11-manage-users.feature.md#TOD.11.1-Users-list",
            BddScenarioAddress.Of(@"Features\Users\TOD.11-manage-users.feature.md", "TOD.11.1 Users list"));
}

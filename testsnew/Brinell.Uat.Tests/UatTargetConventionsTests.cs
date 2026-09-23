using Xunit;

namespace Brinell.Uat.Tests;

/// <summary>
/// The target a fixture is, derived from its base rather than declared in a config.
/// </summary>
public sealed class UatTargetConventionsTests
{
    [Fact]
    public void FromFixture_WalksTheBaseChain()
    {
        Assert.Equal("MAUI", UatTargetConventions.FromFixture(new DerivedFixture()));
    }

    [Fact]
    public void FromFixture_UnrelatedType_IsNull()
    {
        Assert.Null(UatTargetConventions.FromFixture(new object()));
    }

    [Theory]
    [InlineData("MauiTestFixtureBase", "MAUI")]
    [InlineData("WpfTestFixtureBase", "WPF")]
    [InlineData("WinFormsTestFixtureBase", "WINFORMS")]
    [InlineData("StrideTestFixtureBase", "STRIDE")]
    public void FromBaseTypeNames_MapsEachFixtureBase(string baseName, string expected)
    {
        Assert.Equal(expected, UatTargetConventions.FromBaseTypeNames(["Intermediate", baseName]));
    }

    [Fact]
    public void FromBaseTypeNames_NearestFixtureBaseWins()
    {
        Assert.Equal("WPF", UatTargetConventions.FromBaseTypeNames(["WpfTestFixtureBase", "MauiTestFixtureBase"]));
    }

    // Named to match the real base, since the mapping is by type name.
    private abstract class MauiTestFixtureBase;

    private sealed class DerivedFixture : MauiTestFixtureBase;
}

using System.Reflection;
using Xunit;

namespace Bruinell.Bdd.Workspace.Tests;

public sealed class BddVersionCheckTests
{
    private static readonly Dictionary<string, Version> Own = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Bruinell.Uat"] = new(1, 4, 2),
        ["Bruinell.Bdd"] = new(1, 4, 2),
    };

    [Fact]
    public void APatchApart_IsTheSame()
    {
        Assert.Empty(BddVersionCheck.Mismatches([Named("Bruinell.Uat", "1.4.0")], Own));
    }

    [Fact]
    public void AMinorOrMajorApart_IsSaid_PerAssembly()
    {
        var mismatches = BddVersionCheck.Mismatches([Named("Bruinell.Uat", "1.5.0"), Named("Bruinell.Bdd", "2.0.1"), Named("Newtonsoft.Json", "9.0.0")], Own);

        Assert.Equal(
            ["the suite is built against Bruinell.Bdd 2.0.1, and the extension runs 1.4.2", "the suite is built against Bruinell.Uat 1.5.0, and the extension runs 1.4.2"],
            mismatches);
    }

    [Fact]
    public void AnAssemblyTheToolDoesNotRun_IsNotCompared()
    {
        Assert.Empty(BddVersionCheck.Mismatches([Named("Bruinell.Maui", "9.9.9")], Own));
    }

    private static AssemblyName Named(string name, string version) => new(name) { Version = Version.Parse(version) };
}
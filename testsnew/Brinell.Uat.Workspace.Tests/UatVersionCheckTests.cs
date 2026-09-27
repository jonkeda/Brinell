using System.Reflection;
using Xunit;

namespace Brinell.Uat.Workspace.Tests;

public sealed class UatVersionCheckTests
{
    private static readonly Dictionary<string, Version> Own = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Brinell.Uat"] = new(1, 4, 2),
        ["Brinell.Bdd"] = new(1, 4, 2),
    };

    [Fact]
    public void APatchApart_IsTheSame()
    {
        Assert.Empty(UatVersionCheck.Mismatches([Named("Brinell.Uat", "1.4.0")], Own));
    }

    [Fact]
    public void AMinorOrMajorApart_IsSaid_PerAssembly()
    {
        var mismatches = UatVersionCheck.Mismatches([Named("Brinell.Uat", "1.5.0"), Named("Brinell.Bdd", "2.0.1"), Named("Newtonsoft.Json", "9.0.0")], Own);

        Assert.Equal(
            ["the suite is built against Brinell.Bdd 2.0.1, and the extension runs 1.4.2", "the suite is built against Brinell.Uat 1.5.0, and the extension runs 1.4.2"],
            mismatches);
    }

    [Fact]
    public void AnAssemblyTheToolDoesNotRun_IsNotCompared()
    {
        Assert.Empty(UatVersionCheck.Mismatches([Named("Brinell.Maui", "9.9.9")], Own));
    }

    private static AssemblyName Named(string name, string version) => new(name) { Version = Version.Parse(version) };
}
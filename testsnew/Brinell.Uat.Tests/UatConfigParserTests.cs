using Xunit;

namespace Brinell.Uat.Tests;

public sealed class UatConfigParserTests
{
    [Fact]
    public void Parse_ConfigMarkdown_ReturnsRuntimeAssembliesAndDiscoverySettings()
    {
        var markdown = """
            # UAT Config

            ## Runtime

            | Field | Value |
            | --- | --- |
            | Target | MAUI |
            | Profile | Local |

            ## Assemblies

            | Kind | Assembly |
            | --- | --- |
            | Pages | Example.App.Pages.dll |
            | Controls | Example.App.Controls.dll |
            | Commands | Example.App.UatCommands.dll |

            ## Discovery

            | Field | Value |
            | --- | --- |
            | RequireExplicitUatAttributes | true |
            | AllowNameInference | false |
            """;

        var config = UatConfigParser.Parse(markdown);

        Assert.Equal("MAUI", config.Runtime["Target"]);
        Assert.Equal("Local", config.Runtime["Profile"]);
        Assert.Equal(3, config.Assemblies.Count);
        Assert.Contains(config.Assemblies, x => x.Kind == "Pages" && x.Assembly == "Example.App.Pages.dll");
        Assert.True(config.Discovery.RequireExplicitUatAttributes);
        Assert.False(config.Discovery.AllowNameInference);
    }

    [Fact]
    public void Parse_ConfigWithoutDiscoverySection_UsesDefaultDiscoverySettings()
    {
        var markdown = """
            # UAT Config

            ## Runtime

            | Field | Value |
            | --- | --- |
            | Target | MAUI |
            """;

        var config = UatConfigParser.Parse(markdown);

        Assert.False(config.Discovery.RequireExplicitUatAttributes);
        Assert.True(config.Discovery.AllowNameInference);
    }
}

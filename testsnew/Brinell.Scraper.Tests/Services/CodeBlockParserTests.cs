using Xunit;

namespace Brinell.Scraper.Tests.Services;

using Brinell.Scraper.Services;

public sealed class CodeBlockParserTests
{
    [Fact]
    public void ExtractCSharpBlocks_SingleFencedBlock()
    {
        var input = """
            Here is the generated page object:

            ```csharp
            using Brinell.Core.Locators;

            namespace ExactOnline.Pages;

            public sealed class LoginPage : HtmlPageObjectBase<LoginPage>
            {
                public TextInputControl<LoginPage> UserName =>
                    Control<TextInputControl<LoginPage>>(Locator.ByText("User name"));
            }
            ```

            This class maps the login page elements.
            """;

        var result = CodeBlockParser.ExtractCSharpBlocks(input);

        Assert.Single(result);
        Assert.Contains("public sealed class LoginPage", result[0]);
        Assert.DoesNotContain("```", result[0]);
    }

    [Fact]
    public void ExtractCSharpBlocks_MultipleFencedBlocks()
    {
        var input = """
            Here is the page object:

            ```csharp
            public sealed class LoginPage { }
            ```

            And the first container:

            ```csharp
            public sealed class HeaderContainer { }
            ```

            And a second container:

            ```csharp
            public sealed class FooterContainer { }
            ```
            """;

        var result = CodeBlockParser.ExtractCSharpBlocks(input);

        Assert.Equal(3, result.Count);
        Assert.Contains("LoginPage", result[0]);
        Assert.Contains("HeaderContainer", result[1]);
        Assert.Contains("FooterContainer", result[2]);
    }

    [Fact]
    public void ExtractCSharpBlocks_CsFenceMarker()
    {
        var input = """
            ```cs
            public sealed class TestPage { }
            ```
            """;

        var result = CodeBlockParser.ExtractCSharpBlocks(input);

        Assert.Single(result);
        Assert.Contains("TestPage", result[0]);
    }

    [Fact]
    public void ExtractCSharpBlocks_NoFences_FallbackExtraction()
    {
        var input = """
            Here is the generated code:

            using Brinell.Core.Locators;

            namespace ExactOnline.Pages;

            public sealed class LoginPage : HtmlPageObjectBase<LoginPage>
            {
                public TextInputControl<LoginPage> UserName =>
                    Control<TextInputControl<LoginPage>>(Locator.ByText("User name"));
            }
            """;

        var result = CodeBlockParser.ExtractCSharpBlocks(input);

        Assert.Single(result);
        Assert.Contains("using Brinell.Core.Locators;", result[0]);
        Assert.Contains("public sealed class LoginPage", result[0]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ExtractCSharpBlocks_EmptyInput_ReturnsEmptyList(string? input)
    {
        var result = CodeBlockParser.ExtractCSharpBlocks(input!);

        Assert.Empty(result);
    }

    [Fact]
    public void ExtractCSharpBlocks_ProseOnly_ReturnsEmptyList()
    {
        var input = """
            I think the best approach would be to create a page object
            that maps each form field to a strongly-typed control property.
            The locator strategy should prefer data-testid attributes
            over CSS selectors for stability.
            """;

        var result = CodeBlockParser.ExtractCSharpBlocks(input);

        Assert.Empty(result);
    }
}

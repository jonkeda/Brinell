using Xunit;

namespace Brinell.Scraper.Tests.Services;

using Brinell.Scraper.Services;

public sealed class CodeValidatorTests
{
    private const string ValidCode = """
        using Brinell.Core.Locators;
        using Brinell.Html.Controls;

        namespace ExactOnline.Pages;

        public sealed class LoginPage : HtmlPageObjectBase<LoginPage>
        {
            public LoginPage(IHtmlTestContext context) : base(context) { }

            public TextInputControl<LoginPage> UserName =>
                Control<TextInputControl<LoginPage>>(Locator.ByText("User name"));
        }
        """;

    [Fact]
    public void Validate_ValidCode_ReturnsNoErrors()
    {
        var result = CodeValidator.Validate(ValidCode);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_SyntaxError_MissingBrace()
    {
        var code = """
            namespace Test;

            public sealed class BrokenPage
            {
                public string Name { get; }
            """;

        var result = CodeValidator.Validate(code);

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Validate_SyntaxError_InvalidExpression()
    {
        var code = """
            namespace Test;

            public sealed class BrokenPage
            {
                public string UserName => ;
            }
            """;

        var result = CodeValidator.Validate(code);

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
        Assert.True(result.Errors[0].Line > 0);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_EmptyInput_ReturnsError(string? code)
    {
        var result = CodeValidator.Validate(code!);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Message.Contains("Empty"));
    }

    [Fact]
    public void Validate_MultipleErrors_ReportsAll()
    {
        var code = """
            namespace Test;

            public sealed class BrokenPage
            {
                public string A => ;
                public string B => ;
                public string C => ;
            }
            """;

        var result = CodeValidator.Validate(code);

        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count >= 3);

        var lines = result.Errors.Select(e => e.Line).Distinct().ToList();
        Assert.True(lines.Count >= 3);
    }
}

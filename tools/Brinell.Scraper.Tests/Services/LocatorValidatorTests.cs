using Xunit;

namespace Brinell.Scraper.Tests.Services;

using Brinell.Scraper.Services;

public sealed class LocatorValidatorTests
{
    [Fact]
    public void ValidateLocators_AllValidMethods_NoWarnings()
    {
        var code = """
            namespace Test;

            public sealed class TestPage
            {
                public TextInputControl<TestPage> Email =>
                    Control<TextInputControl<TestPage>>(Locator.ByText("Email"));

                public ButtonControl<TestPage> Submit =>
                    Control<ButtonControl<TestPage>>(Locator.ByDataTestId("submit-btn"));

                public LabelControl<TestPage> Title =>
                    Control<LabelControl<TestPage>>(Locator.ByAriaLabel("Page title"));

                public ElementControl<TestPage> Logo =>
                    Control<ElementControl<TestPage>>(Locator.ById("logo"));
            }
            """;

        var result = CodeValidator.Validate(code);

        Assert.True(result.IsValid);
        Assert.DoesNotContain(result.Warnings, w => w.Message.Contains("last-resort"));
        Assert.DoesNotContain(result.Warnings, w => w.Message.Contains("Unknown locator"));
    }

    [Fact]
    public void ValidateLocators_ByCss_WarnsUser()
    {
        var code = """
            namespace Test;

            public sealed class TestPage
            {
                public ButtonControl<TestPage> Submit =>
                    Control<ButtonControl<TestPage>>(Locator.ByCss(".submit-btn"));
            }
            """;

        var result = CodeValidator.Validate(code);

        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, w => w.Message.Contains("ByCss is a last-resort locator"));
    }

    [Fact]
    public void ValidateLocators_UnknownMethod_WarnsUser()
    {
        var code = """
            namespace Test;

            public sealed class TestPage
            {
                public ElementControl<TestPage> Div =>
                    Control<ElementControl<TestPage>>(Locator.ByXPath("//div"));
            }
            """;

        var result = CodeValidator.Validate(code);

        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, w => w.Message.Contains("Unknown locator method: Locator.ByXPath()"));
    }

    [Fact]
    public void ValidateLocators_EmptyArgument_WarnsUser()
    {
        var code = """
            namespace Test;

            public sealed class TestPage
            {
                public TextInputControl<TestPage> Email =>
                    Control<TextInputControl<TestPage>>(Locator.ByText(""));
            }
            """;

        var result = CodeValidator.Validate(code);

        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, w => w.Message.Contains("non-empty string literal"));
    }

    [Fact]
    public void ValidateLocators_MultipleByCss_WarnsAll()
    {
        var code = """
            namespace Test;

            public sealed class TestPage
            {
                public ButtonControl<TestPage> Btn1 =>
                    Control<ButtonControl<TestPage>>(Locator.ByCss(".btn-1"));

                public ButtonControl<TestPage> Btn2 =>
                    Control<ButtonControl<TestPage>>(Locator.ByCss(".btn-2"));

                public ButtonControl<TestPage> Btn3 =>
                    Control<ButtonControl<TestPage>>(Locator.ByCss(".btn-3"));
            }
            """;

        var result = CodeValidator.Validate(code);

        Assert.True(result.IsValid);
        var byCssWarnings = result.Warnings
            .Where(w => w.Message.Contains("ByCss is a last-resort"))
            .ToList();
        Assert.Equal(3, byCssWarnings.Count);
    }
}

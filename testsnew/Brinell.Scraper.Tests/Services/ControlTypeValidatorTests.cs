using Xunit;

namespace Brinell.Scraper.Tests.Services;

using Brinell.Scraper.Models;
using Brinell.Scraper.Services;
using NSubstitute;

public sealed class ControlTypeValidatorTests
{
    private const string ValidCodeTemplate = """
        namespace Test;

        public sealed class TestPage
        {{
            {0}
        }}
        """;

    [Fact]
    public void ValidateWithRegistry_BuiltInTypes_NoWarnings()
    {
        var code = string.Format(ValidCodeTemplate,
            """
            public TextInputControl<TestPage> Email =>
                Control<TextInputControl<TestPage>>(Locator.ByText("Email"));

            public ButtonControl<TestPage> Submit =>
                Control<ButtonControl<TestPage>>(Locator.ByText("Submit"));
            """);

        var registry = Substitute.For<IControlRegistry>();
        registry.GetAllControls().Returns([]);

        var result = CodeValidator.ValidateWithRegistry(code, registry);

        Assert.True(result.IsValid);
        Assert.DoesNotContain(result.Warnings, w => w.Message.Contains("Unknown control type"));
    }

    [Fact]
    public void ValidateWithRegistry_CustomTypeFromRegistry_NoWarning()
    {
        var code = string.Format(ValidCodeTemplate,
            """
            public DatePickerControl<TestPage> StartDate =>
                Control<DatePickerControl<TestPage>>(Locator.ByText("Start date"));
            """);

        var registry = Substitute.For<IControlRegistry>();
        registry.GetAllControls().Returns(
        [
            new GeneratedControl { Name = "DatePickerControl", DomSignature = "div.date-picker" }
        ]);

        var result = CodeValidator.ValidateWithRegistry(code, registry);

        Assert.True(result.IsValid);
        Assert.DoesNotContain(result.Warnings, w => w.Message.Contains("Unknown control type"));
    }

    [Fact]
    public void ValidateWithRegistry_UnknownType_WarnsUser()
    {
        var code = string.Format(ValidCodeTemplate,
            """
            public FancyWidgetControl<TestPage> Widget =>
                Control<FancyWidgetControl<TestPage>>(Locator.ByText("widget"));
            """);

        var registry = Substitute.For<IControlRegistry>();
        registry.GetAllControls().Returns([]);

        var result = CodeValidator.ValidateWithRegistry(code, registry);

        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, w => w.Message.Contains("FancyWidgetControl"));
    }

    [Fact]
    public void ValidateWithRegistry_MixedKnownAndUnknown_WarnsOnlyUnknown()
    {
        var code = string.Format(ValidCodeTemplate,
            """
            public TextInputControl<TestPage> Email =>
                Control<TextInputControl<TestPage>>(Locator.ByText("Email"));

            public DatePickerControl<TestPage> StartDate =>
                Control<DatePickerControl<TestPage>>(Locator.ByText("Start date"));

            public MysteryControl<TestPage> Unknown =>
                Control<MysteryControl<TestPage>>(Locator.ByText("mystery"));
            """);

        var registry = Substitute.For<IControlRegistry>();
        registry.GetAllControls().Returns(
        [
            new GeneratedControl { Name = "DatePickerControl", DomSignature = "div.date-picker" }
        ]);

        var result = CodeValidator.ValidateWithRegistry(code, registry);

        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, w => w.Message.Contains("MysteryControl"));
        Assert.DoesNotContain(result.Warnings, w => w.Message.Contains("TextInputControl"));
        Assert.DoesNotContain(result.Warnings, w => w.Message.Contains("DatePickerControl"));
        registry.Received().GetAllControls();
    }
}

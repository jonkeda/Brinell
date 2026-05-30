using Xunit;

namespace Brinell.Uat.Tests;

public sealed class UatReflectionRuntimeTests
{
    [Fact]
    public void FromRoot_DiscoversPageAndControlNames()
    {
        var runtime = UatReflectionRuntime.FromRoot(new Fixture());

        var main = Assert.Single(runtime.Pages, page => page.Name == "Main");
        Assert.Contains(main.Controls, control => control.Name == "Name");
        Assert.Contains(main.Controls, control => control.Name == "Greet");
        Assert.Contains(main.Controls, control => control.Name == "Greeting");

        var report = string.Join(Environment.NewLine, runtime.DescribeDiscovery());
        Assert.Contains("Main", report);
        Assert.Contains("Greeting", report);
    }

    [Fact]
    public async Task CreatedCatalog_RunsScenarioThroughDiscoveredObjects()
    {
        var fixture = new Fixture();
        var runtime = UatReflectionRuntime.FromRoot(fixture);
        var scenario = BindScenario(runtime, """
            # UAT: Greeting

            ## Scenario: User receives greeting

            Given I am on the Main page
            When I clear Name
            And I enter "Alice" into Name
            And I tap Greet
            Then Greeting should contain "Hello, Alice!"
            """);

        var result = await new UatScenarioRunner().RunAsync(scenario);

        Assert.True(result.Passed, FormatResults(result));
        Assert.True(fixture.MainPage.Navigated);
        Assert.Equal("Alice", fixture.MainPage.NameEntry.Text);
        Assert.Equal("Hello, Alice!", fixture.MainPage.GreetingLabel.Text);
    }

    [Fact]
    public async Task CreatedCatalog_SelectsAndChecksDiscoveredControls()
    {
        var fixture = new Fixture();
        var runtime = UatReflectionRuntime.FromRoot(fixture);
        var scenario = BindScenario(runtime, """
            # UAT: User Form

            ## Scenario: User profile values are entered

            Given I am on the User Form page
            When I enter "Ada" into First Name
            And I check Terms
            And I select "United States" from Country
            Then First Name should contain "Ada"
            And Terms should be checked
            And Country should have selected "United States"
            """);

        var result = await new UatScenarioRunner().RunAsync(scenario);

        Assert.True(result.Passed, FormatResults(result));
        Assert.True(fixture.UserFormPage.Navigated);
        Assert.Equal("Ada", fixture.UserFormPage.FirstNameEntry.Text);
        Assert.True(fixture.UserFormPage.TermsCheckBox.Checked);
        Assert.Equal("United States", fixture.UserFormPage.CountryPicker.SelectedText);
    }

    [Fact]
    public async Task CreatedCatalog_WithoutCurrentPage_ReturnsDiagnosticFailure()
    {
        var runtime = UatReflectionRuntime.FromRoot(new Fixture());
        var scenario = BindScenario(runtime, """
            # UAT: Missing Page

            ## Scenario: Missing current page

            When I tap Greet
            """);

        var result = await new UatScenarioRunner().RunAsync(scenario);

        var step = Assert.Single(result.Steps);
        Assert.Equal(UatStepResultStatus.Failed, step.Status);
        Assert.Contains("No current UAT page", step.Message);
        Assert.Contains("Available pages", step.Message);
    }

    [Fact]
    public async Task CreatedCatalog_MissingControl_ReturnsAvailableControlNames()
    {
        var runtime = UatReflectionRuntime.FromRoot(new Fixture());
        var scenario = BindScenario(runtime, """
            # UAT: Missing Control

            ## Scenario: Missing control

            Given I am on the Main page
            When I tap Imaginary Button
            """);

        var result = await new UatScenarioRunner().RunAsync(scenario);

        var step = Assert.Single(result.Steps, x => x.Status == UatStepResultStatus.Failed);
        Assert.Contains("Imaginary Button", step.Message);
        Assert.Contains("Available controls", step.Message);
        Assert.Contains("Greet", step.Message);
    }

    private static UatBoundScenario BindScenario(UatReflectionRuntime runtime, string markdown)
    {
        var parse = UatMarkdownParser.Parse(markdown);
        Assert.True(parse.Success, FormatDiagnostics(parse));
        Assert.NotNull(parse.Document);

        var bind = UatBinder.Bind(parse.Document, runtime.CreateCommandCatalog());
        Assert.True(bind.Success, FormatDiagnostics(bind));
        Assert.NotNull(bind.Document);
        return Assert.Single(bind.Document.Scenarios);
    }

    private static string FormatDiagnostics(UatParseResult result)
    {
        return string.Join(Environment.NewLine, result.Diagnostics.Select(x => $"{x.Code} {x.Message}"));
    }

    private static string FormatDiagnostics(UatBindResult result)
    {
        return string.Join(Environment.NewLine, result.Diagnostics.Select(x => $"{x.Code} {x.Message}"));
    }

    private static string FormatResults(UatScenarioRunResult result)
    {
        return string.Join(
            Environment.NewLine,
            result.Steps.Select(x => $"{x.Status}: {x.Invocation.Step.Text} {x.Message}"));
    }

    private sealed class Fixture
    {
        public MainPage MainPage { get; } = new();

        public UserFormPage UserFormPage { get; } = new();

        public void NavigateToMain()
        {
            MainPage.Navigated = true;
        }

        public void NavigateToUserForm()
        {
            UserFormPage.Navigated = true;
        }
    }

    private sealed class MainPage
    {
        public bool Navigated { get; set; }

        public TextEntry NameEntry { get; } = new();

        public GreetingButton GreetButton { get; } = new();

        public TextLabel GreetingLabel { get; } = new();

        public bool WaitReady(int? timeoutMs = null)
        {
            GreetButton.Clicked = () =>
            {
                GreetingLabel.Text = string.IsNullOrWhiteSpace(NameEntry.Text)
                    ? "Please enter your name"
                    : $"Hello, {NameEntry.Text}!";
            };
            return true;
        }
    }

    private sealed class UserFormPage
    {
        public bool Navigated { get; set; }

        public TextEntry FirstNameEntry { get; } = new();

        public ToggleControl TermsCheckBox { get; } = new();

        public PickerControl CountryPicker { get; } = new();

        public bool WaitReady(int? timeoutMs = null)
        {
            return true;
        }
    }

    private sealed class TextEntry
    {
        public string Text { get; private set; } = string.Empty;

        public void Enter(string? text, int? timeoutMs = null)
        {
            Text += text;
        }

        public void Clear(int? timeoutMs = null)
        {
            Text = string.Empty;
        }

        public void AssertTextContains(string? expected, string? message = null, int? timeoutMs = null)
        {
            Assert.Contains(expected ?? string.Empty, Text);
        }
    }

    private sealed class GreetingButton
    {
        public Action? Clicked { get; set; }

        public void Click(int? timeoutMs = null)
        {
            Clicked?.Invoke();
        }
    }

    private sealed class TextLabel
    {
        public string Text { get; set; } = string.Empty;

        public string GetText(int? timeoutMs = null)
        {
            return Text;
        }

        public void AssertTextContains(string? expected, string? message = null, int? timeoutMs = null)
        {
            Assert.Contains(expected ?? string.Empty, Text);
        }
    }

    private sealed class ToggleControl
    {
        public bool Checked { get; private set; }

        public void Check(int? timeoutMs = null)
        {
            Checked = true;
        }

        public void AssertChecked(bool? expected, string? message = null, int? timeoutMs = null)
        {
            Assert.Equal(expected, Checked);
        }
    }

    private sealed class PickerControl
    {
        public string SelectedText { get; private set; } = string.Empty;

        public void SelectByText(string? text, int? timeoutMs = null)
        {
            SelectedText = text ?? string.Empty;
        }

        public void AssertSelectedText(string? expected, string? message = null, int? timeoutMs = null)
        {
            Assert.Equal(expected, SelectedText);
        }
    }
}

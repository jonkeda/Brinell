using Xunit;

namespace Brinell.Uat.Tests;

/// <summary>A step that starts with an app or user runs on that app, in the first person (AD-015).</summary>
public sealed class UatSubjectTests
{
    private const string Apps = """
        ## Apps

        | Name | User | Head |
        | --- | --- | --- |
        | Daan's PC | Daan | |
        | Daan's phone | Daan | |
        | Eva | | |
        | Eva's phone | Eva | |
        | Finn's web | Finn | web |
        """;

    [Theory]
    [InlineData("Eva taps OK", "Test.Tap")]
    [InlineData("Eva tap OK", "Test.Tap")]
    [InlineData("Eva is on the Main page", "Test.Open")]
    [InlineData("Eva has 3 todos", "Test.Have")]
    [InlineData("Eva does the dishes", "Test.Dishes")]
    [InlineData("Eva advances the clock", "Test.Advance")]
    [InlineData("Eva presses Enter", "Test.Press")]
    [InlineData("Eva tries again", "Test.Retry")]
    [InlineData("Eva should be on the Main page", "Test.AssertOpen")]
    public void ASubject_BindsTheFirstPersonPhrase_OnItsApp(string step, string commandId)
    {
        var invocation = BindSingle($"When {step}");

        Assert.Equal(commandId, invocation.CommandId);
        Assert.Equal("Eva", invocation.App);
    }

    [Fact]
    public void ASubject_FillsThePhrasesArguments()
    {
        var invocation = BindSingle("When Eva presses Enter");

        Assert.Equal("Enter", invocation.Arguments["key"]);
        Assert.Equal("Eva presses Enter", invocation.Step.Text);
    }

    [Fact]
    public void ASubject_Sticks_UntilAnotherIsNamed()
    {
        var invocations = BindScenario("""
            When I tap OK
            And Eva taps OK
            And I tap OK
            And Finn's web taps OK
            And I tap OK
            """);

        Assert.Equal([null, "Eva", "Eva", "Finn's web", "Finn's web"], invocations.Select(invocation => invocation.App));
    }

    [Fact]
    public void TheLongestName_Wins()
    {
        Assert.Equal("Eva's phone", BindSingle("When Eva's phone taps OK").App);
    }

    [Fact]
    public void AUser_WithOneApp_PicksThatApp()
    {
        Assert.Equal("Finn's web", BindSingle("When Finn taps OK").App);
    }

    [Fact]
    public void AUser_WithSeveralApps_PicksTheOneUsedLast()
    {
        var invocations = BindScenario("""
            When Daan's phone taps OK
            And Daan taps OK
            """);

        Assert.Equal("Daan's phone", invocations[1].App);
    }

    [Fact]
    public void AUser_WithSeveralApps_NoneUsedYet_FailsNamingTheApps()
    {
        var result = Bind("When Daan taps OK");

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("UATB006", diagnostic.Code);
        Assert.Contains("Daan's PC, Daan's phone", diagnostic.Message);
    }

    [Fact]
    public void AStepThatStartsWithAControl_IsNotASubject()
    {
        var invocation = BindSingle("""Then Todos should have todo "Buy milk" """);

        Assert.Equal("Test.HasTodo", invocation.CommandId);
        Assert.Null(invocation.App);
    }

    [Fact]
    public void AName_NotInTheAppsTable_IsNotASubject()
    {
        var diagnostic = Assert.Single(Bind("When Dan taps OK").Diagnostics);

        Assert.Equal("UATB001", diagnostic.Code);
        Assert.DoesNotContain("tried", diagnostic.Message);
    }

    [Fact]
    public void ASubject_WithNoFirstPersonPhrase_ListsWhatWasTried()
    {
        var diagnostic = Assert.Single(Bind("When Eva frobnicates the widget").Diagnostics);

        Assert.Equal("UATB001", diagnostic.Code);
        Assert.Contains("As Eva, tried 'I frobnicates the widget'", diagnostic.Message);
        Assert.Contains("'I frobnicate the widget'", diagnostic.Message);
    }

    [Fact]
    public void ASubject_MatchingTwoFirstPersonPhrases_IsAmbiguous()
    {
        var catalog = CreateCatalog();
        catalog.Register(UatEffectiveStepKeyword.When, "I taps {control}", "Test.Taps");

        var diagnostic = Assert.Single(Bind("When Eva taps OK", catalog).Diagnostics);

        Assert.Equal("UATB002", diagnostic.Code);
        Assert.Contains("'I tap {control}'", diagnostic.Message);
        Assert.Contains("'I taps {control}'", diagnostic.Message);
    }

    [Fact]
    public void UsesTheApp_SwitchesTheAppForLaterSteps()
    {
        var invocations = BindScenario("""
            Given Daan's PC uses the app
            When Daan taps OK
            """);

        Assert.Equal(["Daan's PC", "Daan's PC"], invocations.Select(invocation => invocation.App));
        Assert.Equal("Daan's PC", invocations[0].Arguments["app"]);
    }

    [Fact]
    public void AnAppOfAnotherHead_BindsAgainstThatHeadsCatalog()
    {
        var web = new UatCommandCatalog();
        web.Register(UatEffectiveStepKeyword.When, "I tap {control}", "Web.Tap");

        var invocations = BindScenario(
            """
            When Eva taps OK
            And Finn taps OK
            And I tap OK
            """,
            catalogsByHead: new Dictionary<string, UatCommandCatalog> { ["web"] = web });

        Assert.Equal(["Test.Tap", "Web.Tap", "Web.Tap"], invocations.Select(invocation => invocation.CommandId));
    }

    [Fact]
    public void AUserParameter_InFirstPlace_SwitchesToThatUsersApp()
    {
        var invocation = BindSingle("Then Finn should have 3 todos on the server");

        Assert.Equal("Test.ServerCount", invocation.CommandId);
        Assert.Equal("Finn", invocation.Arguments["user"]);
        Assert.Equal("Finn's web", invocation.App);
    }

    [Fact]
    public void AUserParameter_SaidAsI_IsTheCurrentAppsUser()
    {
        var invocations = BindScenario("""
            When Finn's web taps OK
            Then I should have 3 todos on the server
            """);

        Assert.Equal("Finn", invocations[1].Arguments["user"]);
    }

    [Fact]
    public void AUserParameter_SaidAsI_WithNoAppCurrent_Fails()
    {
        var diagnostic = Assert.Single(Bind("Then I should have 3 todos on the server").Diagnostics);

        Assert.Equal("UATB007", diagnostic.Code);
        Assert.Contains("no app is current", diagnostic.Message);
    }

    [Fact]
    public void AUserParameter_InALaterPlace_IsCheckedButDoesNotSwitch()
    {
        var invocation = BindSingle("""Then the server should have received "Milk" from Finn""");

        Assert.Equal("Finn", invocation.Arguments["user"]);
        Assert.Null(invocation.App);
    }

    [Fact]
    public void AnUnknownUser_InAUserParameter_Fails()
    {
        var diagnostic = Assert.Single(Bind("""Then the server should have received "Milk" from Zed""").Diagnostics);

        Assert.Equal("UATB007", diagnostic.Code);
        Assert.Contains("no app or user 'Zed'", diagnostic.Message);
    }

    [Fact]
    public void AnAppNamedLikeAControl_IsAnError()
    {
        var catalogs = new UatCatalogSet(CreateCatalog(), ReadApps(), suiteHead: null, new Dictionary<string, UatCommandCatalog>(), ["eva"]);

        var result = UatBinder.Bind(Parse("When I tap OK"), catalogs);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("UATB005", diagnostic.Code);
        Assert.Contains("'Eva'", diagnostic.Message);
    }

    [Fact]
    public void WithoutApps_NothingIsASubject()
    {
        var result = UatBinder.Bind(Parse("When Eva taps OK"), CreateCatalog());

        Assert.Equal("UATB001", Assert.Single(result.Diagnostics).Code);
    }

    [Fact]
    public void AppsTable_UserAndHead_DefaultToTheNameAndTheSuite()
    {
        var apps = ReadApps();

        Assert.Equal("Eva", apps.Single(app => app.Name == "Eva").User);
        Assert.Equal("Daan", apps.Single(app => app.Name == "Daan's PC").User);
        Assert.Null(apps.Single(app => app.Name == "Eva").Head);
        Assert.Equal("web", apps.Single(app => app.Name == "Finn's web").Head);
    }

    private static UatStepInvocation BindSingle(string step)
        => Assert.Single(BindScenario(step));

    private static IReadOnlyList<UatStepInvocation> BindScenario(
        string steps,
        IReadOnlyDictionary<string, UatCommandCatalog>? catalogsByHead = null)
    {
        var catalogs = new UatCatalogSet(CreateCatalog(), ReadApps(), suiteHead: null, catalogsByHead ?? new Dictionary<string, UatCommandCatalog>(), []);
        var result = UatBinder.Bind(Parse(steps), catalogs);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics.Select(x => $"{x.Code} {x.Message}")));
        return Assert.Single(result.Document!.Scenarios).Invocations;
    }

    private static UatBindResult Bind(string steps, UatCommandCatalog? catalog = null)
        => UatBinder.Bind(
            Parse(steps),
            new UatCatalogSet(catalog ?? CreateCatalog(), ReadApps(), suiteHead: null, new Dictionary<string, UatCommandCatalog>(), []));

    private static UatDocument Parse(string steps)
    {
        var result = UatMarkdownParser.Parse($"""
            # UAT: Subjects

            ## Scenario: Subjects

            {steps}
            """);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics.Select(x => $"{x.Code} {x.Message}")));
        return result.Document!;
    }

    private static IReadOnlyList<UatAppConfig> ReadApps() => UatConfigParser.Parse($"# UAT Config\n\n{Apps}").Apps;

    private static UatCommandCatalog CreateCatalog()
    {
        var catalog = new UatCommandCatalog();
        catalog.Register(UatEffectiveStepKeyword.Given, "I am on the {page} page", "Test.Open");
        catalog.Register(UatEffectiveStepKeyword.Given, "{app} uses the app", "Test.Use");
        catalog.Register(UatEffectiveStepKeyword.When, "{app} uses the app", "Test.Use");
        catalog.Register(UatEffectiveStepKeyword.When, "I am on the {page} page", "Test.Open");
        catalog.Register(UatEffectiveStepKeyword.When, "I tap {control}", "Test.Tap");
        catalog.Register(UatEffectiveStepKeyword.When, "I have {count} todos", "Test.Have");
        catalog.Register(UatEffectiveStepKeyword.When, "I do the dishes", "Test.Dishes");
        catalog.Register(UatEffectiveStepKeyword.When, "I advance the clock", "Test.Advance");
        catalog.Register(UatEffectiveStepKeyword.When, "I press {key}", "Test.Press");
        catalog.Register(UatEffectiveStepKeyword.When, "I try again", "Test.Retry");
        catalog.Register(UatEffectiveStepKeyword.When, "I should be on the {page} page", "Test.AssertOpen");
        catalog.Register(UatEffectiveStepKeyword.Then, "{control} should have todo {title}", "Test.HasTodo");
        catalog.Register(UatEffectiveStepKeyword.Then, "{user} should have {count} todos on the server", "Test.ServerCount");
        catalog.Register(UatEffectiveStepKeyword.Then, "the server should have received {title} from {user}", "Test.ServerReceived");
        return catalog;
    }
}

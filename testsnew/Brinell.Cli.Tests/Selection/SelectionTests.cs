using Brinell.Cli.Selection;

namespace Brinell.Cli.Tests.Selection;

// brinell tests touch-map and affected: a suite, its app and a library on disk, and one run's timings.
public sealed class SelectionTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("brinell-selection-").FullName;
    private readonly string _suite;
    private readonly string _app;

    public SelectionTests()
    {
        _suite = Write("Shop.UITests/Shop.UITests.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <ProjectReference Include="..\Shop.App\Shop.App.csproj" />
                <ProjectReference Include="..\Lib\Lib.csproj" />
              </ItemGroup>
            </Project>
            """);
        Write("Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        Write("Lib/Control.cs", "class Control {}");
        _app = Write("Shop.App/Shop.App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk.Web\" />");
        Write("Shop.App/Components/Pages/Counter.razor", """
            @page "/counter"
            <h1 data-testid="counter-title">Counter</h1>
            <button data-testid="increment-btn">+</button>
            """);
        Write("Shop.App/Components/Pages/Cart.razor", """
            @page "/cart"
            @foreach (var line in Lines) { <li data-testid="line-@line.Id">@line.Name</li> }
            <input id="coupon" />
            """);
        Write("Shop.App/Components/Pages/About.razor", "<p>About</p>");
        Write("Shop.App/Components/Layout/MainLayout.razor", "@Body");
        Write("Shop.UITests/uat.config.md", "# UAT Config\n");
        Write("Shop.UITests/Features/counter.feature.md", """
            # Feature: Counter

            ## Scenario: Counting up

            ```gherkin
            Given the counter page
            ```
            """);
        Write("Shop.UITests/PageObjects/CounterPage.cs", "namespace Shop.UITests.PageObjects;\npublic sealed class CounterPage {}");
        Write("Shop.UITests/PageObjects/CartPage.cs", "namespace Shop.UITests.PageObjects;\npublic sealed class CartPage {}");
        Write("Shop.UITests/PageObjects/AboutPage.cs", "namespace Shop.UITests.PageObjects;\npublic sealed class AboutPage {}");
        Write("Shop.UITests/Tests/CartTests.cs", "namespace Shop.UITests.Tests;\npublic sealed class CartTests\n{\n    [Fact]\n    public void Coupon() {}\n}");
        Write("Shop.UITests/TestBase/ShopTestBase.cs", "namespace Shop.UITests;\npublic abstract class ShopTestBase {}");
        Write("timings/timings-1.csv", """
            Run;Head;Test;Scenario;Step;Kind;Name;Page;Control;Action;StartMs;DurationMs;Attempts;Result
            1;bunit;Shop.UITests.Tests.CounterTests.Title;;;navigate;NavigateTo;;;;0;9;;
            1;bunit;Shop.UITests.Tests.CounterTests.Title;;;call;AssertVisible;CounterPage;[data-testid='counter-title'];AssertVisible;9;1;1;ok
            1;bunit;Shop.UITests.Tests.CartTests.Coupon;;;call;Enter;CartPage;#coupon;Enter;10;1;1;ok
            1;bunit;Shop.UITests.Tests.CartTests.Lines;;;call;Count;CartPage;Rows('Css:[data-testid^='line-']');Count;11;1;1;ok
            1;bunit;Shop.UITests.Tests.ShellTests.Menu;;;call;Click;SampleShell;SampleShell;Click;12;1;1;ok
            1;bunit;Shop.UITests.Tests.CounterTests.Visible;;;query;TryFind;CounterPage;[data-testid='increment-btn'];TryFind;12;0;;
            1;bunit;Uat: Counting up;Counting up;3: Given the counter page;call;Click;CounterPage;[data-testid='increment-btn'];Click;13;1;1;ok
            """);
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private string Write(string relative, string content)
    {
        var path = Path.Combine(_root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return Path.GetDirectoryName(path)!;
    }

    private string At(string relative) => Path.Combine(_root, relative);

    private TouchMap Map() => TouchMap.Read(TouchMap.CsvFiles(At("timings")), _suite);

    private Affected Changed(params string[] files) => Affected.Of(_suite, null, files.Select(At), Map());

    [Fact]
    public void TouchMap_KeysATestByItsFullName_AndAScenarioByItsAddress()
    {
        var map = Map();

        Assert.Equal("FullyQualifiedName=Shop.UITests.Tests.CounterTests.Title", map.Tests.Single(test => test.Name == "Shop.UITests.Tests.CounterTests.Title").Filter);
        var scenario = map.Tests.Single(test => test.Name == "Counting up");
        Assert.Equal("BddScenario=Features/counter.feature.md#Counting-up", scenario.Filter);
        Assert.Equal(["CounterPage"], scenario.Pages);
        Assert.Equal(["[data-testid='increment-btn']"], scenario.Controls);
    }

    [Fact]
    public void TouchMap_CountsAQueryOutsideACall()
        => Assert.Equal(["[data-testid='increment-btn']"], Map().Tests.Single(test => test.Name == "Shop.UITests.Tests.CounterTests.Visible").Controls);

    [Fact]
    public void TouchMap_SavedAndLoaded_IsTheSame()
    {
        var map = Map();
        map.Save(At(TouchMap.FileName));

        Assert.Equal(map.Tests.Select(test => (test.Name, test.Filter, string.Join(',', test.Controls))),
            TouchMap.Load(At(TouchMap.FileName)).Tests.Select(test => (test.Name, test.Filter, string.Join(',', test.Controls))));
    }

    [Fact]
    public void APage_ReachesTheTestsThatUsedItsTestIds_AndNoOthers()
    {
        var affected = Changed("Shop.App/Components/Pages/Counter.razor");

        Assert.Null(affected.Whole);
        Assert.Equal(["Counting up", "Shop.UITests.Tests.CounterTests.Title", "Shop.UITests.Tests.CounterTests.Visible"], affected.Picked.Select(test => test.Name));
        Assert.Equal("BddScenario=Features/counter.feature.md#Counting-up|FullyQualifiedName=Shop.UITests.Tests.CounterTests.Title|FullyQualifiedName=Shop.UITests.Tests.CounterTests.Visible", affected.Filter);
        Assert.Contains("counter-title of Components/Pages/Counter.razor", affected.Picked[1].Why);
    }

    [Fact]
    public void APagesCode_ReachesWhatItsMarkupDoes()
        => Assert.Equal(3, Changed("Shop.App/Components/Pages/Counter.razor.cs").Picked.Count);

    [Fact]
    public void AnIdBuiltAtRunTime_CountsByItsStart_AndAPlainId()
        => Assert.Equal(["Shop.UITests.Tests.CartTests.Coupon", "Shop.UITests.Tests.CartTests.Lines"], Changed("Shop.App/Components/Pages/Cart.razor").Picked.Select(test => test.Name));

    [Fact]
    public void APageNoTestReaches_RunsNothing_AndSaysSo()
    {
        var affected = Changed("Shop.App/Components/Pages/About.razor");

        Assert.Null(affected.Filter);
        Assert.Contains("Components/Pages/About.razor: no test ids", affected.Notes);
    }

    [Fact]
    public void APageObject_ReachesTheTestsThatUsedIt()
        => Assert.Equal(["Shop.UITests.Tests.CartTests.Coupon", "Shop.UITests.Tests.CartTests.Lines"], Changed("Shop.UITests/PageObjects/CartPage.cs").Picked.Select(test => test.Name));

    [Fact]
    public void ATestClass_RunsItsTests()
        => Assert.Equal("FullyQualifiedName~Shop.UITests.Tests.CartTests.", Changed("Shop.UITests/Tests/CartTests.cs").Filter);

    [Fact]
    public void AFeatureFile_RunsItsScenarios()
        => Assert.Equal("BddScenario=Features/counter.feature.md#Counting-up", Changed("Shop.UITests/Features/counter.feature.md").Filter);

    [Theory]
    [InlineData("Shop.App/Components/Layout/MainLayout.razor", "shared by the app's pages")]
    [InlineData("Shop.App/Program.cs", "shared by the app's pages")]
    [InlineData("Lib/Control.cs", "in Lib, which the suite builds on")]
    [InlineData("Shop.UITests/TestBase/ShopTestBase.cs", "the suite's shared test code")]
    [InlineData("Shop.UITests/uat.config.md", "the suite's set-up")]
    [InlineData("Directory.Packages.props", "every project's build")]
    public void SharedCode_RunsTheSuiteWhole(string file, string why)
    {
        var affected = Changed("Shop.App/Components/Pages/Counter.razor", file);

        Assert.Contains(why, affected.Whole);
        Assert.Null(affected.Filter);
    }

    [Fact]
    public void AFileOutsideTheSuiteItsAppAndItsLibraries_RunsNothing()
    {
        var affected = Changed("docs/readme.md", "Other/Other.cs");

        Assert.Null(affected.Whole);
        Assert.Empty(affected.Picked);
    }

    [Fact]
    public void TheAlwaysSet_JoinsAPick_ButNotAnEmptyOne()
    {
        Assert.EndsWith("|Gate=Smoke", Affected.Of(_suite, null, [At("Shop.App/Components/Pages/Counter.razor")], Map(), ["Gate=Smoke"]).Filter);
        Assert.Null(Affected.Of(_suite, null, [At("docs/readme.md")], Map(), ["Gate=Smoke"]).Filter);
    }

    [Fact]
    public async Task TheCommands_WriteTheMap_ThenPrintTheFilterWithWhy()
    {
        var io = new CliIo(TextReader.Null, new StringWriter(), new StringWriter(), _root, Interactive: false);
        Assert.Equal(0, await BrinellCommand.RunAsync(["tests", "touch-map", "timings", "--suite", "Shop.UITests"], io));
        Assert.True(File.Exists(Path.Combine(_suite, TouchMap.FileName)));

        var output = new StringWriter();
        var exit = await BrinellCommand.RunAsync(["tests", "affected", "--suite", "Shop.UITests", "--files", "Shop.UITests/PageObjects/CounterPage.cs"],
            io with { Out = output });

        Assert.Equal(0, exit);
        Assert.Contains("Counting up: uses CounterPage (PageObjects/CounterPage.cs)", output.ToString());
        Assert.Contains("--filter \"BddScenario=Features/counter.feature.md#Counting-up|FullyQualifiedName=Shop.UITests.Tests.CounterTests.Title|FullyQualifiedName=Shop.UITests.Tests.CounterTests.Visible\"", output.ToString());
    }
}

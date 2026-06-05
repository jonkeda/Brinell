# Test Composition

## Rules

- Fixture owns app lifecycle and root services.
- Fixture exposes one `TestComposition Composition`.
- Pages are discovered automatically from `[TestPage]` or Brinell page bases.
- Pages are scoped DI services.
- Pages expose typed ControlObjects.
- Flows and scenario services are `[TestScenarioService]` scoped services.
- Scenarios are optional; tests may resolve pages, flows, or scenarios directly.
- UAT phrase classes are `[UatPhraseClass]` scoped services.
- Fixture page properties and fixture `[UatPhrase]` methods remain compatibility only.
- No page catalogs.
- No custom page factories.

## Fixture

```csharp
[TestModuleScan(typeof(BodyCamFixture), NamespacePrefix = "BodyCam.UAT.Runtime")]
public sealed class BodyCamFixture : IDisposable
{
    public BodyCamFixture()
    {
        Context = CreateContext();

        Composition = TestComposition.ForFixture(this, services =>
        {
            services.AddSingleton<IMauiTestContext>(Context);
            services.AddSingleton(Settings);
        });
    }

    public IMauiTestContext Context { get; }

    public BodyCamTestSettings Settings { get; } = BodyCamTestSettings.Load();

    public TestComposition Composition { get; }

    public void NavigateToMain()
    {
        Context.NavigateToRoot();
    }

    public void Dispose()
    {
        Context.Dispose();
    }
}
```

## Page

```csharp
[TestPage("Main")]
public sealed class MainPage : PageObjectBase<MainPage>
{
    public MainPage(IMauiTestContext context)
        : base(context)
    {
    }

    public Grid<MainPage> ActionsDrawer => new(this, "ActionsDrawer");

    public Button<MainPage> ActionsDrawerButton => new(this, "ActionsDrawerButton");

    public Button<MainPage> LookButton => new(this, "LookButton");

    public Button<MainPage> LookOverviewButton => new(this, "LookOverviewButton");

    public Label<MainPage> TranscriptText => new(this, "TranscriptText");

    public override bool IsLoaded(int? timeoutMs = null)
    {
        return TranscriptText.WaitVisible(true, timeoutMs);
    }

    public void EnsureActionsExpanded()
    {
        if (ActionsDrawer.WaitVisible(true, 1000))
        {
            return;
        }

        ActionsDrawerButton.Click();
        ActionsDrawer.AssertVisible(true, timeoutMs: 5000);
    }
}
```

## Flow

```csharp
[TestScenarioService]
public sealed class CameraActionFlow : TestScenarioServiceBase
{
    private readonly MainPage _main;

    public CameraActionFlow(MainPage main)
    {
        _main = main;
    }

    public void OpenActionSurface()
    {
        _main.WaitReady(5000);
        _main.EnsureActionsExpanded();
    }

    public void ChooseLookOverview()
    {
        _main.LookButton.Click();
        _main.LookOverviewButton.Click();
    }

    public void AssertActionSurfaceClosed()
    {
        _main.ActionsDrawer.AssertVisible(false, timeoutMs: 5000);
    }
}
```

## Scenario Service

```csharp
[TestScenarioService]
public sealed class LookOverviewScenario : TestScenarioServiceBase
{
    private readonly CameraActionFlow _cameraActions;
    private readonly MainPage _main;

    public LookOverviewScenario(CameraActionFlow cameraActions, MainPage main)
    {
        _cameraActions = cameraActions;
        _main = main;
    }

    public void Run()
    {
        _cameraActions.OpenActionSurface();
        _cameraActions.ChooseLookOverview();
        _cameraActions.AssertActionSurfaceClosed();
        _main.TranscriptText.AssertTextContains("Look Overview", timeoutMs: 5000);
    }
}
```

## Test Resolving A Scenario

```csharp
public sealed class CameraActionUiTests : IClassFixture<BodyCamFixture>
{
    private readonly BodyCamFixture _fixture;

    public CameraActionUiTests(BodyCamFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void LookOverview_ClosesActionSurface()
    {
        using var scope = _fixture.Composition.CreateScope();
        var scenario = scope.ServiceProvider.GetRequiredService<LookOverviewScenario>();

        scenario.Run();
    }
}
```

## Test Resolving Pages Directly

```csharp
public sealed class MainPageTests : IClassFixture<BodyCamFixture>
{
    private readonly BodyCamFixture _fixture;

    public MainPageTests(BodyCamFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void MainPage_CanOpenActions()
    {
        using var scope = _fixture.Composition.CreateScope();
        var page = scope.ServiceProvider.GetRequiredService<MainPage>();

        _fixture.NavigateToMain();
        page.WaitReady(5000);
        page.EnsureActionsExpanded();

        page.ActionsDrawer.AssertVisible(true, timeoutMs: 5000);
    }
}
```

## UAT Phrases

```csharp
[UatPhraseClass]
public sealed class CameraActionUatPhrases : UatPhraseClassBase
{
    private readonly CameraActionFlow _cameraActions;
    private readonly LookOverviewScenario _lookOverview;

    public CameraActionUatPhrases(
        CameraActionFlow cameraActions,
        LookOverviewScenario lookOverview)
    {
        _cameraActions = cameraActions;
        _lookOverview = lookOverview;
    }

    public void GivenTheCameraActionSurfaceIsOpen()
    {
        _cameraActions.OpenActionSurface();
    }

    public void WhenIChooseLookOverview()
    {
        _cameraActions.ChooseLookOverview();
    }

    public void ThenTheCameraActionSurfaceShouldBeClosed()
    {
        _cameraActions.AssertActionSurfaceClosed();
    }

    [UatPhrase(UatEffectiveStepKeyword.When, "I run the Look Overview scenario")]
    public void RunLookOverviewScenario()
    {
        _lookOverview.Run();
    }
}
```

## UAT Test Base

```csharp
public sealed class BodyCamUatScenarioTests(BodyCamFixture fixture)
    : UatScenarioTestBase<BodyCamFixture>(fixture),
        IClassFixture<BodyCamFixture>
{
    public static IEnumerable<object[]> ScenarioFiles => GetScenarioFiles();

    protected override UatRuntimeValidationOptions RuntimeValidation { get; } =
        new(Target: "MAUI", Fixture: "BodyCamFixture");

    [Theory(Timeout = 120000)]
    [MemberData(nameof(ScenarioFiles))]
    public Task UatFile_Passes(string filePath)
    {
        return RunUatFileAsync(filePath);
    }
}
```

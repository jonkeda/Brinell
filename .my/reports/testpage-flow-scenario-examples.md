```csharp
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Brinell.Maui.Controls.Buttons;
using Brinell.Maui.Controls.Container;
using Brinell.Maui.Controls.Display;

namespace BodyCam.UAT.Runtime;

[TestPage("Main")]
public sealed class MainPage : PageObjectBase<MainPage>
{
    public MainPage(IMauiTestContext context)
        : base(context)
    {
    }

    public Grid<MainPage> ActionsDrawer => new(this, "ActionsDrawer");

    public Button<MainPage> LookButton => new(this, "LookButton");

    public Button<MainPage> LookOverviewButton => new(this, "LookOverviewButton");

    public void Open()
    {
        WaitReady(5000);
    }

    public void OpenActions()
    {
        ActionsDrawer.AssertVisible(timeoutMs: 5000);
    }
}

[TestPage("Transcript")]
public sealed class TranscriptPage : PageObjectBase<TranscriptPage>
{
    public TranscriptPage(IMauiTestContext context)
        : base(context)
    {
    }

    public Label<TranscriptPage> TranscriptText => new(this, "TranscriptText");

    public void Open()
    {
        WaitReady(5000);
    }
}

[TestScenarioService]
public sealed class CameraActionFlow
{
    private readonly MainPage _main;

    public CameraActionFlow(MainPage main)
    {
        _main = main;
    }

    public void OpenActionSurface()
    {
        _main.Open();
        _main.OpenActions();
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

[TestScenarioService]
public sealed class TranscriptFlow
{
    private readonly TranscriptPage _transcript;

    public TranscriptFlow(TranscriptPage transcript)
    {
        _transcript = transcript;
    }

    public void AssertLookOverviewWasRecorded()
    {
        _transcript.Open();
        _transcript.TranscriptText.AssertTextContains("Look Overview", timeoutMs: 5000);
    }
}

[TestScenarioService]
public sealed class LookOverviewScenario
{
    private readonly CameraActionFlow _cameraActions;
    private readonly TranscriptFlow _transcript;

    public LookOverviewScenario(
        CameraActionFlow cameraActions,
        TranscriptFlow transcript)
    {
        _cameraActions = cameraActions;
        _transcript = transcript;
    }

    public void Run()
    {
        _cameraActions.OpenActionSurface();
        _cameraActions.ChooseLookOverview();
        _cameraActions.AssertActionSurfaceClosed();
        _transcript.AssertLookOverviewWasRecorded();
    }
}

public sealed class CameraActionUiTests : IClassFixture<BodyCamFixture>
{
    private readonly BodyCamFixture _fixture;

    public CameraActionUiTests(BodyCamFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void LookOverview_ClosesActionSurface_AndWritesTranscript()
    {
        using var scope = _fixture.Composition.CreateScope();
        var scenario = scope.ServiceProvider.GetRequiredService<LookOverviewScenario>();

        scenario.Run();
    }
}
```

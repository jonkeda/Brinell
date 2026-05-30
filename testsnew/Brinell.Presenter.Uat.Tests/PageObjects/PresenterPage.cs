using Brinell.Maui.Controls.Buttons;
using Brinell.Maui.Controls.Display;
using Brinell.Maui.Controls.Selection;
using Brinell.Maui.Interfaces;
using Brinell.Maui.Pages;
using Brinell.Uat;

namespace Brinell.Presenter.Uat.Tests.PageObjects;

public sealed class PresenterPage : PageObjectBase<PresenterPage>
{
    public PresenterPage(IMauiTestContext context)
        : base(context)
    {
    }

    public override string Name => "Presenter";

    [UatName("Status Summary")]
    public PresenterStatusLabel<PresenterPage> StatusSummary => new(this, "StatusSummaryLabel");

    [UatName("Workspace Summary")]
    public Label<PresenterPage> WorkspaceSummary => Label("WorkspaceSummaryLabel");

    [UatName("Scenario List")]
    public Label<PresenterPage> ScenarioList => Label("ScenarioListText");

    [UatName("Step List")]
    public Label<PresenterPage> StepList => Label("StepListText");

    [UatName("Workspace Config")]
    public Label<PresenterPage> WorkspaceConfig => Label("WorkspaceConfigText");

    public Button<PresenterPage> RunSelectedButton => Button("RunSelectedButton");

    public Button<PresenterPage> ValidateButton => Button("ValidateButton");

    public Button<PresenterPage> NextStepButton => Button("NextStepButton");

    [UatName("Execution Mode")]
    public Picker<PresenterPage> ExecutionModePicker => Picker("ExecutionModePicker");

    public override bool IsLoaded(int? timeoutMs = null)
    {
        return StatusSummary.IsExists();
    }
}

public sealed class PresenterStatusLabel<TScope> : Label<TScope>
    where TScope : IMauiScope<TScope>
{
    public PresenterStatusLabel(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }

    public new TScope AssertTextContains(string? expected, string? message = null, int? timeoutMs = null)
    {
        return base.AssertTextContains(expected, message, timeoutMs ?? 90000);
    }
}

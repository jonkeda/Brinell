namespace Brinell.Uat;

public static class UatSpecCommandCatalog
{
    public static UatCommandCatalog CreateDefault()
    {
        var catalog = new UatCommandCatalog();
        RegisterDefault(catalog);
        return catalog;
    }

    public static void RegisterDefault(UatCommandCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        catalog.Register(UatEffectiveStepKeyword.Given, "I am on the {page} page", "Spec.Page.Open");
        catalog.Register(UatEffectiveStepKeyword.Then, "I should be on the {page} page", "Spec.Page.AssertOpen");
        catalog.Register(UatEffectiveStepKeyword.When, "I tap {control}", "Spec.Control.Tap");
        catalog.Register(UatEffectiveStepKeyword.When, "I enter {value} into {control}", "Spec.Control.Enter");
        catalog.Register(UatEffectiveStepKeyword.When, "I set {control} to {value}", "Spec.Control.SetText");
        catalog.Register(UatEffectiveStepKeyword.When, "I clear {control}", "Spec.Control.Clear");
        catalog.Register(UatEffectiveStepKeyword.When, "I check {control}", "Spec.Control.Check");
        catalog.Register(UatEffectiveStepKeyword.When, "I uncheck {control}", "Spec.Control.Uncheck");
        catalog.Register(UatEffectiveStepKeyword.When, "I select {value} from {control}", "Spec.Control.SelectByText");
        catalog.Register(UatEffectiveStepKeyword.Then, "{control} should contain {value}", "Spec.Control.AssertTextContains");
        catalog.Register(UatEffectiveStepKeyword.Then, "{control} should equal {value}", "Spec.Control.AssertText");
        catalog.Register(UatEffectiveStepKeyword.Then, "{control} should be visible", "Spec.Control.AssertVisible");
        catalog.Register(UatEffectiveStepKeyword.Then, "{control} should not be visible", "Spec.Control.AssertNotVisible");
        catalog.Register(UatEffectiveStepKeyword.Then, "{control} should be enabled", "Spec.Control.AssertEnabled");
        catalog.Register(UatEffectiveStepKeyword.Then, "{control} should be checked", "Spec.Control.AssertChecked.True");
        catalog.Register(UatEffectiveStepKeyword.Then, "{control} should be unchecked", "Spec.Control.AssertChecked.False");
        catalog.Register(UatEffectiveStepKeyword.Then, "{control} should have selected {value}", "Spec.Control.AssertSelectedText");
        catalog.Register(UatEffectiveStepKeyword.Then, "I should see {text}", "Spec.Page.AssertTextVisible");
    }
}

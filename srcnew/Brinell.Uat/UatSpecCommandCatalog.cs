namespace Brinell.Uat;

public static class UatSpecCommandCatalog
{
    public static UatCommandCatalog CreateDefault()
    {
        var catalog = new UatCommandCatalog { IsGeneric = true };
        RegisterDefault(catalog);
        return catalog;
    }

    public static void RegisterDefault(UatCommandCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        // The three intrinsic page-graph verbs and their named forms — phrases only, no handler in a
        // spec catalog.
        catalog.Register(UatEffectiveStepKeyword.Given, "I am on the {page} page", "Spec.Page.Open", namesPage: true);
        catalog.Register(UatEffectiveStepKeyword.Given, "{user} is on the {page} page", "Spec.Page.Open", namesPage: true);
        catalog.Register(UatEffectiveStepKeyword.Then, "I should be on the {page} page", "Spec.Page.AssertOpen", namesPage: true);
        catalog.Register(UatEffectiveStepKeyword.Then, "{user} should be on the {page} page", "Spec.Page.AssertOpen", namesPage: true);
        catalog.Register(UatEffectiveStepKeyword.Then, "I should see {text}", "Spec.Page.AssertTextVisible");
        catalog.Register(UatEffectiveStepKeyword.Then, "{user} should see {text}", "Spec.Page.AssertTextVisible");

        // Control vocabulary is discovered from the same [UatWhen]/[UatThen] attributes the runtime uses,
        // so the spec surface and the executable surface can never drift.
        UatCatalogBuilder.RegisterControlVerbs(
            catalog,
            UatCatalogBuilder.CoreStepTypes,
            "Spec.",
            handlerFactory: null);
    }
}

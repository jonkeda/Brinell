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

        // The three intrinsic page-graph verbs — phrases only, no handler in a spec catalog.
        catalog.Register(UatEffectiveStepKeyword.Given, "I am on the {page} page", "Spec.Page.Open");
        catalog.Register(UatEffectiveStepKeyword.Then, "I should be on the {page} page", "Spec.Page.AssertOpen");
        catalog.Register(UatEffectiveStepKeyword.Then, "I should see {text}", "Spec.Page.AssertTextVisible");

        // Control vocabulary is discovered from the same [UatStep] attributes the runtime uses,
        // so the spec surface and the executable surface can never drift.
        UatCatalogBuilder.RegisterControlVerbs(
            catalog,
            UatCatalogBuilder.CoreStepTypes,
            "Spec.",
            handlerFactory: null);
    }
}

using Brinell.Core.Composition;
using Brinell.Core.Testing;
using Xunit;

namespace Brinell.Uat.Tests.NamespaceCases;

/// <summary>Using: a file reaches another folder's phrases and pages by naming the namespace.</summary>
public sealed class UatUsingTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "brinell-using-" + Guid.NewGuid().ToString("N"), "Scenarios");

    public void Dispose()
    {
        if (Directory.Exists(Path.GetDirectoryName(_root)))
        {
            Directory.Delete(Path.GetDirectoryName(_root)!, recursive: true);
        }
    }

    [Fact]
    public void AUsingRow_ReachesASiblingsPhrases()
    {
        var bound = Bind("Accounts/account.uat.md", "When I add a line \"Rent\"", usings: "Invoicing");

        Assert.True(bound.Success, UatDiagnosticsFormatter.FormatDiagnostics(bound.Diagnostics));
        var invocation = Assert.Single(bound.Document!.Scenarios[0].Invocations);
        Assert.EndsWith("InvoicingPhrases.AddLine", invocation.Command.CommandId, StringComparison.Ordinal);
        Assert.Equal("Invoicing", invocation.Scope);
    }

    [Fact]
    public void WithoutIt_TheErrorSaysToAddOne()
    {
        var error = Assert.Single(Bind("Accounts/account.uat.md", "When I add a line \"Rent\"").Diagnostics);

        Assert.Equal("UATB010", error.Code);
        Assert.Contains("add a 'Using' row for it", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void UsingTwoNamespacesWithTheSamePhrase_IsAmbiguous()
    {
        var error = Assert.Single(Bind("Accounts/account.uat.md", "When I add a line \"Rent\"", usings: "Invoicing, Purchasing").Diagnostics, d => d.Severity == UatDiagnosticSeverity.Error);

        Assert.Equal("UATB002", error.Code);
    }

    [Fact]
    public void AUsingThatNamesNothing_FailsBinding_ListingTheNamespaces()
    {
        var error = Assert.Single(Bind("account.uat.md", "When I note \"paid\"", usings: "Invoicng").Diagnostics);

        Assert.Equal("UATB011", error.Code);
        Assert.Contains("Using 'Invoicng' names no namespace. The namespaces are Invoicing, Purchasing.", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AUsingNoStepNeeds_IsAWarning()
    {
        var bound = Bind("account.uat.md", "When I note \"paid\"", usings: "Purchasing");

        Assert.True(bound.Success);
        var warning = Assert.Single(bound.Diagnostics);
        Assert.Equal(("UATB012", UatDiagnosticSeverity.Warning), (warning.Code, warning.Severity));
    }

    [Fact]
    public void TheConfigsUsings_AreEveryFilesUsings()
    {
        var file = UatScenarioFiles.Load(Write("account.uat.md", Scenario("When I add a line \"Rent\"")));

        var bound = file.Bind(new UatCatalogSet(Catalog()) { Usings = ["Invoicing"] });

        Assert.True(bound.Success, UatDiagnosticsFormatter.FormatDiagnostics(bound.Diagnostics));
        Assert.Equal(["Backend", "Invoicing/Credit notes"], UatConfigParser.Parse("# UAT Config\n\n## Using\n\n| Namespace |\n| --- |\n| Backend |\n| Invoicing\\Credit notes |\n").Usings);
    }

    [Fact]
    public void AFilesOwnCallable_BindsWithTheFilesUsings_APhraseFilesDoesNot()
    {
        Write("Common.phrases.md", "# Phrases: Common\n\n## Phrase: a shared line\n\nWhen I add a line \"Rent\"\n");
        var own = Scenario("Given my own line", usings: "Invoicing") + "\n## Phrase: my own line\n\nWhen I add a line \"Rent\"\n";

        var ownBound = UatScenarioFiles.Load(Write("Accounts/own.uat.md", own)).Bind(new UatCatalogSet(Catalog()));
        var sharedBound = Bind("Accounts/shared.uat.md", "Given a shared line", usings: "Invoicing");

        Assert.True(ownBound.Success, UatDiagnosticsFormatter.FormatDiagnostics(ownBound.Diagnostics));
        Assert.Contains(sharedBound.Diagnostics, d => d.Code == "UATB010");
    }

    [Fact]
    public void TheStepReport_NamesTheScope()
    {
        var invocation = Assert.Single(Bind("Accounts/account.uat.md", "When I add a line \"Rent\"", usings: "Invoicing").Document!.Scenarios[0].Invocations);

        var line = Assert.Single(UatDiagnosticsFormatter.FormatSteps([UatStepResult.Passed(invocation)]));

        Assert.Contains("[Invoicing] ", line, StringComparison.Ordinal);
    }

    [Fact]
    public async Task APageName_ResolvesAmongThePagesTheFileSees_AndRunsThatPage()
    {
        var fixture = new DetailFixture();
        var catalogs = new UatCatalogSet(UatReflectionRuntime.FromRoot(fixture).CreateCommandCatalog());

        var invoicing = UatScenarioFiles.Load(Write("Invoicing/invoice.uat.md", Scenario("Given I am on the Detail page\nWhen I press Approve"))).Bind(catalogs);
        var accounts = UatScenarioFiles.Load(Write("Accounts/account.uat.md", Scenario("Given I am on the Detail page\nWhen I press Approve"))).Bind(catalogs);

        Assert.True(invoicing.Success, UatDiagnosticsFormatter.FormatDiagnostics(invoicing.Diagnostics));
        Assert.Equal("Invoicing/Detail", invoicing.Document!.Scenarios[0].Invocations[0].Arguments["page"]);
        Assert.True((await new UatScenarioRunner().RunAsync(invoicing.Document.Scenarios[0])).Passed);
        Assert.True((await new UatScenarioRunner().RunAsync(accounts.Document!.Scenarios[0])).Passed);
        Assert.Equal((1, 1), (fixture.Invoice.Approve.Pressed, fixture.Account.Approve.Pressed));
    }

    [Fact]
    public void APageOfAnotherFolder_FailsBinding_AndTwoSeenPagesOfOneName_AreAmbiguous_UnlessNamedWithTheirNamespace()
    {
        var catalogs = new UatCatalogSet(UatReflectionRuntime.FromRoot(new DetailFixture()).CreateCommandCatalog());

        var unseen = UatScenarioFiles.Load(Write("root.uat.md", Scenario("Given I am on the Detail page"))).Bind(catalogs);
        var both = UatScenarioFiles.Load(Write("Invoicing/both.uat.md", Scenario("Given I am on the Detail page", usings: "Accounts"))).Bind(catalogs);
        var named = UatScenarioFiles.Load(Write("Invoicing/named.uat.md", Scenario("Given I am on the Accounts/Detail page", usings: "Accounts"))).Bind(catalogs);

        Assert.Equal("UATB010", Assert.Single(unseen.Diagnostics).Code);
        var ambiguous = Assert.Single(both.Diagnostics, d => d.Severity == UatDiagnosticSeverity.Error);
        Assert.Equal("UATB002", ambiguous.Code);
        Assert.Contains("Name it with its namespace", ambiguous.Message, StringComparison.Ordinal);
        Assert.True(named.Success, UatDiagnosticsFormatter.FormatDiagnostics(named.Diagnostics));
        Assert.Empty(named.Diagnostics);
    }

    [Fact]
    public void OneNameInTwoNamespaces_IsNotADuplicatePage()
    {
        var catalog = TestCompositionCatalog.Discover(TestCompositionDiscoveryOptions.FromFixture(typeof(PageScanFixture)));

        Assert.Empty(catalog.Diagnostics);
        Assert.Equal(["Accounts", "Invoicing"], catalog.Pages.Where(page => page.Name == "Ledger").Select(page => page.Namespace).Order());
    }

    private UatBindResult Bind(string path, string steps, string? usings = null)
    {
        var file = UatScenarioFiles.Load(Write(path, Scenario(steps, usings)));
        Assert.True(file.Success, UatDiagnosticsFormatter.FormatDiagnostics(file.Diagnostics));
        return file.Bind(new UatCatalogSet(Catalog()));
    }

    private string Write(string relative, string text)
    {
        var path = Path.Combine(_root, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
        return path;
    }

    private static UatCommandCatalog Catalog()
    {
        var fixture = new NamespaceFixture();
        return UatReflectionRuntime.FromComposition(fixture, fixture.Composition).CreateCommandCatalog();
    }

    private static string Scenario(string steps, string? usings = null)
        => $"# UAT: Using\n\n{(usings is null ? string.Empty : $"## Metadata\n\n| Field | Value |\n| --- | --- |\n| Using | {usings} |\n\n")}## Scenario: Using\n\n{steps}\n";

    private sealed class DetailFixture
    {
        [UatName("Detail")]
        public InvoiceDetailPage Invoice { get; } = new();

        [UatName("Detail")]
        public AccountDetailPage Account { get; } = new();
    }

    [BddNamespace("Invoicing")]
    private sealed class InvoiceDetailPage
    {
        public PressControl Approve { get; } = new();

        public bool WaitReady(int? timeoutMs = null) => true;
    }

    [BddNamespace("Accounts")]
    private sealed class AccountDetailPage
    {
        public PressControl Approve { get; } = new();

        public bool WaitReady(int? timeoutMs = null) => true;
    }

    private sealed class PressControl
    {
        public int Pressed { get; private set; }

        [BddWhen("I press {control}")]
        public void Press() => Pressed++;
    }
}

[TestModuleScan(typeof(PageScanFixture), NamespacePrefix = "Brinell.Uat.Tests.PageNamespaceCases")]
[UatIgnore]
public sealed class PageScanFixture
{
}

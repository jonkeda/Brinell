using Bruinell.Core.Composition;
using Bruinell.Core.Testing;
using Bruinell.Html.Interfaces;
using Bruinell.Maui.Interfaces;
using Bruinell.Samples.Todo.Mixed.Uat.Phrases;
using Bruinell.Samples.Todo.TestSupport.Apps;
using Bruinell.Samples.Todo.UITests;
using Bruinell.Samples.Todo.Web.UITests;
using Bruinell.Uat;
using Microsoft.Extensions.DependencyInjection;

namespace Bruinell.Samples.Todo.Mixed.Uat.Runtime;

/// <summary>
/// The apps of <c>apps.md</c> presented to the UAT engine: a step that names Daan runs on Daan's
/// app, through the page objects of that app's head (AD-014, <c>.docs/contracts/uat-apps-host.md</c>).
/// </summary>
/// <remarks>
/// The fixture's own composition holds only the mixed phrases, for steps that name no app. Each
/// head's composition adds that head's page objects, and each app's adds its test context.
/// </remarks>
public sealed class TodoMixedUatFixture : TodoAppsFixture, IUatAppsHost
{
    private const string MixedPhrases = "Bruinell.Samples.Todo.Mixed.Uat.Phrases";

    private static readonly TestCompositionDiscoveryOptions MauiModules = Modules(
        (typeof(TodoMauiDevice), "Bruinell.Samples.Todo.UITests.Pages"));

    private static readonly TestCompositionDiscoveryOptions WebModules = Modules(
        (typeof(TodoWebDevice), "Bruinell.Samples.Todo.Web.UITests.Pages"));

    /// <summary>The mixed phrases, for steps before any app is named.</summary>
    public TestComposition Composition { get; private set; } = null!;

    /// <inheritdoc />
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        Composition = TestComposition.ForFixture(this, Modules());
    }

    /// <inheritdoc />
    public TestCompositionDiscoveryOptions? ModulesFor(TestAppDefinition app) => app.Head switch
    {
        "maui" => MauiModules,
        "web" => WebModules,
        _ => throw new NotSupportedException($"{app.Name} has head '{app.Head}'. The Todo apps are 'maui' or 'web'."),
    };

    /// <inheritdoc />
    public void AddAppServices(ITestApp app, IServiceCollection services)
    {
        switch (app)
        {
            case TodoMauiDevice maui:
                services.AddSingleton<IMauiTestContext>(maui.Context);
                break;
            case TodoWebDevice web:
                services.AddSingleton<IHtmlTestContext>(web.Context);
                break;
        }
    }

    // A head's pages, and the mixed phrases every head shares.
    private static TestCompositionDiscoveryOptions Modules(params (Type Marker, string Namespace)[] pages)
        => TestCompositionDiscoveryOptions.FromScans(pages
            .Select(page => new TestModuleScanAttribute(page.Marker) { NamespacePrefix = page.Namespace })
            .Append(new TestModuleScanAttribute(typeof(TodoMixedPhrases)) { NamespacePrefix = MixedPhrases }));
}

/// <summary>Binds the mixed scenarios to one <see cref="TodoMixedUatFixture"/>.</summary>
[CollectionDefinition(Name)]
public sealed class TodoMixedUatCollection : ICollectionFixture<TodoMixedUatFixture>
{
    /// <summary>The collection's name.</summary>
    public const string Name = "Todo mixed UAT";
}

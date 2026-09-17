using Microsoft.Extensions.DependencyInjection;
using Brinell.Presenter.Uat.Tests.PageObjects;

namespace Brinell.Presenter.Uat.Tests.Runtime;

[Collection(PresenterUatCollection.CollectionName)]
[Trait("Category", "UAT")]
[Trait("Target", "Presenter")]
public sealed class PresenterWorkspaceTreeTests : IDisposable
{
    private readonly IServiceScope _scope;
    private readonly PresenterPage _page;

    public PresenterWorkspaceTreeTests(PresenterFixture fixture)
    {
        _scope = fixture.Composition.CreateScope();
        _page = _scope.ServiceProvider.GetRequiredService<PresenterPage>();
    }

    [Fact]
    public void WorkspaceTree_ShowsMarkdownOnly()
    {
        ReloadWorkspace();

        var allTree = _page.AllWorkspaceTree.GetText(timeoutMs: 10000) ?? string.Empty;
        Assert.Contains("uat.config.md", allTree, StringComparison.Ordinal);
        Assert.Contains("main-page-greeting.uat.md", allTree, StringComparison.Ordinal);
        Assert.Contains("main-page-validation.uat.md", allTree, StringComparison.Ordinal);
        Assert.DoesNotContain(".csproj", allTree, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".dll", allTree, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("bin", allTree, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("obj", allTree, StringComparison.OrdinalIgnoreCase);
    }

    private void ReloadWorkspace()
    {
        Assert.True(_page.IsLoaded(timeoutMs: 30000), "Presenter page was not loaded.");
        _page.ReloadButton.Click();
        _page.StatusSummary.AssertTextContains("Ready", timeoutMs: 30000);
    }

    public void Dispose()
    {
        _scope.Dispose();
    }
}

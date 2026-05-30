namespace Brinell.Presenter.Uat.Tests.Runtime;

[CollectionDefinition(CollectionName)]
public sealed class PresenterUatCollection : ICollectionFixture<PresenterFixture>
{
    public const string CollectionName = "Presenter UAT";
}

using Brinell.Maui.UITests;

namespace Brinell.Maui.Uat.Tests.Runtime;

[CollectionDefinition(CollectionName)]
public sealed class MauiUatCollection : ICollectionFixture<AppiumFixture>
{
    public const string CollectionName = "MAUI UAT";
}

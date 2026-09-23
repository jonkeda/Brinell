namespace Brinell.Samples.Todo.Uat.Runtime;

/// <summary>Binds every UAT scenario class to the one <see cref="TodoUatFixture"/> (one app per run).</summary>
[CollectionDefinition(CollectionName)]
public sealed class TodoUatCollection : ICollectionFixture<TodoUatFixture>
{
    /// <summary>The collection name shared by the UAT scenario classes.</summary>
    public const string CollectionName = "Todo UAT";
}

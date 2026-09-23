using Brinell.Samples.Todo.Infrastructure;

namespace Brinell.Samples.Todo.UITests;

/// <summary>Three synced todos: Buy milk (open, due 12 March), Book dentist (in progress), File taxes (done).</summary>
public sealed class ThreeTodosFixture : TodoAppFixture
{
    /// <inheritdoc />
    protected override string ScenarioName => "three-todos";
}

/// <summary>No todos, locally or on the server.</summary>
public sealed class EmptyFixture : TodoAppFixture
{
    /// <inheritdoc />
    protected override string ScenarioName => "empty";
}

/// <summary>
/// Local changes the server has not seen: an edit (Buy oat milk) and a todo created offline
/// (Call plumber), beside a synced one (Book dentist).
/// </summary>
/// <remarks>No sync on start: the changes this scenario exists to show would be pushed at once.</remarks>
public sealed class PendingChangesFixture : TodoAppFixture
{
    /// <inheritdoc />
    protected override string ScenarioName => "pending-local-change";

    /// <inheritdoc />
    protected override LaunchSettings AdjustSettings(LaunchSettings settings) => settings with { SyncOnStart = false };
}

/// <summary>
/// Due dates around the pinned 10 March: Renew passport and Pay rent overdue, Water plants done
/// in the past, Plan trip due today.
/// </summary>
public sealed class OverdueFixture : TodoAppFixture
{
    /// <inheritdoc />
    protected override string ScenarioName => "overdue";
}

/// <summary>Three todos on the device, and a backend nothing listens on.</summary>
public sealed class UnreachableServerFixture : TodoAppFixture
{
    /// <inheritdoc />
    protected override string ScenarioName => "three-todos";

    /// <inheritdoc />
    /// <remarks>Port 1: refused at once, so the app learns quickly that nobody is there.</remarks>
    protected override LaunchSettings AdjustSettings(LaunchSettings settings) => settings with { ApiBaseUrl = "http://127.0.0.1:1" };
}

/// <summary>Tests against <see cref="ThreeTodosFixture"/>.</summary>
[CollectionDefinition(Name)]
public sealed class ThreeTodosCollection : ICollectionFixture<ThreeTodosFixture>
{
    /// <summary>The collection's name.</summary>
    public const string Name = "Todo: three todos";
}

/// <summary>Tests against <see cref="EmptyFixture"/>.</summary>
[CollectionDefinition(Name)]
public sealed class EmptyCollection : ICollectionFixture<EmptyFixture>
{
    /// <summary>The collection's name.</summary>
    public const string Name = "Todo: empty";
}

/// <summary>Tests against <see cref="PendingChangesFixture"/>.</summary>
[CollectionDefinition(Name)]
public sealed class PendingChangesCollection : ICollectionFixture<PendingChangesFixture>
{
    /// <summary>The collection's name.</summary>
    public const string Name = "Todo: pending changes";
}

/// <summary>Tests against <see cref="OverdueFixture"/>.</summary>
[CollectionDefinition(Name)]
public sealed class OverdueCollection : ICollectionFixture<OverdueFixture>
{
    /// <summary>The collection's name.</summary>
    public const string Name = "Todo: overdue";
}

/// <summary>Tests against <see cref="UnreachableServerFixture"/>.</summary>
[CollectionDefinition(Name)]
public sealed class UnreachableServerCollection : ICollectionFixture<UnreachableServerFixture>
{
    /// <summary>The collection's name.</summary>
    public const string Name = "Todo: unreachable server";
}

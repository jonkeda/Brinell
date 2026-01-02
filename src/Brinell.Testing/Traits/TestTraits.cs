using Xunit;

namespace Brinell.Testing.Traits;

/// <summary>
/// Test category traits for organizing tests.
/// Use with [Trait("Category", TestCategory.Unit)].
/// </summary>
public static class TestCategory
{
    public const string Unit = "Unit";
    public const string Integration = "Integration";
    public const string UI = "UI";
    public const string Performance = "Performance";
    public const string EndToEnd = "E2E";
}

/// <summary>
/// Test speed traits.
/// Use with [Trait("Speed", TestSpeed.Fast)].
/// </summary>
public static class TestSpeed
{
    public const string Fast = "Fast";
    public const string Slow = "Slow";
    public const string VerySlow = "VerySlow";
}

/// <summary>
/// Test prerequisite traits.
/// Use with [Trait("Requires", TestPrerequisite.Database)].
/// </summary>
public static class TestPrerequisite
{
    public const string Database = "Database";
    public const string Network = "Network";
    public const string FileSystem = "FileSystem";
    public const string ExternalService = "ExternalService";
}

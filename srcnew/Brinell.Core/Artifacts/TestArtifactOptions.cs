namespace Brinell.Core.Artifacts;

public sealed record TestArtifactOptions(
    string RootDirectory,
    string RunId,
    string? SuiteName)
{
    public const string RootDirectoryEnvironmentVariable = "BRINELL_TEST_RESULTS_DIR";
    public const string RunIdEnvironmentVariable = "BRINELL_TEST_RUN_ID";
    public const string SuiteEnvironmentVariable = "BRINELL_TEST_SUITE";
}

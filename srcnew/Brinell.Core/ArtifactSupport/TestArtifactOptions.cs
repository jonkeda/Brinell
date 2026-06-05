namespace Brinell.Core.Artifacts;

public static class TestArtifactOptions
{
    public const string RootDirectoryEnvironmentVariable = "BRINELL_TEST_RESULTS_DIR";
    public const string RunIdEnvironmentVariable = "BRINELL_TEST_RUN_ID";
    public const string SuiteEnvironmentVariable = "BRINELL_TEST_SUITE";

    public const string LegacyRootDirectoryEnvironmentVariable = "BRINELL_ARTIFACT_ROOT";
    public const string LegacyRunIdEnvironmentVariable = "BRINELL_ARTIFACT_RUN_ID";
    public const string LegacySuiteEnvironmentVariable = "BRINELL_ARTIFACT_SUITE";
}

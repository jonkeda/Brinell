namespace Brinell.Core.Artifacts;

public sealed record TestArtifactOptions(
    string RootDirectory,
    string RunId,
    string? SuiteName)
{
    public const string RootDirectoryEnvironmentVariable = "BRINELL_TEST_RESULTS_DIR";
    public const string RunIdEnvironmentVariable = "BRINELL_TEST_RUN_ID";
    public const string SuiteEnvironmentVariable = "BRINELL_TEST_SUITE";

    /// <summary>
    /// The <c>TestResults</c> folder a suite's runs write and the IDEs' preview reads:
    /// <see cref="RootDirectoryEnvironmentVariable"/> when set, else at the nearest folder above
    /// <paramref name="directory"/> with <c>.git</c> or a <c>.sln</c>, else in it.
    /// </summary>
    /// <param name="directory">The suite's project or feature folder.</param>
    public static string RootFor(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        if (Environment.GetEnvironmentVariable(RootDirectoryEnvironmentVariable) is { Length: > 0 } configured)
        {
            return configured;
        }

        for (var folder = new DirectoryInfo(directory); folder is not null; folder = folder.Parent)
        {
            if (Directory.Exists(Path.Combine(folder.FullName, ".git")) || (folder.Exists && folder.EnumerateFiles("*.sln").Any()))
            {
                return Path.Combine(folder.FullName, "TestResults");
            }
        }

        return Path.Combine(directory, "TestResults");
    }
}

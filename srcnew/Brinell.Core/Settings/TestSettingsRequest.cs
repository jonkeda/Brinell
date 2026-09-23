namespace Brinell.Core.Settings;

public sealed record TestSettingsRequest(
    string ProjectDirectory,
    string SettingsRoot = "TestSettings",
    string DefaultFile = "testsettings.json",
    string? LocalFile = "testsettings.local.json",
    string? ScenarioId = null,
    string? ScenarioConvention = "scenarios/{ScenarioId}.json",
    IReadOnlyList<string>? ExplicitFiles = null);

public interface ITestSettingsProvider
{
    TestSettings Resolve(TestSettingsRequest request);
}

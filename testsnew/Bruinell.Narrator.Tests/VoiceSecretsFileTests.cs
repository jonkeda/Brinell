using System.Diagnostics;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Bruinell.Narrator.Tests;

/// <summary>Writing the cloud voice's file, describing it, and where git lets it be saved.</summary>
public sealed class VoiceSecretsFileTests : IDisposable
{
    private const string Key = "secret-key-123";
    private const string Endpoint = "https://voice.openai.azure.com";

    private readonly string _root = Path.Combine(Path.GetTempPath(), "Bruinell", "VoiceSecretsFileTests", Guid.NewGuid().ToString("N"));
    private readonly string _home;

    public VoiceSecretsFileTests()
    {
        _home = Path.Combine(_root, "home");
        Directory.CreateDirectory(_home);
    }

    private string UserFile => Path.Combine(_home, ".bruinell", VoiceSecrets.FileName);

    public void Dispose()
    {
        // git's own files are read-only.
        foreach (var file in Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }

        Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void ASavedFile_IsReadBack()
    {
        VoiceSecrets.Save(UserFile, Endpoint, deployment: null, apiVersion: null, Key, "coral");

        var secrets = VoiceSecrets.Find(null, NoVariables, _home);

        Assert.Equal(Endpoint, secrets?.Endpoint);
        Assert.Equal(VoiceSecrets.DefaultDeployment, secrets?.Deployment);
        Assert.Equal(VoiceSecrets.DefaultApiVersion, secrets?.ApiVersion);
        Assert.Equal(Key, secrets?.Key);
        Assert.Equal("coral", secrets?.Voice);
    }

    [Fact]
    public void AnEmptyKey_KeepsTheSavedOne_AndWithNoneSaved_IsRefused()
    {
        Assert.Throws<InvalidOperationException>(() => VoiceSecrets.Save(UserFile, Endpoint, null, null, key: "", voice: null));

        VoiceSecrets.Save(UserFile, Endpoint, null, null, Key, null);
        VoiceSecrets.Save(UserFile, "https://other.openai.azure.com", null, null, key: " ", voice: "sage");

        var secrets = VoiceSecrets.Find(null, NoVariables, _home);
        Assert.Equal(Key, secrets?.Key);
        Assert.Equal("https://other.openai.azure.com", secrets?.Endpoint);
    }

    [Fact]
    public void TheFile_IsTheCurrentUsersOnly()
    {
        // Access rules are Windows': elsewhere the file is written without them.
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        VoiceSecrets.Save(UserFile, Endpoint, null, null, Key, null);

        var security = new FileInfo(UserFile).GetAccessControl();
        var rule = Assert.Single(security.GetAccessRules(includeExplicit: true, includeInherited: true, typeof(SecurityIdentifier)).Cast<FileSystemAccessRule>());

        Assert.True(security.AreAccessRulesProtected);
        Assert.Equal(WindowsIdentity.GetCurrent().User, rule.IdentityReference);
    }

    [Fact]
    public void TheDescription_SaysWhereEachValueIsFrom_AndHasNoKey()
    {
        VoiceSecrets.Save(UserFile, Endpoint, null, null, Key, "coral");

        var fromFile = VoiceSecrets.Describe(null, NoVariables, _home);
        Assert.True(fromFile.IsSetUp);
        Assert.Equal(UserFile, fromFile.File);
        Assert.Empty(fromFile.FromEnvironment);
        Assert.DoesNotContain(Key, fromFile.ToString(), StringComparison.Ordinal);

        var fromAgent = VoiceSecrets.Describe(null, name => name == VoiceSecrets.KeyVariable ? "agent-key" : null, _home);
        Assert.Equal([VoiceSecrets.KeyVariable], fromAgent.FromEnvironment);

        VoiceSecrets.Delete(UserFile);
        Assert.False(VoiceSecrets.Describe(null, NoVariables, _home).IsSetUp);
        Assert.Null(VoiceSecrets.Describe(null, NoVariables, _home).File);
    }

    [Fact]
    public void AFolderInARepository_IsSafeOnlyOnceGitIgnoresTheFile()
    {
        var repository = Path.Combine(_root, "repo");
        var suite = Path.Combine(repository, "tests", "Suite");
        Directory.CreateDirectory(suite);
        Git(repository, "init -q");

        Assert.Equal(VoiceSecretsIgnore.NotIgnored, VoiceSecretsPlaces.IsIgnored(suite));

        var ignore = VoiceSecretsPlaces.AddIgnoreRule(suite);

        Assert.Equal(Path.Combine(repository, ".gitignore"), ignore, ignoreCase: true);
        Assert.Contains(VoiceSecretsPlaces.IgnoreRule, File.ReadAllText(ignore), StringComparison.Ordinal);
        Assert.Equal(VoiceSecretsIgnore.Ignored, VoiceSecretsPlaces.IsIgnored(suite));
    }

    [Fact]
    public void AFolderInNoRepository_SaysSo()
    {
        Assert.Equal(VoiceSecretsIgnore.NoRepository, VoiceSecretsPlaces.IsIgnored(_home));
    }

    private static string? NoVariables(string name) => null;

    private static void Git(string folder, string arguments)
    {
        using var git = Process.Start(new ProcessStartInfo("git", arguments) { WorkingDirectory = folder, UseShellExecute = false })!;
        git.WaitForExit();
        Assert.Equal(0, git.ExitCode);
    }
}

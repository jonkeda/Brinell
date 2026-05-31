using Brinell.Presenter.Models;
using Brinell.Presenter.Services;
using Brinell.Presenter.ViewModels;

namespace Brinell.Presenter.Uat.Tests.Services;

public sealed class PresenterUserSettingsServiceTests
{
    [Fact]
    public void RecordOpenedFolder_KeepsLastTenAndMovesDuplicatesToTop()
    {
        var root = Path.Combine(Path.GetTempPath(), "BrinellPresenterSettings", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var settingsPath = Path.Combine(root, "settings.json");
            var service = new PresenterUserSettingsService(settingsPath);
            var folders = Enumerable.Range(0, 12)
                .Select(index =>
                {
                    var folder = Path.Combine(root, $"Workspace{index:00}");
                    Directory.CreateDirectory(folder);
                    return folder;
                })
                .ToArray();

            foreach (var folder in folders)
            {
                service.RecordOpenedFolder(folder);
            }

            var settings = service.Load();
            Assert.Equal(10, settings.RecentFolders.Count);
            Assert.Equal(Path.GetFullPath(folders[11]), settings.LastOpenedFolder);
            Assert.Equal(Path.GetFullPath(folders[11]), settings.RecentFolders[0]);
            Assert.DoesNotContain(settings.RecentFolders, path => path.EndsWith("Workspace00", StringComparison.Ordinal));

            service.RecordOpenedFolder(folders[5]);

            settings = service.Load();
            Assert.Equal(10, settings.RecentFolders.Count);
            Assert.Equal(Path.GetFullPath(folders[5]), settings.RecentFolders[0]);
            Assert.Equal(1, settings.RecentFolders.Count(path => path.Equals(Path.GetFullPath(folders[5]), StringComparison.OrdinalIgnoreCase)));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void PresenterShell_LoadsFirstExistingRecentFolderOnStartup()
    {
        var root = Path.Combine(Path.GetTempPath(), "BrinellPresenterStartup", Guid.NewGuid().ToString("N"));
        var settingsPath = Path.Combine(root, "settings.json");
        var missing = Path.Combine(root, "Missing");
        var recent = Path.Combine(root, "RecentWorkspace");
        var fallback = Path.Combine(root, "FallbackWorkspace");
        Directory.CreateDirectory(recent);
        Directory.CreateDirectory(fallback);
        File.WriteAllText(Path.Combine(recent, "uat.config.md"), "# Recent");
        File.WriteAllText(Path.Combine(fallback, "uat.config.md"), "# Fallback");

        try
        {
            var settingsService = new PresenterUserSettingsService(settingsPath);
            settingsService.Save(new PresenterUserSettings
            {
                LastOpenedFolder = missing,
                RecentFolders = [missing, recent, fallback]
            });

            var workspaceService = new FakeWorkspaceService(fallback);
            var viewModel = new PresenterShellViewModel(
                workspaceService,
                new FakeExecutionService(),
                new FakeFolderPickerService(),
                settingsService);

            Assert.Equal(Path.GetFullPath(recent), Path.GetFullPath(viewModel.WorkspacePath!));
            Assert.DoesNotContain(missing, viewModel.RecentFoldersText, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("RecentWorkspace", viewModel.WorkspaceName, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private sealed class FakeFolderPickerService : IFolderPickerService
    {
        public Task<string?> PickFolderAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<string?>(null);
        }
    }

    private sealed class FakeExecutionService : IUatExecutionService
    {
        public Task<PresenterUatExecutionSession> CreateSessionAsync(
            string workspacePath,
            string scenarioFilePath,
            string scenarioName,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeWorkspaceService : IUatWorkspaceService
    {
        private readonly string _defaultWorkspace;

        public FakeWorkspaceService(string defaultWorkspace)
        {
            _defaultWorkspace = defaultWorkspace;
        }

        public string? FindDefaultWorkspace()
        {
            return _defaultWorkspace;
        }

        public UatWorkspaceLoadResult LoadFolder(string folderPath)
        {
            var fullPath = Path.GetFullPath(folderPath);
            return new UatWorkspaceLoadResult(
                fullPath,
                new DirectoryInfo(fullPath).Name,
                new UatWorkspaceConfigLoadResult(
                    true,
                    Path.Combine(fullPath, "uat.config.md"),
                    "MAUI",
                    "Fixture",
                    "App.exe",
                    "App.exe",
                    true,
                    string.Empty,
                    string.Empty,
                    true,
                    [],
                    []),
                [],
                [],
                [],
                "Discovery report:",
                "Command catalog:");
        }
    }
}

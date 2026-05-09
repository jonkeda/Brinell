using System.IO;
using Brinell.Scraper.Models;
using Brinell.Scraper.Services;
using Brinell.Scraper.Tests.TestHelpers;
using Brinell.Scraper.ViewModels;
using Brinell.Scraper.ViewModels.Tabs;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Brinell.Scraper.Tests.ViewModels;

public sealed class CorpusTabViewModelCommandTests : IClassFixture<StaThreadFixture>, IDisposable
{
    private readonly StaThreadFixture _sta;
    private readonly List<string> _dbPaths = [];

    public CorpusTabViewModelCommandTests(StaThreadFixture sta)
    {
        _sta = sta;
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        foreach (var path in _dbPaths)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
                // Best effort cleanup only.
            }
        }
    }

    [Fact]
    public void RefreshPageCommand_CanExecute_DependsOnSelectedPage()
    {
        _sta.Run(() =>
        {
            var (vm, _) = CreateSutWithOnePage();
            var cmd = Assert.IsType<AsyncRelayCommand>(vm.RefreshPageCommand);

            vm.SelectedPage = null;
            Assert.False(cmd.CanExecute(null));

            vm.SelectedPage = vm.Pages[0];
            Assert.True(cmd.CanExecute(null));
        });
    }

    [Fact]
    public void DeletePageCommand_CanExecute_DependsOnSelectedPage()
    {
        _sta.Run(() =>
        {
            var (vm, _) = CreateSutWithOnePage();
            var cmd = Assert.IsType<AsyncRelayCommand>(vm.DeletePageCommand);

            vm.SelectedPage = null;
            Assert.False(cmd.CanExecute(null));

            vm.SelectedPage = vm.Pages[0];
            Assert.True(cmd.CanExecute(null));
        });
    }

    [Fact]
    public void DeleteSnapshotCommand_CanExecute_RequiresSelectedPageAndSnapshotParameter()
    {
        _sta.Run(() =>
        {
            var (vm, _) = CreateSutWithOnePage();
            var cmd = Assert.IsType<AsyncRelayCommand<SnapshotVersionRow>>(vm.DeleteSnapshotCommand);
            var row = vm.Pages[0].Versions[0];

            vm.SelectedPage = null;
            Assert.False(cmd.CanExecute(row));

            vm.SelectedPage = vm.Pages[0];
            Assert.False(cmd.CanExecute(null));
            Assert.True(cmd.CanExecute(row));
        });
    }

    private (CorpusTabViewModel vm, CorpusService service) CreateSutWithOnePage()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"scraper-corpus-cmd-{Guid.NewGuid()}.db");
        _dbPaths.Add(dbPath);

        var service = new CorpusService($"Data Source={dbPath}", NullLogger<CorpusService>.Instance);
        service.StoreSnapshot(777, new DomSnapshot
        {
            SiteName = "Test",
            PageName = "Home",
            PageUrl = "https://a.local/home",
            PageTitle = "Home",
            CapturedAt = DateTimeOffset.UtcNow,
            RootElement = new DomElement
            {
                Tag = "html",
                Children =
                {
                    new DomElement { Tag = "body" }
                }
            }
        });

        var vm = new CorpusTabViewModel(
            service,
            new DomDiffService(),
            NullLogger<CorpusTabViewModel>.Instance);
        vm.Load(777);

        return (vm, service);
    }
}

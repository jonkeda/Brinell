using Brinell.Scraper.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Brinell.Scraper.Tests.ViewModels;

public class BrowserViewModelTests
{
    private readonly BrowserViewModel _sut = new(NullLogger<BrowserViewModel>.Instance);

    [Fact]
    public void OnNavigationStarting_SetsIsLoading()
    {
        _sut.OnNavigationStarting("https://example.com");

        Assert.True(_sut.IsLoading);
    }

    [Fact]
    public void OnNavigationStarting_SetsStatusText()
    {
        _sut.OnNavigationStarting("https://example.com");

        Assert.Contains("https://example.com", _sut.StatusText);
    }

    [Fact]
    public void OnNavigationCompleted_Success_ClearsLoading()
    {
        _sut.OnSourceChanged("https://example.com");
        _sut.OnNavigationStarting("https://example.com");

        _sut.OnNavigationCompleted(true, null);

        Assert.False(_sut.IsLoading);
        Assert.Equal("https://example.com", _sut.StatusText);
    }

    [Fact]
    public void OnNavigationCompleted_Failure_SetsErrorStatus()
    {
        _sut.OnNavigationStarting("https://example.com");

        _sut.OnNavigationCompleted(false, "404 Not Found");

        Assert.False(_sut.IsLoading);
        Assert.Contains("Navigation failed", _sut.StatusText);
        Assert.Contains("404 Not Found", _sut.StatusText);
    }

    [Fact]
    public void OnSourceChanged_UpdatesAddressUrl()
    {
        _sut.OnSourceChanged("https://example.com/page");

        Assert.Equal("https://example.com/page", _sut.AddressUrl);
    }

    [Fact]
    public void OnHistoryChanged_UpdatesCanGoBack()
    {
        _sut.OnHistoryChanged(true, false);

        Assert.True(_sut.CanGoBack);
    }

    [Fact]
    public void OnHistoryChanged_UpdatesCanGoForward()
    {
        _sut.OnHistoryChanged(false, true);

        Assert.True(_sut.CanGoForward);
    }

    [Fact]
    public void NavigateCommand_Disabled_WhenUrlEmpty()
    {
        Assert.False(_sut.NavigateCommand.CanExecute(null));
    }

    [Fact]
    public void NavigateCommand_Enabled_WhenUrlSet()
    {
        _sut.OnSourceChanged("https://example.com");

        Assert.True(_sut.NavigateCommand.CanExecute(null));
    }

    [Fact]
    public void NavigateCommand_FiresNavigateRequested()
    {
        string? receivedUrl = null;
        _sut.NavigateRequested += url => receivedUrl = url;
        _sut.OnSourceChanged("https://example.com");

        _sut.NavigateCommand.Execute(null);

        Assert.Equal("https://example.com", receivedUrl);
    }
}

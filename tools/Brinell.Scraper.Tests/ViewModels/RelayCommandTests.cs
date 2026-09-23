using Brinell.Scraper.ViewModels;
using Xunit;

namespace Brinell.Scraper.Tests.ViewModels;

public class RelayCommandTests
{
    [Fact]
    public void Execute_CallsAction()
    {
        var called = false;
        var command = new RelayCommand(() => called = true);

        command.Execute(null);

        Assert.True(called);
    }

    [Fact]
    public void CanExecute_ReturnsTrue_WhenNoPredicate()
    {
        var command = new RelayCommand(() => { });

        Assert.True(command.CanExecute(null));
    }

    [Fact]
    public void CanExecute_ReturnsFalse_WhenPredicateFails()
    {
        var command = new RelayCommand(() => { }, () => false);

        Assert.False(command.CanExecute(null));
    }

    [Fact]
    public void CanExecute_ReturnsTrue_WhenPredicatePasses()
    {
        var command = new RelayCommand(() => { }, () => true);

        Assert.True(command.CanExecute(null));
    }

    [Fact]
    public void RaiseCanExecuteChanged_FiresEvent()
    {
        var command = new RelayCommand(() => { });
        var fired = false;
        command.CanExecuteChanged += (_, _) => fired = true;

        command.RaiseCanExecuteChanged();

        Assert.True(fired);
    }

    [Fact]
    public void Execute_DoesNotThrow_WhenCanExecuteFalse()
    {
        var called = false;
        var command = new RelayCommand(() => called = true, () => false);

        command.Execute(null);

        Assert.True(called);
    }

    [Fact]
    public void RelayCommandT_PassesParameter()
    {
        string? received = null;
        var command = new RelayCommand<string>(p => received = p);

        command.Execute("hello");

        Assert.Equal("hello", received);
    }

    [Fact]
    public void RelayCommandT_CanExecute_ReceivesParameter()
    {
        string? received = null;
        var command = new RelayCommand<string>(_ => { }, p => { received = p; return true; });

        command.CanExecute("test");

        Assert.Equal("test", received);
    }
}

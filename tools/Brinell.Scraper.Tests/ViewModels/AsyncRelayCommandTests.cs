using Brinell.Scraper.ViewModels;
using Xunit;

namespace Brinell.Scraper.Tests.ViewModels;

public class AsyncRelayCommandTests
{
    [Fact]
    public async Task Execute_CallsAsyncAction()
    {
        var called = false;
        var cmd = new AsyncRelayCommand(async ct =>
        {
            called = true;
            await Task.CompletedTask;
        });

        cmd.Execute(null);
        await Task.Yield();

        Assert.True(called);
    }

    [Fact]
    public async Task IsRunning_TrueWhileExecuting()
    {
        var tcs = new TaskCompletionSource();
        var cmd = new AsyncRelayCommand(async ct => { await tcs.Task; });

        cmd.Execute(null);
        await Task.Yield();

        Assert.True(cmd.IsRunning);

        tcs.SetResult();
        await Task.Yield();
    }

    [Fact]
    public async Task IsRunning_FalseAfterCompletion()
    {
        var tcs = new TaskCompletionSource();
        var cmd = new AsyncRelayCommand(async ct => { await tcs.Task; });

        cmd.Execute(null);
        await Task.Yield();

        tcs.SetResult();
        await Task.Delay(50);

        Assert.False(cmd.IsRunning);
    }

    [Fact]
    public async Task CanExecute_ReturnsFalse_WhileRunning()
    {
        var tcs = new TaskCompletionSource();
        var cmd = new AsyncRelayCommand(async ct => { await tcs.Task; });

        Assert.True(cmd.CanExecute(null));

        cmd.Execute(null);
        await Task.Yield();

        Assert.False(cmd.CanExecute(null));

        tcs.SetResult();
        await Task.Delay(50);

        Assert.True(cmd.CanExecute(null));
    }

    [Fact]
    public async Task Execute_HandlesException()
    {
        var tcs = new TaskCompletionSource();
        var cmd = new AsyncRelayCommand(async ct =>
        {
            await tcs.Task;
            throw new InvalidOperationException("test error");
        });

        cmd.Execute(null);
        await Task.Yield();

        Assert.True(cmd.IsRunning);

        tcs.SetResult();
        await Task.Delay(50);

        Assert.False(cmd.IsRunning);
    }

    [Fact]
    public async Task CancellationToken_Propagated()
    {
        var tokenCancelled = false;
        var tcs = new TaskCompletionSource();
        var cmd = new AsyncRelayCommand(async ct =>
        {
            ct.Register(() => tokenCancelled = true);
            await tcs.Task;
        });

        cmd.Execute(null);
        await Task.Yield();

        cmd.Cancel();

        Assert.True(tokenCancelled);

        tcs.SetResult();
        await Task.Delay(50);
    }

    [Fact]
    public async Task AsyncRelayCommandT_PassesParameter()
    {
        int? receivedValue = null;
        var cmd = new AsyncRelayCommand<int>(async (value, ct) =>
        {
            receivedValue = value;
            await Task.CompletedTask;
        });

        cmd.Execute(42);
        await Task.Yield();

        Assert.Equal(42, receivedValue);
    }
}

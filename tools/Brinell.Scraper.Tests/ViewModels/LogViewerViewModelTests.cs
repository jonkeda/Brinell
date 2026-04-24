using Brinell.Scraper.Logging;
using Brinell.Scraper.Models;
using Brinell.Scraper.ViewModels;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Brinell.Scraper.Tests.ViewModels;

public sealed class LogViewerViewModelTests
{
    /// <summary>
    /// Runs an action on an STA thread (required for CollectionViewSource / WPF binding).
    /// </summary>
    private static void RunOnSta(Action action)
    {
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { exception = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (exception is not null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception).Throw();
    }

    private static (LogViewerViewModel vm, InAppLogService service) CreateVm()
    {
        var service = new InAppLogService();
        var vm = new LogViewerViewModel(service);
        return (vm, service);
    }

    [Fact]
    public void FilteredLogEntries_ShowsAll_AtDebugLevel()
    {
        RunOnSta(() =>
        {
            var (vm, service) = CreateVm();
            service.Entries.Add(new LogEntry(DateTime.UtcNow, LogLevel.Debug, "A", "debug"));
            service.Entries.Add(new LogEntry(DateTime.UtcNow, LogLevel.Information, "B", "info"));
            service.Entries.Add(new LogEntry(DateTime.UtcNow, LogLevel.Error, "C", "error"));

            vm.SelectedLogLevel = LogLevel.Debug;
            vm.FilteredLogEntries.Refresh();

            var visible = vm.FilteredLogEntries.Cast<LogEntry>().ToList();
            Assert.Equal(3, visible.Count);
        });
    }

    [Fact]
    public void FilteredLogEntries_HidesDebug_AtInfoLevel()
    {
        RunOnSta(() =>
        {
            var (vm, service) = CreateVm();
            service.Entries.Add(new LogEntry(DateTime.UtcNow, LogLevel.Debug, "A", "debug"));
            service.Entries.Add(new LogEntry(DateTime.UtcNow, LogLevel.Information, "B", "info"));
            service.Entries.Add(new LogEntry(DateTime.UtcNow, LogLevel.Warning, "C", "warn"));

            vm.SelectedLogLevel = LogLevel.Information;

            var visible = vm.FilteredLogEntries.Cast<LogEntry>().ToList();
            Assert.Equal(2, visible.Count);
            Assert.DoesNotContain(visible, e => e.Level == LogLevel.Debug);
        });
    }

    [Fact]
    public void FilteredLogEntries_ShowsErrorOnly_AtErrorLevel()
    {
        RunOnSta(() =>
        {
            var (vm, service) = CreateVm();
            service.Entries.Add(new LogEntry(DateTime.UtcNow, LogLevel.Debug, "A", "debug"));
            service.Entries.Add(new LogEntry(DateTime.UtcNow, LogLevel.Information, "B", "info"));
            service.Entries.Add(new LogEntry(DateTime.UtcNow, LogLevel.Warning, "C", "warn"));
            service.Entries.Add(new LogEntry(DateTime.UtcNow, LogLevel.Error, "D", "error"));

            vm.SelectedLogLevel = LogLevel.Error;

            var visible = vm.FilteredLogEntries.Cast<LogEntry>().ToList();
            Assert.Single(visible);
            Assert.Equal(LogLevel.Error, visible[0].Level);
        });
    }

    [Fact]
    public void SelectedLogLevel_Change_RefreshesFilter()
    {
        RunOnSta(() =>
        {
            var (vm, service) = CreateVm();
            service.Entries.Add(new LogEntry(DateTime.UtcNow, LogLevel.Debug, "A", "debug"));
            service.Entries.Add(new LogEntry(DateTime.UtcNow, LogLevel.Error, "B", "error"));

            // Start at Debug — all visible.
            vm.SelectedLogLevel = LogLevel.Debug;
            Assert.Equal(2, vm.FilteredLogEntries.Cast<LogEntry>().Count());

            // Switch to Error — only one visible.
            vm.SelectedLogLevel = LogLevel.Error;
            Assert.Single(vm.FilteredLogEntries.Cast<LogEntry>());
        });
    }

    [Fact]
    public void ClearLogsCommand_CanExecute()
    {
        RunOnSta(() =>
        {
            var (vm, service) = CreateVm();
            service.Entries.Add(new LogEntry(DateTime.UtcNow, LogLevel.Information, "A", "msg"));

            // ClearLogsCommand delegates to InAppLogService.Clear(), which requires
            // Application.Current dispatcher. Verify the command is always executable.
            Assert.True(vm.ClearLogsCommand.CanExecute(null));

            // Verify it does not throw when invoked without a WPF Application.
            var ex = Record.Exception(() => vm.ClearLogsCommand.Execute(null));
            Assert.Null(ex);
        });
    }

    [Fact]
    public void IsAutoScroll_DefaultTrue()
    {
        RunOnSta(() =>
        {
            var (vm, _) = CreateVm();

            Assert.True(vm.IsAutoScroll);
        });
    }

    [Fact]
    public void IsAutoScroll_RaisesPropertyChanged()
    {
        RunOnSta(() =>
        {
            var (vm, _) = CreateVm();
            string? raised = null;
            vm.PropertyChanged += (_, e) => raised = e.PropertyName;

            vm.IsAutoScroll = false;

            Assert.Equal(nameof(vm.IsAutoScroll), raised);
        });
    }

    [Fact]
    public void SelectedLogLevel_DefaultsToDebug()
    {
        RunOnSta(() =>
        {
            var (vm, _) = CreateVm();

            Assert.Equal(LogLevel.Debug, vm.SelectedLogLevel);
        });
    }

    [Fact]
    public void SelectedLogLevel_RaisesPropertyChanged()
    {
        RunOnSta(() =>
        {
            var (vm, _) = CreateVm();
            string? raised = null;
            vm.PropertyChanged += (_, e) => raised = e.PropertyName;

            vm.SelectedLogLevel = LogLevel.Warning;

            Assert.Equal(nameof(vm.SelectedLogLevel), raised);
        });
    }

    [Fact]
    public void LogLevels_ContainsExpectedValues()
    {
        RunOnSta(() =>
        {
            var (vm, _) = CreateVm();

            Assert.Contains(LogLevel.Trace, vm.LogLevels);
            Assert.Contains(LogLevel.Debug, vm.LogLevels);
            Assert.Contains(LogLevel.Information, vm.LogLevels);
            Assert.Contains(LogLevel.Warning, vm.LogLevels);
            Assert.Contains(LogLevel.Error, vm.LogLevels);
        });
    }

    [Fact]
    public void RawEntries_ReturnsSameCollectionAsService()
    {
        RunOnSta(() =>
        {
            var (vm, service) = CreateVm();

            Assert.Same(service.Entries, vm.RawEntries);
        });
    }
}

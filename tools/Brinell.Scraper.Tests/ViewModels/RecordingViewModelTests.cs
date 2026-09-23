using Brinell.Scraper.Models;
using Brinell.Scraper.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Brinell.Scraper.Tests.ViewModels;

public class RecordingViewModelTests
{
    private static RecordingViewModel CreateViewModel() =>
        new(NullLogger<RecordingViewModel>.Instance);

    private static DomSnapshot CreateSnapshot(string url, string title = "Page") => new()
    {
        PageUrl = url,
        PageTitle = title,
        CapturedAt = DateTimeOffset.UtcNow,
        RootElement = new DomElement { Tag = "html" }
    };

    [Fact]
    public void StartRecording_SetsIsRecordingTrue()
    {
        var vm = CreateViewModel();
        vm.StartRecording();
        Assert.True(vm.IsRecording);
    }

    [Fact]
    public void StopRecording_SetsIsRecordingFalse()
    {
        var vm = CreateViewModel();
        vm.StartRecording();
        vm.StopRecording();
        Assert.False(vm.IsRecording);
    }

    [Fact]
    public void PauseRecording_SetsIsPausedTrue()
    {
        var vm = CreateViewModel();
        vm.StartRecording();
        vm.PauseRecording();
        Assert.True(vm.IsPaused);
        Assert.True(vm.IsRecording);
    }

    [Fact]
    public void OnPageTransition_CapturesSnapshot()
    {
        var vm = CreateViewModel();
        vm.StartRecording();
        var snapshot = CreateSnapshot("https://example.com/page1");
        var captured = vm.OnPageTransition("https://example.com/page1", snapshot);
        Assert.True(captured);
        Assert.Single(vm.SessionSnapshots);
    }

    [Fact]
    public void OnPageTransition_SkipsDuplicateWithin2Seconds()
    {
        var vm = CreateViewModel();
        vm.StartRecording();
        var snapshot1 = CreateSnapshot("https://example.com/page1");
        var snapshot2 = CreateSnapshot("https://example.com/page1");
        vm.OnPageTransition("https://example.com/page1", snapshot1);
        var captured = vm.OnPageTransition("https://example.com/page1", snapshot2);
        Assert.False(captured);
        Assert.Single(vm.SessionSnapshots);
    }

    [Fact]
    public void SessionSnapshots_TracksNewPages()
    {
        var vm = CreateViewModel();
        vm.StartRecording();
        vm.OnPageTransition("https://example.com/page1", CreateSnapshot("https://example.com/page1"));
        vm.OnPageTransition("https://example.com/page2", CreateSnapshot("https://example.com/page2"));
        vm.OnPageTransition("https://example.com/page3", CreateSnapshot("https://example.com/page3"));
        Assert.Equal(3, vm.SessionSnapshots.Count);
    }

    [Fact]
    public void StopRecording_FiresAnalyzePrompt()
    {
        var vm = CreateViewModel();
        var fired = false;
        vm.AnalyzePromptRequested += () => fired = true;
        vm.StartRecording();
        vm.StopRecording();
        Assert.True(fired);
    }
}

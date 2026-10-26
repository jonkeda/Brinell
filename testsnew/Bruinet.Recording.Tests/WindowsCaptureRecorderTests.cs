using System.Drawing;
using System.Windows.Forms;
using Bruinet.Core.Recording;
using Bruinet.Recording.Windows;
using Xunit;

namespace Bruinet.Recording.Tests;

public sealed class WindowsCaptureRecorderTests
{
    [Fact]
    public async Task A_covered_window_records_as_it_looks()
    {
        using var windows = new TestWindows();
        var sink = new CountingSink();
        var recorder = new WindowsCaptureRecorder((_, _, _) => sink);
        var target = new RecordingTarget.Window(windows.RecordedHandle);

        Assert.True(recorder.CanRecord(target));
        await using var recording = await recorder.StartAsync(target, new RecordingOptions("video.mp4", FramesPerSecond: 10));
        await Task.Delay(500);
        var result = await recording.StopAsync();

        Assert.True(sink.Frames >= 3, $"{sink.Frames} frames");
        Assert.Equal(result.StartedAt, recording.StartedAt);
        var (blue, green, red) = Middle(sink.Last, result.Size);
        Assert.True(red > 200 && green < 60 && blue < 60, $"The middle is B{blue} G{green} R{red}, not the covered window's red.");
    }

    [Fact]
    public void A_region_is_for_the_region_recorder()
    {
        Assert.False(new WindowsCaptureRecorder().CanRecord(new RecordingTarget.Monitor(0, new Rectangle(0, 0, 10, 10))));
        Assert.False(new WindowsCaptureRecorder().CanRecord(new RecordingTarget.AndroidDevice("emulator-5554")));
    }

    [FfmpegFact]
    public async Task A_window_records_to_an_mp4()
    {
        using var windows = new TestWindows();
        var path = Path.Combine(Path.GetTempPath(), $"bruinet-window-{Guid.NewGuid():N}.mp4");
        try
        {
            await using var recording = await new WindowsCaptureRecorder().StartAsync(new RecordingTarget.Window(windows.RecordedHandle), new RecordingOptions(path));
            await Task.Delay(2000);
            var result = await recording.StopAsync();

            Assert.True(new FileInfo(path).Length > 0);
            Assert.InRange(result.Duration.TotalSeconds, 1.8, 2.5);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [FfmpegFact]
    public async Task A_region_records_to_an_mp4()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bruinet-region-{Guid.NewGuid():N}.mp4");
        try
        {
            await using var recording = await new FfmpegRegionRecorder().StartAsync(new RecordingTarget.Monitor(0, new Rectangle(0, 0, 320, 240)), new RecordingOptions(path));
            await Task.Delay(2000);
            var result = await recording.StopAsync();

            Assert.True(new FileInfo(path).Length > 0);
            Assert.InRange(result.Duration.TotalSeconds, 1.8, 2.5);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static (byte Blue, byte Green, byte Red) Middle(byte[] bgra, Size size)
    {
        var offset = ((size.Height / 2 * size.Width) + (size.Width / 2)) * 4;
        return (bgra[offset], bgra[offset + 1], bgra[offset + 2]);
    }

    // A red window, and a blue always-on-top window over all of it, on their own UI thread.
    private sealed class TestWindows : IDisposable
    {
        private readonly Thread _thread;
        private readonly ManualResetEventSlim _shown = new();
        private Form? _recorded;

        public TestWindows()
        {
            _thread = new Thread(Run) { IsBackground = true };
            _thread.SetApartmentState(ApartmentState.STA);
            _thread.Start();
            Assert.True(_shown.Wait(TimeSpan.FromSeconds(10)), "The test windows did not open.");
        }

        public nint RecordedHandle { get; private set; }

        public void Dispose()
        {
            _recorded?.BeginInvoke(Application.ExitThread);
            _thread.Join(TimeSpan.FromSeconds(5));
            _shown.Dispose();
        }

        private void Run()
        {
            _recorded = new Form { Text = "Bruinet recorded", BackColor = Color.Red, StartPosition = FormStartPosition.Manual, Bounds = new Rectangle(100, 100, 400, 300), ShowInTaskbar = false };
            var cover = new Form { Text = "Bruinet cover", BackColor = Color.Blue, StartPosition = FormStartPosition.Manual, Bounds = new Rectangle(50, 50, 500, 400), TopMost = true, ShowInTaskbar = false };
            _recorded.Shown += (_, _) =>
            {
                cover.Show();
                RecordedHandle = _recorded.Handle;
                _shown.Set();
            };
            Application.Run(_recorded);
            cover.Dispose();
        }
    }
}

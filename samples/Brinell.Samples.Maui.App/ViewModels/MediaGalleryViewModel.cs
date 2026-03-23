using System.Collections.ObjectModel;
using Brinell.Samples.Maui.App.Models;
using Brinell.Samples.Shared.Commands;
using Brinell.Samples.Shared.ViewModels;

namespace Brinell.Samples.Maui.App.ViewModels;

/// <summary>
/// ViewModel for the MediaGallery page demonstrating image, media, and web view controls.
/// </summary>
public class MediaGalleryViewModel : ParentViewModel
{
    private MediaItem? _selectedMedia;
    private string _currentUrl = "https://dotnet.microsoft.com";
    private string _webViewTitle = "Microsoft .NET";
    private bool _isWebLoading = true;
    private bool _isPlaying;
    private bool _isMuted;
    private double _volume = 80;
    private double _position;
    private double _duration = 180;
    private string _positionText = "0:00";
    private string _durationText = "3:00";

    public MediaItem? SelectedMedia
    {
        get => _selectedMedia;
        set => SetProperty(ref _selectedMedia, value);
    }

    public string CurrentUrl
    {
        get => _currentUrl;
        set => SetProperty(ref _currentUrl, value);
    }

    public string WebViewTitle
    {
        get => _webViewTitle;
        set => SetProperty(ref _webViewTitle, value);
    }

    public bool IsWebLoading
    {
        get => _isWebLoading;
        set => SetProperty(ref _isWebLoading, value);
    }

    public bool IsPlaying
    {
        get => _isPlaying;
        set => SetProperty(ref _isPlaying, value);
    }

    public bool IsMuted
    {
        get => _isMuted;
        set => SetProperty(ref _isMuted, value);
    }

    public double Volume
    {
        get => _volume;
        set => SetProperty(ref _volume, value);
    }

    public double Position
    {
        get => _position;
        set
        {
            if (SetProperty(ref _position, value))
            {
                var ts = TimeSpan.FromSeconds(value);
                PositionText = $"{(int)ts.TotalMinutes}:{ts.Seconds:D2}";
            }
        }
    }

    public double Duration
    {
        get => _duration;
        set
        {
            if (SetProperty(ref _duration, value))
            {
                var ts = TimeSpan.FromSeconds(value);
                DurationText = $"{(int)ts.TotalMinutes}:{ts.Seconds:D2}";
            }
        }
    }

    public string PositionText
    {
        get => _positionText;
        set => SetProperty(ref _positionText, value);
    }

    public string DurationText
    {
        get => _durationText;
        set => SetProperty(ref _durationText, value);
    }

    public ObservableCollection<MediaItem> Thumbnails { get; } = new();
    public ObservableCollection<MediaItem> MediaItems { get; } = new();

    public IAsyncRelayCommand PlayCommand { get; }
    public IAsyncRelayCommand PauseCommand { get; }
    public IAsyncRelayCommand StopCommand { get; }
    public IAsyncRelayCommand WebBackCommand { get; }
    public IAsyncRelayCommand WebForwardCommand { get; }
    public IAsyncRelayCommand WebReloadCommand { get; }
    public IAsyncRelayCommand NavigateCommand { get; }
    public IAsyncRelayCommand<MediaItem> SelectThumbnailCommand { get; }

    public MediaGalleryViewModel()
    {
        PlayCommand = new AsyncRelayCommand(this, PlayAsync);
        PauseCommand = new AsyncRelayCommand(this, PauseAsync);
        StopCommand = new AsyncRelayCommand(this, StopAsync);
        WebBackCommand = new AsyncRelayCommand(this, WebBackAsync);
        WebForwardCommand = new AsyncRelayCommand(this, WebForwardAsync);
        WebReloadCommand = new AsyncRelayCommand(this, WebReloadAsync);
        NavigateCommand = new AsyncRelayCommand(this, NavigateAsync);
        SelectThumbnailCommand = new AsyncRelayCommand<MediaItem>(this, SelectThumbnailAsync);

        LoadSampleData();
    }

    private void LoadSampleData()
    {
        Thumbnails.Clear();
        for (int i = 1; i <= 4; i++)
        {
            Thumbnails.Add(new MediaItem
            {
                Id = i,
                Title = $"Image {i}",
                ThumbnailUrl = $"https://picsum.photos/100/100?random={i}",
                FullUrl = $"https://picsum.photos/800/600?random={i}",
                Type = MediaType.Image
            });
        }

        MediaItems.Add(new MediaItem
        {
            Id = 100,
            Title = "Sample Video",
            Description = "A sample video for testing media controls",
            Type = MediaType.Video,
            Duration = TimeSpan.FromMinutes(3)
        });

        if (Thumbnails.Count > 0)
            SelectedMedia = Thumbnails[0];
    }

    private Task SelectThumbnailAsync(MediaItem? item)
    {
        if (item != null)
            SelectedMedia = item;
        return Task.CompletedTask;
    }

    private async Task PlayAsync()
    {
        IsPlaying = true;
        await Task.CompletedTask;
    }

    private async Task PauseAsync()
    {
        IsPlaying = false;
        await Task.CompletedTask;
    }

    private async Task StopAsync()
    {
        IsPlaying = false;
        Position = 0;
        await Task.CompletedTask;
    }

    private async Task WebBackAsync()
    {
        await Task.CompletedTask;
    }

    private async Task WebForwardAsync()
    {
        await Task.CompletedTask;
    }

    private async Task WebReloadAsync()
    {
        IsWebLoading = true;
        await Task.Delay(500);
        IsWebLoading = false;
    }

    private async Task NavigateAsync()
    {
        IsWebLoading = true;
        await Task.Delay(500);
        IsWebLoading = false;
    }
}

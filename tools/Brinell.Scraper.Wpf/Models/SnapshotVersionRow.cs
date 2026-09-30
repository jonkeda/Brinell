using Brinell.Scraper.ViewModels;

namespace Brinell.Scraper.Models;

public sealed class SnapshotVersionRow : ViewModelBase
{
    private long _snapshotId;
    private int _versionNumber;
    private bool _isLatest;
    private DateTime _capturedAt;
    private int _elementCount;
    private long _snapshotSizeBytes;
    private bool _hasPageObject;
    private PageObjectStatus _pageObjectStatus = PageObjectStatus.NotGenerated;

    public long SnapshotId
    {
        get => _snapshotId;
        set => SetProperty(ref _snapshotId, value);
    }

    public int VersionNumber
    {
        get => _versionNumber;
        set
        {
            if (SetProperty(ref _versionNumber, value))
                OnPropertyChanged(nameof(VersionLabel));
        }
    }

    public bool IsLatest
    {
        get => _isLatest;
        set
        {
            if (SetProperty(ref _isLatest, value))
                OnPropertyChanged(nameof(VersionLabel));
        }
    }

    public DateTime CapturedAt
    {
        get => _capturedAt;
        set => SetProperty(ref _capturedAt, value);
    }

    public int ElementCount
    {
        get => _elementCount;
        set => SetProperty(ref _elementCount, value);
    }

    public long SnapshotSizeBytes
    {
        get => _snapshotSizeBytes;
        set
        {
            if (SetProperty(ref _snapshotSizeBytes, value))
                OnPropertyChanged(nameof(SizeLabel));
        }
    }

    public bool HasPageObject
    {
        get => _hasPageObject;
        set
        {
            if (SetProperty(ref _hasPageObject, value))
            {
                OnPropertyChanged(nameof(PageObjectIcon));
                OnPropertyChanged(nameof(PageObjectLabel));
            }
        }
    }

    public PageObjectStatus PageObjectStatus
    {
        get => _pageObjectStatus;
        set
        {
            if (SetProperty(ref _pageObjectStatus, value))
            {
                OnPropertyChanged(nameof(PageObjectIcon));
                OnPropertyChanged(nameof(PageObjectLabel));
            }
        }
    }

    public string VersionLabel => _isLatest ? $"v{_versionNumber} (latest)" : $"v{_versionNumber}";

    public string SizeLabel => _snapshotSizeBytes switch
    {
        < 1024 => $"{_snapshotSizeBytes} B",
        < 1024 * 1024 => $"{_snapshotSizeBytes / 1024.0:F1} KB",
        _ => $"{_snapshotSizeBytes / (1024.0 * 1024.0):F2} MB",
    };

    public string PageObjectIcon => _pageObjectStatus switch
    {
        PageObjectStatus.Generated => "✓",
        PageObjectStatus.Error => "✗",
        _ => "—",
    };

    public string PageObjectLabel => _pageObjectStatus switch
    {
        PageObjectStatus.Generated => "Generated",
        PageObjectStatus.Error => "Error",
        _ => "Not generated",
    };
}

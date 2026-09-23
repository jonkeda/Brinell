namespace Brinell.Samples.Blazor.App.Models;

/// <summary>
/// Represents a user profile for form demonstration.
/// </summary>
public class UserProfile
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public DateTime? BirthDate { get; set; }
    public string Country { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public bool SubscribeNewsletter { get; set; }
    public bool AcceptTerms { get; set; }
    public bool AcceptPrivacy { get; set; }
    public string SubscriptionTier { get; set; } = "Basic";
    public string ContactPreference { get; set; } = "Email";
    public int FontSize { get; set; } = 14;
    public int Volume { get; set; } = 50;
    public int Quantity { get; set; } = 1;
}

/// <summary>
/// Represents a data item for collections and tables.
/// </summary>
public class DataItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
    public bool IsSelected { get; set; }
    public bool IsStarred { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

/// <summary>
/// Represents a media item for gallery demonstration.
/// </summary>
public class MediaItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string FullUrl { get; set; } = string.Empty;
    public MediaType Type { get; set; } = MediaType.Image;
    public TimeSpan Duration { get; set; }
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.Now;
}

public enum MediaType
{
    Image,
    Video,
    Audio,
    Document
}

/// <summary>
/// Represents an uploaded file.
/// </summary>
public class UploadedFile
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.Now;
    public int UploadProgress { get; set; }
    public UploadStatus Status { get; set; } = UploadStatus.Pending;
}

public enum UploadStatus
{
    Pending,
    Uploading,
    Completed,
    Failed
}

/// <summary>
/// Represents a validation result.
/// </summary>
public class ValidationModel
{
    public string RequiredField { get; set; } = string.Empty;
    public string EmailField { get; set; } = string.Empty;
    public string PhoneField { get; set; } = string.Empty;
    public string MinLengthField { get; set; } = string.Empty;
    public string MaxLengthField { get; set; } = string.Empty;
    public int? RangeField { get; set; }
    public string RegexField { get; set; } = string.Empty;
    public string PasswordField { get; set; } = string.Empty;
    public string ConfirmPasswordField { get; set; } = string.Empty;
}

/// <summary>
/// Represents a navigation item.
/// </summary>
public class NavItem
{
    public string Title { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public bool IsExpanded { get; set; }
    public List<NavItem> Children { get; set; } = new();
}

/// <summary>
/// Represents a tab item.
/// </summary>
public class TabItem
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

/// <summary>
/// Represents a toast notification.
/// </summary>
public class ToastMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public ToastType Type { get; set; } = ToastType.Info;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int Duration { get; set; } = 5000;
}

public enum ToastType
{
    Info,
    Success,
    Warning,
    Error
}

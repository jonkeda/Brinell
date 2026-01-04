namespace Brinell.Samples.Maui.App.Models;

/// <summary>
/// Represents a user profile for form demonstrations.
/// </summary>
public class UserProfile
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; } = DateTime.Today.AddYears(-25);
    public TimeSpan PreferredTime { get; set; } = new TimeSpan(9, 0, 0);
    public string Country { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public bool SubscribeNewsletter { get; set; }
    public bool AcceptTerms { get; set; }
    public bool AcceptPrivacy { get; set; }
    public string SubscriptionTier { get; set; } = "Basic";
    public string ContactPreference { get; set; } = "Email";
    public double FontSize { get; set; } = 14;
    public double Volume { get; set; } = 50;
    public int Quantity { get; set; } = 1;
}

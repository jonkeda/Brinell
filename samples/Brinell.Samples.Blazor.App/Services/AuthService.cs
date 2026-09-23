namespace Brinell.Samples.Blazor.App.Services;

/// <summary>
/// Authentication result.
/// </summary>
public record AuthResult(bool Success, string? ErrorMessage = null);

/// <summary>
/// Interface for authentication service.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Attempts to authenticate a user with the provided credentials.
    /// </summary>
    /// <param name="email">The user's email address.</param>
    /// <param name="password">The user's password.</param>
    /// <returns>The authentication result.</returns>
    Task<AuthResult> LoginAsync(string email, string password);

    /// <summary>
    /// Logs out the current user.
    /// </summary>
    Task LogoutAsync();

    /// <summary>
    /// Gets whether a user is currently authenticated.
    /// </summary>
    bool IsAuthenticated { get; }
}

/// <summary>
/// Mock authentication service for demonstration purposes.
/// </summary>
public class AuthService : IAuthService
{
    private const string ValidEmail = "test@example.com";
    private const string ValidPassword = "password123";
    private const int SimulatedDelayMs = 1500;

    /// <inheritdoc />
    public bool IsAuthenticated { get; private set; }

    /// <inheritdoc />
    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        // Simulate network delay
        await Task.Delay(SimulatedDelayMs);

        // Check credentials
        if (email.Equals(ValidEmail, StringComparison.OrdinalIgnoreCase) &&
            password == ValidPassword)
        {
            IsAuthenticated = true;
            return new AuthResult(true);
        }

        return new AuthResult(false, "Invalid email or password");
    }

    /// <inheritdoc />
    public Task LogoutAsync()
    {
        IsAuthenticated = false;
        return Task.CompletedTask;
    }
}

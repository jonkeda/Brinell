using Brinell.Samples.Todo.Core.Models;
using Brinell.Samples.Todo.Core.Rules;
using Brinell.Samples.Todo.Core.Sync;

namespace Brinell.Samples.Todo.Core.Services;

/// <summary>
/// The device's copy of the server's users. SQLite in the app; an in-memory fake in unit tests.
/// </summary>
public interface IUserRepository
{
    /// <summary>Every cached user.</summary>
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>One cached user, or <c>null</c>.</summary>
    Task<User?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Inserts or replaces one user, after the server has accepted it.</summary>
    Task SaveAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>Replaces the whole cache with <paramref name="users"/>, in one transaction.</summary>
    Task ReplaceAllAsync(IReadOnlyList<User> users, CancellationToken cancellationToken = default);
}

/// <summary>
/// The server's users. Implemented over HTTP in Infrastructure.
/// </summary>
/// <remarks>
/// A failure to reach or understand the server is a <see cref="TodoApiException"/>; a user the
/// server refuses is a <see cref="UserRejectedException"/>.
/// </remarks>
public interface IUserApi
{
    /// <summary>Every user on the server.</summary>
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Stores <paramref name="user"/> and returns what the server kept.</summary>
    Task<User> PutAsync(User user, CancellationToken cancellationToken = default);
}

/// <summary>The server refused a user; <see cref="Errors"/> says why, per field.</summary>
public sealed class UserRejectedException(IReadOnlyList<UserFieldError> errors)
    : Exception("The server refused the user: " + string.Join("; ", errors.Select(error => $"{error.Field}: {error.Message}")))
{
    /// <summary>The problems, by field.</summary>
    public IReadOnlyList<UserFieldError> Errors { get; } = errors;
}

/// <summary>What happened to a save.</summary>
/// <param name="Saved">The user as the server kept it, or <c>null</c> when nothing was saved.</param>
/// <param name="Errors">The fields that were refused, by the app or by the server.</param>
/// <param name="Failure">Why the server could not be asked, or <c>null</c>.</param>
public sealed record UserSaveResult(User? Saved, IReadOnlyList<UserFieldError> Errors, string? Failure)
{
    /// <summary>Whether the user was saved.</summary>
    public bool Succeeded => Saved is not null;
}

/// <summary>
/// What the user can do to users. Changes go straight to the server and are cached after it
/// accepts them; reading works offline, from the cache.
/// </summary>
public sealed class UserService(IUserRepository cache, IUserApi api, IConnectivityState connectivity, TimeProvider time)
{
    /// <summary>Shown when a change is attempted offline.</summary>
    public const string OfflineMessage = "You are offline. Users can be viewed, but not changed.";

    /// <summary>Today, in the device's time zone.</summary>
    public DateOnly Today => DateOnly.FromDateTime(time.GetLocalNow().DateTime);

    /// <summary>Whether a change can be sent now.</summary>
    public bool CanChange => connectivity.IsOnline;

    /// <summary>Every cached user.</summary>
    public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
        => cache.GetAllAsync(cancellationToken);

    /// <summary>One cached user, or <c>null</c>.</summary>
    public Task<User?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => cache.GetAsync(id, cancellationToken);

    /// <summary>
    /// Checks <paramref name="user"/>, sends it to the server, and caches what the server kept.
    /// Never throws for a refusal or a server failure; the result says what happened.
    /// </summary>
    public async Task<UserSaveResult> SaveAsync(User user, CancellationToken cancellationToken = default)
    {
        var normalized = user with
        {
            Name = user.Name?.Trim() ?? string.Empty,
            Email = Normalize(user.Email),
            Notes = Normalize(user.Notes),
        };

        var errors = UserValidation.Validate(normalized, await cache.GetAllAsync(cancellationToken).ConfigureAwait(false));
        if (errors.Count > 0)
        {
            return new UserSaveResult(null, errors, null);
        }

        if (!connectivity.IsOnline)
        {
            return new UserSaveResult(null, [], OfflineMessage);
        }

        try
        {
            var saved = await api.PutAsync(normalized with { UpdatedAt = time.GetUtcNow() }, cancellationToken).ConfigureAwait(false);
            await cache.SaveAsync(saved, cancellationToken).ConfigureAwait(false);
            return new UserSaveResult(saved, [], null);
        }
        catch (UserRejectedException rejected)
        {
            return new UserSaveResult(null, rejected.Errors, null);
        }
        catch (TodoApiException failure)
        {
            return new UserSaveResult(null, [], "Not saved. " + FailureText(failure.Error));
        }
    }

    /// <summary>
    /// Fetches every user and replaces the cache, only once the whole list has arrived.
    /// </summary>
    /// <exception cref="TodoApiException">The server could not be asked; the cache is unchanged.</exception>
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var users = await api.GetAllAsync(cancellationToken).ConfigureAwait(false);
        await cache.ReplaceAllAsync(users, cancellationToken).ConfigureAwait(false);
    }

    private static string FailureText(SyncError error) => error switch
    {
        SyncError.Unreachable => "The server could not be reached.",
        SyncError.Timeout => "The server did not answer in time.",
        SyncError.Offline => OfflineMessage,
        _ => TodoListRules.ErrorMessage(error),
    };

    private static string? Normalize(string? text)
        => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}

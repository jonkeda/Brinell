namespace Bruinell.Samples.Todo.UnitTests;

/// <summary>A user cache in a dictionary.</summary>
internal sealed class InMemoryUserRepository : IUserRepository
{
    private readonly Dictionary<Guid, User> _users = [];

    public IReadOnlyCollection<User> Users => _users.Values;

    public InMemoryUserRepository With(params User[] users)
    {
        foreach (var user in users)
        {
            _users[user.Id] = user;
        }

        return this;
    }

    public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<User>>(_users.Values.ToList());

    public Task<User?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_users.GetValueOrDefault(id));

    public Task SaveAsync(User user, CancellationToken cancellationToken = default)
    {
        _users[user.Id] = user;
        return Task.CompletedTask;
    }

    public Task ReplaceAllAsync(IReadOnlyList<User> users, CancellationToken cancellationToken = default)
    {
        _users.Clear();
        return Task.FromResult(With([.. users]));
    }
}

/// <summary>A server that keeps what it is sent and answers with it, unless told to fail.</summary>
internal sealed class FakeUserApi : IUserApi
{
    private readonly Dictionary<Guid, User> _users = [];

    public Exception? Failure { get; set; }

    public List<User> Puts { get; } = [];

    public IReadOnlyCollection<User> Users => _users.Values;

    public FakeUserApi With(params User[] users)
    {
        foreach (var user in users)
        {
            _users[user.Id] = user;
        }

        return this;
    }

    public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
        => Failure is { } failure
            ? Task.FromException<IReadOnlyList<User>>(failure)
            : Task.FromResult<IReadOnlyList<User>>(_users.Values.ToList());

    public Task<User> PutAsync(User user, CancellationToken cancellationToken = default)
    {
        if (Failure is { } failure)
        {
            return Task.FromException<User>(failure);
        }

        Puts.Add(user);
        _users[user.Id] = user;
        return Task.FromResult(user);
    }
}

internal static class BuildUser
{
    public static User User(
        string name = "Eva",
        UserStatus status = UserStatus.Active,
        DateOnly? allowedUntil = null,
        UserRole role = UserRole.Member,
        Guid? id = null)
        => new()
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
            Status = status,
            AllowedUntil = allowedUntil,
            Role = role,
            HoursPerWeek = 40,
            HourlyRate = 50m,
            UpdatedAt = Build.Now.AddDays(-7),
        };

    public static UserService Service(InMemoryUserRepository cache, FakeUserApi api, bool online = true)
        => new(cache, api, Build.Online(online), Build.Clock());
}

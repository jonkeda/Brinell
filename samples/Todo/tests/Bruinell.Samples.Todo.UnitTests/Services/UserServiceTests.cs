namespace Bruinell.Samples.Todo.UnitTests.Services;

[Trait("Module", "Todo")]
[Trait("Pyramid", "Unit")]
public sealed class UserServiceTests
{
    private readonly InMemoryUserRepository _cache = new();
    private readonly FakeUserApi _api = new();

    [Fact]
    [Trait("Journey", "TOD.11.2")]
    public async Task Save_SendsTheUser_AndCachesWhatTheServerKept()
    {
        var result = await BuildUser.Service(_cache, _api).SaveAsync(BuildUser.User("  Eva ") with { Email = " ", Notes = " Hi " });

        Assert.True(result.Succeeded);
        var sent = Assert.Single(_api.Puts);
        Assert.Equal("Eva", sent.Name);
        Assert.Null(sent.Email);
        Assert.Equal("Hi", sent.Notes);
        Assert.Equal(Build.Now, sent.UpdatedAt);
        Assert.Equal(sent, Assert.Single(_cache.Users));
    }

    [Fact]
    [Trait("Journey", "TOD.11.3")]
    public async Task Save_WithABadField_SendsNothing()
    {
        var result = await BuildUser.Service(_cache, _api).SaveAsync(BuildUser.User("") with { HoursPerWeek = 0 });

        Assert.False(result.Succeeded);
        Assert.Equal([UserField.Name, UserField.HoursPerWeek], result.Errors.Select(error => error.Field));
        Assert.Empty(_api.Puts);
    }

    [Fact]
    [Trait("Journey", "TOD.11.3")]
    public async Task Save_WithANameTakenInTheCache_IsRefused()
    {
        _cache.With(BuildUser.User("Eva"));

        var result = await BuildUser.Service(_cache, _api).SaveAsync(BuildUser.User("eva"));

        Assert.Equal(UserValidation.NameTaken, Assert.Single(result.Errors).Message);
        Assert.Empty(_api.Puts);
    }

    [Fact]
    [Trait("Journey", "TOD.11.8")]
    public async Task Save_Offline_SendsNothing_AndSaysWhy()
    {
        var result = await BuildUser.Service(_cache, _api, online: false).SaveAsync(BuildUser.User());

        Assert.False(result.Succeeded);
        Assert.Equal(UserService.OfflineMessage, result.Failure);
        Assert.Empty(_api.Puts);
        Assert.Empty(_cache.Users);
    }

    [Fact]
    [Trait("Journey", "TOD.11.3")]
    public async Task Save_TheServersRefusal_IsReportedPerField()
    {
        _api.Failure = new UserRejectedException([new UserFieldError(UserField.Name, UserValidation.NameTaken)]);

        var result = await BuildUser.Service(_cache, _api).SaveAsync(BuildUser.User());

        Assert.Equal(new UserFieldError(UserField.Name, UserValidation.NameTaken), Assert.Single(result.Errors));
        Assert.Null(result.Failure);
        Assert.Empty(_cache.Users);
    }

    [Theory]
    [Trait("Journey", "TOD.11.8")]
    [InlineData(SyncError.Unreachable, "Not saved. The server could not be reached.")]
    [InlineData(SyncError.ServerError, "Not saved. The server had a problem. Try again.")]
    public async Task Save_AServerFailure_IsReported_AndNothingIsCached(SyncError error, string message)
    {
        _api.Failure = new TodoApiException(error, "failed");

        var result = await BuildUser.Service(_cache, _api).SaveAsync(BuildUser.User());

        Assert.Equal(message, result.Failure);
        Assert.Empty(_cache.Users);
    }

    [Fact]
    [Trait("Journey", "TOD.11.9")]
    public async Task Refresh_ReplacesTheCache_WithTheServersList()
    {
        _cache.With(BuildUser.User("Old"));
        _api.With(BuildUser.User("Eva"), BuildUser.User("Daan"));

        await BuildUser.Service(_cache, _api).RefreshAsync();

        Assert.Equal(["Daan", "Eva"], _cache.Users.Select(user => user.Name).Order());
    }

    [Fact]
    [Trait("Journey", "TOD.11.9")]
    public async Task Refresh_ThatFails_LeavesTheCacheAsItWas()
    {
        _cache.With(BuildUser.User("Old"));
        _api.Failure = new TodoApiException(SyncError.Timeout, "slow");

        await Assert.ThrowsAsync<TodoApiException>(() => BuildUser.Service(_cache, _api).RefreshAsync());

        Assert.Equal("Old", Assert.Single(_cache.Users).Name);
    }
}

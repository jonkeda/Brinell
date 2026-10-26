namespace Bruinet.Samples.Todo.UnitTests.ViewModels;

[Trait("Module", "Todo")]
[Trait("Pyramid", "Unit")]
public sealed class UserListViewModelTests
{
    private readonly InMemoryUserRepository _cache = new();
    private readonly INavigator _navigator = Substitute.For<INavigator>();

    [Fact]
    [Trait("Journey", "TOD.11.1")]
    public async Task TheRows_ShowAvailableUsersFirst_EachWithItsRoleAndState()
    {
        _cache.With(
            BuildUser.User("Hana", UserStatus.OnLeave),
            BuildUser.User("Eva", allowedUntil: new DateOnly(2026, 12, 31)),
            BuildUser.User("Finn", allowedUntil: new DateOnly(2026, 3, 1)),
            BuildUser.User("Daan", role: UserRole.Lead),
            BuildUser.User("Gus", UserStatus.Inactive, role: UserRole.Admin));
        var list = Create();

        await list.ReloadAsync();

        Assert.Equal(
            [("Daan", "Lead", ""), ("Eva", "Member", ""), ("Finn", "Member", "Expired"), ("Gus", "Admin", "Inactive"), ("Hana", "Member", "On leave")],
            list.Items.Select(row => (row.Name, row.RoleText, row.StateText)));
        Assert.Equal([true, true, false, false, false], list.Items.Select(row => row.IsAvailable));
    }

    [Fact]
    [Trait("Journey", "TOD.11.8")]
    public async Task Offline_AddIsDisabled_TheNoticeShows_AndTheListStillReads()
    {
        _cache.With(BuildUser.User("Eva"));
        var list = Create(online: false);

        await list.ReloadAsync();

        Assert.True(list.IsOffline);
        Assert.False(list.AddCommand.CanExecute(null));
        Assert.Single(list.Items);
    }

    [Fact]
    [Trait("Journey", "TOD.11.2")]
    public async Task Add_OpensAnEmptyEditPage()
    {
        await Create().AddCommand.ExecuteAsync(null);

        await _navigator.Received(1).GoToAsync(UserRoutes.Edit);
    }

    [Fact]
    [Trait("Journey", "TOD.11.4")]
    public async Task SelectingARow_OpensThatUser_AndClearsTheSelection()
    {
        var eva = BuildUser.User("Eva");
        _cache.With(eva);
        var list = Create();
        await list.ReloadAsync();

        list.SelectedItem = list.Items[0];

        await _navigator.Received(1).GoToAsync(UserRoutes.Edit, Arg.Is<IReadOnlyDictionary<string, object>>(p => (string)p[TodoRoutes.IdParameter] == eva.Id.ToString("D")));
        Assert.Null(list.SelectedItem);
    }

    private UserListViewModel Create(bool online = true)
        => new(BuildUser.Service(_cache, new FakeUserApi(), online), Build.Online(online), _navigator);
}

namespace Brinell.Samples.Todo.UnitTests.Rules;

[Trait("Module", "Todo")]
[Trait("Pyramid", "Unit")]
public sealed class UserRulesTests
{
    public static TheoryData<UserStatus, int?, bool, string> Availability => new()
    {
        // status, end date as days from today (null: none), available, state text
        { UserStatus.Active, null, true, "" },
        { UserStatus.Active, 1, true, "" },
        { UserStatus.Active, 0, true, "" },
        { UserStatus.Active, -1, false, UserRules.Expired },
        { UserStatus.OnLeave, null, false, UserRules.OnLeave },
        { UserStatus.OnLeave, 1, false, UserRules.OnLeave },
        { UserStatus.OnLeave, -1, false, UserRules.OnLeave },
        { UserStatus.Inactive, null, false, UserRules.Inactive },
        { UserStatus.Inactive, -1, false, UserRules.Inactive },
    };

    [Theory]
    [MemberData(nameof(Availability))]
    [Trait("Journey", "TOD.11.6")]
    public void Availability_DependsOnStatusAndEndDate(UserStatus status, int? daysLeft, bool available, string state)
    {
        var user = BuildUser.User(status: status, allowedUntil: daysLeft is { } days ? Build.Today.AddDays(days) : null);

        Assert.Equal(available, UserRules.IsAvailable(user, Build.Today));
        Assert.Equal(state, UserRules.StateText(user, Build.Today));
    }

    [Fact]
    [Trait("Journey", "TOD.11.1")]
    public void Arrange_PutsAvailableUsersFirst_ThenTheOthers_EachByName()
    {
        var users = new[]
        {
            BuildUser.User("hana", UserStatus.OnLeave),
            BuildUser.User("Eva"),
            BuildUser.User("Finn", allowedUntil: Build.Today.AddDays(-9)),
            BuildUser.User("daan"),
            BuildUser.User("Gus", UserStatus.Inactive),
        };

        var names = UserRules.Arrange(users, Build.Today).Select(user => user.Name);

        Assert.Equal(["daan", "Eva", "Finn", "Gus", "hana"], names);
    }
}

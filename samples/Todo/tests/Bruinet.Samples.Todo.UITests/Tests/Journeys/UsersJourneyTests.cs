using Bruinet.Samples.Todo.Contracts;
using Bruinet.Samples.Todo.UITests.Pages;

namespace Bruinet.Samples.Todo.UITests.Tests.Journeys;

/// <summary>TOD.11, hermetic: managing users on the <c>team</c> test set.</summary>
/// <remarks>
/// Every test that changes users works on users of its own, with a unique name, so none depends on
/// another having run first; the app's cached users are not reset between tests, so order checks
/// look at the team only. The rules at their boundaries are unit tests; these show each rule once,
/// on screen.
/// </remarks>
[Collection(TeamCollection.Name)]
[Trait("Category", "UITest")]
[Trait("Module", "Users")]
[Trait("Pyramid", "UiHermetic")]
public sealed class UsersJourneyTests
{
    private const string OfflineNotice = "You are offline. Users can be viewed, but not changed.";

    private readonly TeamFixture _fixture;

    public UsersJourneyTests(TeamFixture fixture)
    {
        _fixture = fixture;
        fixture.StartTest();
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Journey", "TOD.11.1")]
    public Task Users_ListsAvailableUsersFirst_EachWithItsRoleAndState()
    {
        var users = _fixture.List.OpenUsers();

        users.Users
            .AssertOrder(["Daan", "Eva", "Finn", "Gus", "Hana"])
            .AssertRole("Daan", "Lead")
            .AssertNoState("Daan")
            .AssertState("Finn", "Expired")
            .AssertState("Gus", "Inactive")
            .AssertState("Hana", "On leave");

        users.Back();
        return Task.CompletedTask;
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Journey", "TOD.11.2")]
    public Task Add_WithEveryField_ShowsTheUser_AndTheServerHasEachValue()
    {
        var name = Unique("Ivo");
        var edit = _fixture.List.OpenUsers().Add().EnterName(name);
        edit.Email.SetText("ivo@example.com");
        edit.Role.SelectByText("Lead");
        edit.SetHours(32);
        edit.Rate.SetText("1,250.75");
        edit.SetStartOfDay(new TimeOnly(8, 30));
        edit.Reminders.SetChecked(false);
        edit.SetWorkload(35);

        var list = edit.SaveNew();

        list.Users.AssertRole(name, "Lead").AssertNoState(name);
        var saved = Assert.Single(_fixture.Server.Users, user => user.Name == name);
        Assert.Equal(
            ("ivo@example.com", UserRoleDto.Lead, 32, 1250.75m, new TimeOnly(8, 30), false, 35, UserStatusDto.Active, (DateOnly?)null),
            (saved.Email, saved.Role, saved.HoursPerWeek, saved.HourlyRate, saved.StartOfDay, saved.ReceivesReminders, saved.Workload, saved.Status, saved.AllowedUntil));
        list.Back();
        return Task.CompletedTask;
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Journey", "TOD.11.3")]
    public Task Save_WithBadValues_ShowsEachErrorOnItsField_AndStays()
    {
        var edit = _fixture.List.OpenUsers().Add();
        edit.Email.SetText("eva@");
        edit.Rate.SetText("1,5");

        edit.SaveExpectingErrors();

        edit.NameError.AssertText("Name is required");
        edit.EmailError.AssertText("Enter an email address such as eva@example.com");
        edit.RateError.AssertText("Enter a rate such as 1,250.75");

        edit.EnterName("Eva").SaveExpectingErrors();
        edit.NameError.AssertText("Another user already has this name");

        edit.Cancel().Back();
        return Task.CompletedTask;
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Journey", "TOD.11.4")]
    public Task Edit_ChangesWhatTheListShows_AndReopeningShowsTheValues()
    {
        var name = Unique("Jet");
        var users = _fixture.List.OpenUsers().AddUser(name);

        var edit = users.Open(name);
        edit.Role.SelectByText("Admin");
        edit.SetStatus("On leave");
        edit.SetHours(20);
        edit.SetStartOfDay(new TimeOnly(7, 45));
        users = edit.SaveNew();

        users.Users.AssertRole(name, "Admin").AssertState(name, "On leave");
        var reopened = users.Open(name);
        reopened.AssertStatus("On leave");
        reopened.HoursValue.AssertText("20 hours");
        reopened.AssertStartOfDay(new TimeOnly(7, 45));

        reopened.Cancel().Back();
        return Task.CompletedTask;
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Journey", "TOD.11.5")]
    [Trait("Journey", "TOD.11.6")]
    public Task EndDate_IsOnlyEditableWhenChecked_AndAPastOneShowsExpired_ATomorrowOneDoesNot()
    {
        var yesterday = Unique("Kees");
        var tomorrow = Unique("Lia");
        var edit = _fixture.List.OpenUsers().Add().EnterName(yesterday);

        edit.HasEndDate.AssertChecked(false);
        edit.AllowedUntil.AssertEnabled(false);
        edit.SetEndDate(new DateOnly(2026, 3, 9));
        var users = edit.SaveNew();

        edit = users.Add().EnterName(tomorrow);
        edit.SetEndDate(new DateOnly(2026, 3, 11));
        users = edit.SaveNew();

        users.Users.AssertState(yesterday, "Expired").AssertNoState(tomorrow);
        Assert.Equal(new DateOnly(2026, 3, 9), Assert.Single(_fixture.Server.Users, user => user.Name == yesterday).AllowedUntil);

        users.Back();
        return Task.CompletedTask;
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Journey", "TOD.11.7")]
    public Task Inactive_KeepsTheUserListed_AndTheNameCanBeUsedAgain()
    {
        var name = Unique("Mo");
        var users = _fixture.List.OpenUsers().AddUser(name);

        users = users.Open(name).SetStatus("Inactive").SaveNew();
        users.Users.AssertState(name, "Inactive");

        users = users.AddUser(name);

        users.Users.AssertRow(name);
        Assert.Equal(2, users.Users.Names().Count(shown => shown == name));
        users.Back();
        return Task.CompletedTask;
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Journey", "TOD.11.8")]
    public Task Offline_TheListStillShows_AddAndSaveAreDisabled_AndThePageSaysWhy()
    {
        _fixture.Network.SetOffline();

        var users = _fixture.List.OpenUsers();
        users.OfflineNotice.AssertText(OfflineNotice);
        users.AddButton.AssertEnabled(false);
        users.Users.AssertRow("Eva");

        var edit = users.Open("Eva");
        edit.OfflineNotice.AssertText(OfflineNotice);
        edit.SaveButton.AssertEnabled(false);

        edit.Cancel().Back();
        return Task.CompletedTask;
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Journey", "TOD.11.9")]
    public Task AUserAddedOnAnotherDevice_ShowsAfterASync()
    {
        var name = _fixture.ServerHasUser("Noor");

        // A sync fetches users before todos, so the todo arriving says the users have too.
        var title = _fixture.ServerHasTodo("Welcome Noor");
        _fixture.List.Sync().Todos.AssertRow(title);

        var users = _fixture.List.OpenUsers();
        users.Users.AssertRow(name).AssertNoState(name);

        users.Back();
        return Task.CompletedTask;
    }

    private static string Unique(string name) => $"{name} {Guid.NewGuid().ToString("N")[..6]}";
}

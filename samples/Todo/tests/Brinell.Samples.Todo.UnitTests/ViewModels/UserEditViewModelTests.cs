namespace Brinell.Samples.Todo.UnitTests.ViewModels;

[Trait("Module", "Todo")]
[Trait("Pyramid", "Unit")]
public sealed class UserEditViewModelTests
{
    private readonly InMemoryUserRepository _cache = new();
    private readonly FakeUserApi _api = new();
    private readonly INavigator _navigator = Substitute.For<INavigator>();

    [Fact]
    [Trait("Journey", "TOD.11.2")]
    public async Task New_StartsWithTheDefaults()
    {
        var edit = Create();

        await edit.LoadAsync(null);

        Assert.True(edit.IsNew);
        Assert.Equal("New user", edit.PageTitle);
        Assert.True(edit.IsActive);
        Assert.False(edit.HasEndDate);
        Assert.Equal(0, edit.RoleIndex);
        Assert.Equal(40, edit.HoursPerWeek);
        Assert.Equal("40 hours", edit.HoursText);
        Assert.Equal("0.00", edit.RateText);
        Assert.Equal(new TimeSpan(9, 0, 0), edit.StartOfDay);
        Assert.True(edit.ReceivesReminders);
        Assert.Equal("0 %", edit.WorkloadText);
        Assert.Equal(string.Empty, edit.LastChangedText);
    }

    [Fact]
    [Trait("Journey", "TOD.11.2")]
    public async Task Save_New_SendsEveryField_AndGoesBack()
    {
        var edit = Create();
        await edit.LoadAsync(null);
        edit.Name = "Eva";
        edit.Email = "eva@example.com";
        edit.IsOnLeave = true;
        edit.HasEndDate = true;
        edit.AllowedUntil = new DateTime(2026, 12, 31, 15, 0, 0);
        edit.RoleIndex = 1;
        edit.HoursPerWeek = 38;
        edit.RateText = "1,250.75";
        edit.StartOfDay = new TimeSpan(8, 30, 0);
        edit.ReceivesReminders = false;
        edit.Workload = 36;
        edit.Notes = "Works Monday to Thursday";

        await edit.SaveCommand.ExecuteAsync(null);

        var sent = Assert.Single(_api.Puts);
        Assert.Equal("Eva", sent.Name);
        Assert.Equal("eva@example.com", sent.Email);
        Assert.Equal(UserStatus.OnLeave, sent.Status);
        Assert.Equal(new DateOnly(2026, 12, 31), sent.AllowedUntil);
        Assert.Equal(UserRole.Lead, sent.Role);
        Assert.Equal(38, sent.HoursPerWeek);
        Assert.Equal(1250.75m, sent.HourlyRate);
        Assert.Equal(new TimeOnly(8, 30), sent.StartOfDay);
        Assert.False(sent.ReceivesReminders);
        Assert.Equal(35, sent.Workload);
        Assert.Equal("Works Monday to Thursday", sent.Notes);
        await _navigator.Received(1).GoBackAsync();
    }

    [Fact]
    [Trait("Journey", "TOD.11.4")]
    public async Task Edit_LoadsEveryField_AndSavesTheChange()
    {
        var eva = BuildUser.User("Eva", UserStatus.OnLeave, new DateOnly(2026, 12, 31), UserRole.Admin) with
        {
            Email = "eva@example.com",
            HoursPerWeek = 38,
            HourlyRate = 1250.75m,
            StartOfDay = new TimeOnly(8, 30),
            ReceivesReminders = false,
            Workload = 35,
            Notes = "Hi",
            UpdatedAt = new DateTimeOffset(2026, 3, 1, 8, 0, 0, TimeSpan.Zero),
        };
        _cache.With(eva);
        var edit = Create();

        await edit.LoadAsync(eva.Id);

        Assert.Equal("Edit user", edit.PageTitle);
        Assert.Equal("Eva", edit.Name);
        Assert.Equal("eva@example.com", edit.Email);
        Assert.True(edit.IsOnLeave);
        Assert.True(edit.HasEndDate);
        Assert.Equal(new DateTime(2026, 12, 31), edit.AllowedUntil);
        Assert.Equal(2, edit.RoleIndex);
        Assert.Equal(38, edit.HoursPerWeek);
        Assert.Equal("1,250.75", edit.RateText);
        Assert.Equal(new TimeSpan(8, 30, 0), edit.StartOfDay);
        Assert.False(edit.ReceivesReminders);
        Assert.Equal("35 %", edit.WorkloadText);
        Assert.Equal("Hi", edit.Notes);
        Assert.StartsWith("Last changed 01-Mar-2026", edit.LastChangedText);

        edit.Name = "Eva B";
        await edit.SaveCommand.ExecuteAsync(null);

        var sent = Assert.Single(_api.Puts);
        Assert.Equal(eva.Id, sent.Id);
        Assert.Equal(eva with { Name = "Eva B", UpdatedAt = Build.Now }, sent);
    }

    [Fact]
    [Trait("Journey", "TOD.11.3")]
    public async Task Save_ShowsEveryError_OnItsField_AndStays()
    {
        var edit = Create();
        await edit.LoadAsync(null);
        edit.Email = "eva@";
        edit.HoursPerWeek = 0;
        edit.RateText = "1,5";
        edit.Notes = new string('n', 501);

        await edit.SaveCommand.ExecuteAsync(null);

        Assert.Equal(UserValidation.NameRequired, edit.NameError);
        Assert.Equal(UserValidation.EmailInvalid, edit.EmailError);
        Assert.Equal(UserValidation.HoursOutOfRange, edit.HoursError);
        Assert.Equal(UserValidation.RateUnreadable, edit.RateError);
        Assert.Equal(UserValidation.NotesTooLong, edit.NotesError);
        Assert.True(edit.HasNameError && edit.HasEmailError && edit.HasHoursError && edit.HasRateError && edit.HasNotesError);
        Assert.False(edit.HasWorkloadError);
        Assert.Empty(_api.Puts);
        await _navigator.DidNotReceive().GoBackAsync();
    }

    [Fact]
    [Trait("Journey", "TOD.11.3")]
    public async Task TypingInAField_ClearsOnlyThatFieldsError()
    {
        var edit = Create();
        await edit.LoadAsync(null);
        edit.HoursPerWeek = 0;
        await edit.SaveCommand.ExecuteAsync(null);

        edit.Name = "E";

        Assert.Null(edit.NameError);
        Assert.Equal(UserValidation.HoursOutOfRange, edit.HoursError);
    }

    [Fact]
    [Trait("Journey", "TOD.11.3")]
    public async Task TheServersRefusal_ShowsOnTheField()
    {
        _api.Failure = new UserRejectedException([new UserFieldError(UserField.Name, UserValidation.NameTaken)]);
        var edit = Create();
        await edit.LoadAsync(null);
        edit.Name = "Eva";

        await edit.SaveCommand.ExecuteAsync(null);

        Assert.Equal(UserValidation.NameTaken, edit.NameError);
        Assert.False(edit.HasSaveError);
        await _navigator.DidNotReceive().GoBackAsync();
    }

    [Fact]
    [Trait("Journey", "TOD.11.5")]
    public async Task WithoutAnEndDate_ThePickedDateIsNotSaved()
    {
        var edit = Create();
        await edit.LoadAsync(null);
        edit.Name = "Eva";
        edit.HasEndDate = true;
        edit.AllowedUntil = new DateTime(2026, 12, 31);
        edit.HasEndDate = false;

        await edit.SaveCommand.ExecuteAsync(null);

        Assert.Null(Assert.Single(_api.Puts).AllowedUntil);
    }

    [Fact]
    [Trait("Journey", "TOD.11.5")]
    public async Task AnEndDateInThePast_IsSaved()
    {
        var edit = Create();
        await edit.LoadAsync(null);
        edit.Name = "Finn";
        edit.HasEndDate = true;
        edit.AllowedUntil = new DateTime(2026, 3, 1);

        await edit.SaveCommand.ExecuteAsync(null);

        Assert.Equal(new DateOnly(2026, 3, 1), Assert.Single(_api.Puts).AllowedUntil);
    }

    [Fact]
    [Trait("Journey", "TOD.11.3")]
    public void Workload_SnapsToStepsOf5()
    {
        var edit = Create();

        edit.Workload = 37.4;

        Assert.Equal(35, edit.Workload);
        Assert.Equal("35 %", edit.WorkloadText);
    }

    [Fact]
    [Trait("Journey", "TOD.11.7")]
    public async Task SettingInactive_SavesTheStatus()
    {
        var gus = BuildUser.User("Gus");
        _cache.With(gus);
        var edit = Create();
        await edit.LoadAsync(gus.Id);

        edit.IsInactive = true;
        await edit.SaveCommand.ExecuteAsync(null);

        Assert.True(edit.IsInactive);
        Assert.False(edit.IsActive);
        Assert.Equal(UserStatus.Inactive, Assert.Single(_api.Puts).Status);
    }

    [Fact]
    [Trait("Journey", "TOD.11.8")]
    public async Task Offline_SaveIsDisabled_AndTheNoticeShows()
    {
        var edit = Create(online: false);

        await edit.LoadAsync(null);

        Assert.False(edit.SaveCommand.CanExecute(null));
        Assert.True(edit.IsOffline);
        Assert.Equal(UserService.OfflineMessage, edit.OfflineNotice);
    }

    [Fact]
    [Trait("Journey", "TOD.11.8")]
    public async Task AServerFailure_ShowsTheMessage_AndStays()
    {
        _api.Failure = new TodoApiException(SyncError.Unreachable, "down");
        var edit = Create();
        await edit.LoadAsync(null);
        edit.Name = "Eva";

        await edit.SaveCommand.ExecuteAsync(null);

        Assert.Equal("Not saved. The server could not be reached.", edit.SaveError);
        await _navigator.DidNotReceive().GoBackAsync();
    }

    private UserEditViewModel Create(bool online = true)
        => new(BuildUser.Service(_cache, _api, online), Build.Online(online), _navigator);
}

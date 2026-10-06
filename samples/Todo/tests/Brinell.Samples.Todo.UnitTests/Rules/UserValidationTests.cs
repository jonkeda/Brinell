namespace Brinell.Samples.Todo.UnitTests.Rules;

[Trait("Module", "Todo")]
[Trait("Pyramid", "Unit")]
[Trait("Journey", "TOD.11.3")]
public sealed class UserValidationTests
{
    [Fact]
    public void AValidUser_HasNoErrors()
        => Assert.Empty(UserValidation.Validate(BuildUser.User() with { Email = "eva@example.com", Notes = "Hi" }, []));

    [Theory]
    [InlineData("", UserValidation.NameRequired)]
    [InlineData("   ", UserValidation.NameRequired)]
    [InlineData("12345678901234567890123456789012345678901234567890", null)]
    [InlineData("123456789012345678901234567890123456789012345678901", UserValidation.NameTooLong)]
    public void Name_IsRequired_AndAtMost50(string name, string? error)
        => Assert.Equal(error, Error(BuildUser.User(name), UserField.Name));

    [Theory]
    [InlineData("eva", false)]
    [InlineData("EVA ", false)]
    [InlineData("Eve", true)]
    public void Name_IsUniqueIgnoringCase_AmongUsersThatAreNotInactive(string name, bool free)
    {
        var eva = BuildUser.User("Eva");

        var error = Error(BuildUser.User(name), UserField.Name, eva);

        Assert.Equal(free ? null : UserValidation.NameTaken, error);
    }

    [Fact]
    [Trait("Journey", "TOD.11.7")]
    public void AnInactiveUsersName_CanBeTakenAgain()
    {
        var gone = BuildUser.User("Gus", UserStatus.Inactive);

        Assert.Null(Error(BuildUser.User("Gus"), UserField.Name, gone));
    }

    [Fact]
    public void AUser_DoesNotClashWithItself()
    {
        var eva = BuildUser.User("Eva");

        Assert.Null(Error(eva with { Workload = 5 }, UserField.Name, eva));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("eva@example.com", true)]
    [InlineData("eva@example", false)]
    [InlineData("eva.example.com", false)]
    [InlineData("@example.com", false)]
    [InlineData("eva@@example.com", false)]
    [InlineData("eva @example.com", false)]
    public void Email_IsOptional_ButMustLookLikeOne(string? email, bool valid)
        => Assert.Equal(valid ? null : UserValidation.EmailInvalid, Error(BuildUser.User() with { Email = email }, UserField.Email));

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(60, true)]
    [InlineData(61, false)]
    public void HoursPerWeek_Are1To60(int hours, bool valid)
        => Assert.Equal(valid ? null : UserValidation.HoursOutOfRange, Error(BuildUser.User() with { HoursPerWeek = hours }, UserField.HoursPerWeek));

    [Theory]
    [InlineData("0", null)]
    [InlineData("10000.00", null)]
    [InlineData("10000.01", UserValidation.RateOutOfRange)]
    [InlineData("-1", UserValidation.RateOutOfRange)]
    [InlineData("12.34", null)]
    [InlineData("12.345", UserValidation.RateTooPrecise)]
    public void HourlyRate_Is0To10000_WithTwoDecimals(string rate, string? error)
        => Assert.Equal(error, Error(BuildUser.User() with { HourlyRate = decimal.Parse(rate, System.Globalization.CultureInfo.InvariantCulture) }, UserField.HourlyRate));

    [Theory]
    [InlineData("1,250.75", 1250.75)]
    [InlineData("1250.75", 1250.75)]
    [InlineData(" 85 ", 85)]
    public void ARate_IsReadInOneFormat_WhateverTheCulture(string text, double expected)
    {
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("nl-NL");
        try
        {
            Assert.Null(UserValidation.ParseRate(text, out var rate));
            Assert.Equal((decimal)expected, rate);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = culture;
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("1,5")]
    [InlineData("twelve")]
    public void AnUnreadableRate_IsRefused(string text)
        => Assert.Equal(UserValidation.RateUnreadable, UserValidation.ParseRate(text, out _));

    [Fact]
    public void ARate_IsShownWithThousandsAndTwoDecimals()
        => Assert.Equal("1,250.75", UserValidation.FormatRate(1250.75m));

    [Theory]
    [InlineData(0, true)]
    [InlineData(35, true)]
    [InlineData(100, true)]
    [InlineData(33, false)]
    [InlineData(105, false)]
    [InlineData(-5, false)]
    public void Workload_Is0To100_InStepsOf5(int workload, bool valid)
        => Assert.Equal(valid ? null : UserValidation.WorkloadInvalid, Error(BuildUser.User() with { Workload = workload }, UserField.Workload));

    [Theory]
    [InlineData(500, true)]
    [InlineData(501, false)]
    public void Notes_AreAtMost500(int length, bool valid)
        => Assert.Equal(valid ? null : UserValidation.NotesTooLong, Error(BuildUser.User() with { Notes = new string('n', length) }, UserField.Notes));

    [Fact]
    public void EveryBadField_IsReported_OncePerField()
    {
        var user = BuildUser.User("") with { Email = "x", HoursPerWeek = 0, HourlyRate = 1.001m, Workload = 7, Notes = new string('n', 501) };

        var fields = UserValidation.Validate(user, []).Select(error => error.Field);

        Assert.Equal([UserField.Name, UserField.Email, UserField.HoursPerWeek, UserField.HourlyRate, UserField.Workload, UserField.Notes], fields);
    }

    private static string? Error(User user, UserField field, params User[] others)
        => UserValidation.Validate(user, others).SingleOrDefault(error => error.Field == field)?.Message;
}

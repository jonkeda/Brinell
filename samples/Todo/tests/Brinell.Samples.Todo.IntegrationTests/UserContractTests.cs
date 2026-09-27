using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Brinell.Samples.Todo.Api;
using Brinell.Samples.Todo.Core.Rules;
using Brinell.Samples.Todo.Infrastructure.Api;

namespace Brinell.Samples.Todo.IntegrationTests;

/// <summary>
/// The users part of the contract: the real <c>Todo.Api</c> in this process, the app's client,
/// and the WireMock fake, which must answer alike.
/// </summary>
[Trait("Module", "Todo")]
[Trait("Pyramid", "Contract")]
public sealed class UserContractTests : IAsyncLifetime
{
    private static readonly DateTimeOffset At = new(2026, 3, 1, 8, 0, 0, TimeSpan.Zero);

    private readonly TempDatabase _serverFile = new("api-users");
    private RunningTodoApi _api = null!;

    public async Task InitializeAsync()
        => _api = await TodoApiHost.StartAsync(new TodoApiOptions { DatabasePath = _serverFile.Path });

    public async Task DisposeAsync()
    {
        await _api.DisposeAsync();
        _serverFile.Dispose();
    }

    [Fact]
    [Trait("Journey", "TOD.11.2")]
    public async Task TheAppsClient_RoundTripsEveryField_AgainstTheRealApi()
    {
        var client = Client();
        var eva = Eva();

        var stored = await client.PutAsync(eva);
        var listed = Assert.Single(await client.GetAllAsync());

        Assert.Equal(eva, stored);
        Assert.Equal(eva, listed);
    }

    [Theory]
    [Trait("Journey", "TOD.11.10")]
    [InlineData("name", UserField.Name, UserValidation.NameRequired)]
    [InlineData("email", UserField.Email, UserValidation.EmailInvalid)]
    [InlineData("hours", UserField.HoursPerWeek, UserValidation.HoursOutOfRange)]
    [InlineData("rate", UserField.HourlyRate, UserValidation.RateTooPrecise)]
    [InlineData("workload", UserField.Workload, UserValidation.WorkloadInvalid)]
    [InlineData("notes", UserField.Notes, UserValidation.NotesTooLong)]
    public async Task TheRealApi_RefusesWhatTheAppRefuses_NamingTheField(string broken, UserField field, string message)
    {
        var user = broken switch
        {
            "name" => Eva() with { Name = " " },
            "email" => Eva() with { Email = "eva@" },
            "hours" => Eva() with { HoursPerWeek = 61 },
            "rate" => Eva() with { HourlyRate = 1.005m },
            "workload" => Eva() with { Workload = 33 },
            _ => Eva() with { Notes = new string('n', 501) },
        };

        var refused = await Assert.ThrowsAsync<UserRejectedException>(() => Client().PutAsync(user));

        Assert.Equal(new UserFieldError(field, message), Assert.Single(refused.Errors));
        Assert.Equal(message, Assert.Single(UserValidation.Validate(user with { Name = user.Name.Trim() }, [])).Message);
    }

    [Fact]
    [Trait("Journey", "TOD.11.7")]
    public async Task ANameIsTaken_UntilItsOwnerIsInactive()
    {
        var client = Client();
        var gus = await client.PutAsync(Eva() with { Name = "Gus" });

        var taken = await Assert.ThrowsAsync<UserRejectedException>(() => client.PutAsync(Eva() with { Id = Guid.NewGuid(), Name = "GUS" }));
        await client.PutAsync(gus with { Status = UserStatus.Inactive });
        var reused = await client.PutAsync(Eva() with { Id = Guid.NewGuid(), Name = "Gus" });

        Assert.Equal(UserValidation.NameTaken, Assert.Single(taken.Errors).Message);
        Assert.Equal(2, (await client.GetAllAsync()).Count(user => user.Name == "Gus"));
        Assert.Equal(UserStatus.Active, reused.Status);
    }

    [Fact]
    [Trait("Journey", "TOD.11.10")]
    public async Task TheWireMockFake_AnswersUsersLikeTheRealApi()
    {
        using var mock = MockApiServer.Start();
        TodoServerStub.Install(mock, []);

        var real = await RunScriptAsync(_api.BaseUrl);
        var fake = await RunScriptAsync(mock.BaseUrl);

        Assert.Equal(real, fake);
    }

    private static async Task<IReadOnlyList<string>> RunScriptAsync(string baseUrl)
    {
        using var keyed = TodoApiClient.CreateHttpClient(baseUrl, TodoApi.DevelopmentApiKey, TimeSpan.FromSeconds(5));
        using var anonymous = new HttpClient { BaseAddress = new Uri(baseUrl + "/") };
        var eva = UserMapping.ToDto(Eva() with { Id = Guid.Parse("77777777-0000-0000-0000-0000000000aa") });
        var other = eva with { Id = Guid.Parse("77777777-0000-0000-0000-0000000000bb") };

        var steps = new List<(string Name, Func<Task<HttpResponseMessage>> Send)>
        {
            ("list without key", () => anonymous.GetAsync(Relative(TodoApi.UsersPath))),
            ("list empty", () => keyed.GetAsync(Relative(TodoApi.UsersPath))),
            ("put", () => keyed.PutAsJsonAsync(Relative(TodoApi.UserPath(eva.Id)), eva, TodoApi.JsonOptions)),
            ("put wrong id", () => keyed.PutAsJsonAsync(Relative(TodoApi.UserPath(Guid.NewGuid())), eva, TodoApi.JsonOptions)),
            ("put bad hours", () => keyed.PutAsJsonAsync(Relative(TodoApi.UserPath(eva.Id)), eva with { HoursPerWeek = 0 }, TodoApi.JsonOptions)),
            ("put taken name", () => keyed.PutAsJsonAsync(Relative(TodoApi.UserPath(other.Id)), other with { Name = "EVA" }, TodoApi.JsonOptions)),
            ("get one", () => keyed.GetAsync(Relative(TodoApi.UserPath(eva.Id)))),
            ("get missing", () => keyed.GetAsync(Relative(TodoApi.UserPath(other.Id)))),
            ("list one", () => keyed.GetAsync(Relative(TodoApi.UsersPath))),
        };

        var outcomes = new List<string>();
        foreach (var (name, send) in steps)
        {
            using var response = await send();
            outcomes.Add($"{name}: {(int)response.StatusCode} {await NormaliseAsync(response)}");
        }

        return outcomes;
    }

    // A success body is compared as users; a refusal by its fields and messages; any other error
    // body is prose, which is not part of the contract.
    private static async Task<string> NormaliseAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        if (response.StatusCode is HttpStatusCode.OK)
        {
            return body.TrimStart().StartsWith('[')
                ? JsonSerializer.Serialize(JsonSerializer.Deserialize<List<UserDto>>(body, TodoApi.JsonOptions))
                : JsonSerializer.Serialize(JsonSerializer.Deserialize<UserDto>(body, TodoApi.JsonOptions));
        }

        if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict && body.Contains("\"errors\""))
        {
            var problem = JsonSerializer.Deserialize<ValidationProblemDto>(body, TodoApi.JsonOptions)!;
            return string.Join(";", problem.Errors.OrderBy(pair => pair.Key).Select(pair => $"{pair.Key}={string.Join("|", pair.Value)}"));
        }

        return string.Empty;
    }

    private IUserApi Client()
        => new TodoApiClient(TodoApiClient.CreateHttpClient(_api.BaseUrl, TodoApi.DevelopmentApiKey, TimeSpan.FromSeconds(5)));

    private static User Eva() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Eva",
        Email = "eva@example.com",
        Status = UserStatus.Active,
        AllowedUntil = new DateOnly(2026, 12, 31),
        Role = UserRole.Lead,
        HoursPerWeek = 38,
        HourlyRate = 1250.75m,
        StartOfDay = new TimeOnly(8, 30),
        ReceivesReminders = false,
        Workload = 35,
        Notes = "Monday to Thursday",
        UpdatedAt = At,
    };

    private static string Relative(string path) => path.TrimStart('/');
}

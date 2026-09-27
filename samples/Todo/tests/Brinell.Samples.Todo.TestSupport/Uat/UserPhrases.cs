using Brinell.Core.Testing;
using Brinell.Samples.Todo.Contracts;
using Brinell.Uat;

namespace Brinell.Samples.Todo.TestSupport.Uat;

/// <summary>
/// Steps about the users the fake server holds (TOD.11), in the <c>Users</c> namespace: a scenario
/// uses them, and the users pages, with <c>Using | Users</c> (AD-019).
/// </summary>
/// <remarks>
/// A table is checked against <see cref="UserDto"/> by column, so a row lists only what matters:
/// <c>Name | Role | Start Of Day | Receives Reminders</c> with <c>Lead | 08:30 | off</c>.
/// </remarks>
[UatPhraseClass(Namespace = "Users")]
public sealed class UserPhrases(ITodoArrangement fixture) : UatPhraseClassBase
{
    /// <summary>Adds an available Member to the fake server, as another device would.</summary>
    [UatGiven("the server also has a user {name}")]
    public void ServerAlsoHas(string name)
        => fixture.Server.StoreUser(new UserDto(
            Guid.NewGuid(), name, null, UserStatusDto.Active, null, UserRoleDto.Member,
            40, 0m, new TimeOnly(9, 0), true, 0, null, fixture.TestSet.Now.AddDays(-1)));

    /// <summary>The fake server holds a user like each row.</summary>
    [UatThen("the server should have users")]
    public void ServerHas(UatTable table) => table.AssertContains(fixture.Server.Users, fixture.TestSet.Now);
}

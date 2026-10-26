using Bruinet.Samples.Todo.Contracts;
using Bruinet.Samples.Todo.Core.Models;

namespace Bruinet.Samples.Todo.Infrastructure.Api;

/// <summary>Maps users between the wire format and the app's model.</summary>
public static class UserMapping
{
    /// <summary>A server user as the app caches it.</summary>
    public static User ToUser(UserDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        Email = dto.Email,
        Status = dto.Status switch
        {
            UserStatusDto.Active => UserStatus.Active,
            UserStatusDto.OnLeave => UserStatus.OnLeave,
            UserStatusDto.Inactive => UserStatus.Inactive,
            _ => throw new ArgumentOutOfRangeException(nameof(dto), dto.Status, null),
        },
        AllowedUntil = dto.AllowedUntil,
        Role = dto.Role switch
        {
            UserRoleDto.Member => UserRole.Member,
            UserRoleDto.Lead => UserRole.Lead,
            UserRoleDto.Admin => UserRole.Admin,
            _ => throw new ArgumentOutOfRangeException(nameof(dto), dto.Role, null),
        },
        HoursPerWeek = dto.HoursPerWeek,
        HourlyRate = dto.HourlyRate,
        StartOfDay = dto.StartOfDay,
        ReceivesReminders = dto.ReceivesReminders,
        Workload = dto.Workload,
        Notes = dto.Notes,
        UpdatedAt = dto.UpdatedAt,
    };

    /// <summary>An app user on the wire.</summary>
    public static UserDto ToDto(User user) => new(
        user.Id,
        user.Name,
        user.Email,
        user.Status switch
        {
            UserStatus.Active => UserStatusDto.Active,
            UserStatus.OnLeave => UserStatusDto.OnLeave,
            UserStatus.Inactive => UserStatusDto.Inactive,
            _ => throw new ArgumentOutOfRangeException(nameof(user), user.Status, null),
        },
        user.AllowedUntil,
        user.Role switch
        {
            UserRole.Member => UserRoleDto.Member,
            UserRole.Lead => UserRoleDto.Lead,
            UserRole.Admin => UserRoleDto.Admin,
            _ => throw new ArgumentOutOfRangeException(nameof(user), user.Role, null),
        },
        user.HoursPerWeek,
        user.HourlyRate,
        user.StartOfDay,
        user.ReceivesReminders,
        user.Workload,
        user.Notes,
        user.UpdatedAt);
}

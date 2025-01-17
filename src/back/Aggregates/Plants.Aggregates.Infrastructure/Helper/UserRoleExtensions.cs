﻿namespace Plants.Aggregates.Infrastructure;

internal static class UserRoleExtensions
{
    internal static IEnumerable<UserRole> ApplyChangeInto(this UserRole role, UserRole[] roles) =>
            (roles.Contains(role) switch
            {
                true => roles.Except(new[] { role }),
                false => roles.Append(role)
            })
            .ToArray();

}

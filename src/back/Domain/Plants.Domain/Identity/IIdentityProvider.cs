﻿using Plants.Domain.Abstractions;

namespace Plants.Domain.Identity;

/// <summary>
/// Gets the identity of the caller
/// </summary>
public interface IIdentityProvider
{
    IUserIdentity? Identity { get; }
    void UpdateIdentity(IUserIdentity newIdentity);
}

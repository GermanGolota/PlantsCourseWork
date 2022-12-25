﻿using Plants.Aggregates.Infrastructure.Helper;
using Plants.Aggregates.Services;

namespace Plants.Aggregates.Infrastructure.Services;

internal class UserUpdater : IUserUpdater
{
    private readonly EventStoreUserUpdater _eventStore;
    private readonly MongoDbUserUpdater _mongoDb;

    public UserUpdater(EventStoreUserUpdater eventStore, MongoDbUserUpdater mongoDb)
    {
        _eventStore = eventStore;
        _mongoDb = mongoDb;
    }

    public async Task ChangeRole(string username, string fullName, UserRole[] oldRoles, UserRole changedRole)
    {
        await _eventStore.ChangeRole(username, fullName, oldRoles, changedRole);
        await _mongoDb.ChangeRole(username, fullName, oldRoles, changedRole);
    }

    public async Task Create(string username, string password, string fullName, UserRole[] roles)
    {
        await _eventStore.Create(username, password, fullName, roles);
        await _mongoDb.Create(username, password, fullName, roles);
    }

    public async Task UpdatePassword(string username, string oldPassword, string newPassword)
    {
        await _eventStore.UpdatePassword(username, oldPassword, newPassword);
        await _mongoDb.UpdatePassword(username, oldPassword, newPassword);
    }
}

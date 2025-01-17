﻿using EventStore.Client;
using EventStore.ClientAPI.Common.Log;
using EventStore.ClientAPI.UserManagement;
using Microsoft.Extensions.Options;
using System.Net;

namespace Plants.Aggregates.Infrastructure;

internal class EventStoreUserUpdater : IUserUpdater
{
    private readonly IEventStoreUserManagementClientFactory _factory;
    private readonly IIdentityProvider _identity;
    private readonly SymmetricEncrypter _encrypter;
    private readonly ConnectionConfig _config;

    public EventStoreUserUpdater(IEventStoreUserManagementClientFactory factory, IIdentityProvider identity, SymmetricEncrypter encrypter, IOptions<ConnectionConfig> options)
    {
        _factory = factory;
        _identity = identity;
        _encrypter = encrypter;
        _config = options.Value;
    }

    public async Task CreateAsync(string username, string password, string fullName, UserRole[] roles, CancellationToken token = default)
    {
        var groups = roles.Select(x => x.ToString()).Append("$admins").ToArray();
        await _factory.Create().CreateUserAsync(username, fullName, groups, password, userCredentials: GetCallerCreds(), cancellationToken: token);
    }

    private static bool _attachedCallback = false;

    public async Task ChangeRoleAsync(string username, string fullName, UserRole[] oldRoles, UserRole newRole, CancellationToken token = default)
    {
        var groups =
            newRole.ApplyChangeInto(oldRoles)
            .Select(_ => _.ToString())
            .ToArray();

        if (_attachedCallback is false)
        {

            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            _attachedCallback = true;
        }

        using (var httpClientHandler = new HttpClientHandler())
        {
            httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) =>
            {
                return true;
            };
            var creds = GetCallerCreds();
            var uri = new Uri(_config.EventStore.Template.Replace("esdb", "http"));
            var hostInfo = Dns.GetHostEntry(uri.Host);
            var manager = new UsersManager(
                new ConsoleLogger(),
                new IPEndPoint(hostInfo.AddressList[0], uri.Port),
                TimeSpan.FromSeconds(10),
                true,
                httpClientHandler
            );
            await manager.UpdateUserAsync(username, fullName, groups, new(creds.Username, creds.Password));
        }
    }

    public async Task UpdatePasswordAsync(string username, string oldPassword, string newPassword, CancellationToken token = default)
    {
        var creds = GetCallerCreds();
        await _factory.Create().ChangePasswordAsync(username, oldPassword, newPassword, userCredentials: creds, cancellationToken: token);
    }

    private UserCredentials GetCallerCreds()
    {
        var identity = _identity.Identity!;
        var pass = _encrypter.Decrypt(identity.Hash);
        return new UserCredentials(identity.UserName, pass);
    }

}

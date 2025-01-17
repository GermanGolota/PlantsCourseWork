﻿using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Plants.Aggregates.Infrastructure;

internal class MongoClientFactory : IMongoClientFactory
{
    private readonly IIdentityProvider _identity;
    private readonly SymmetricEncrypter _encrypter;
    private readonly ConnectionConfig _config;

    public MongoClientFactory(IIdentityProvider identity, IOptions<ConnectionConfig> options, SymmetricEncrypter encrypter)
    {
        _identity = identity;
        _encrypter = encrypter;
        _config = options.Value;
    }

    public MongoClient CreateClient()
    {
        var identity = _identity.Identity!;
        var connectionString = string.Format(_config.MongoDb.Template, identity.UserName, _encrypter.Decrypt(identity.Hash));
        return new MongoClient(connectionString);
    }

    public IMongoDatabase GetDatabase(string database) =>
        CreateClient().GetDatabase(database);
}

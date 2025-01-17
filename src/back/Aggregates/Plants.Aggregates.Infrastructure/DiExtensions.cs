﻿using Microsoft.Extensions.DependencyInjection;

namespace Plants.Aggregates.Infrastructure;

public static class DiExtensions
{
    public static IServiceCollection AddAggregatesInfrastructure(this IServiceCollection services)
    {
        services.AddDomainDependencies();
        services.AddScoped<TempPasswordContext>();
        services.AddScoped<IAuthorizer, Authorizer>();
        services.AddHttpContextAccessor();
        services.AddScoped<IIdentityProvider, IdentityProvider>();
        services.AddScoped<IIdentityHelper, IdentityHelper>();

        services.AddHttpClient();
        services.AddScoped<ElasticSearchHelper>();
        services.AddScoped<ElasticSearchUserUpdater>();
        services.AddScoped<EventStoreUserUpdater>();
        services.AddScoped<MongoDbUserUpdater>();
        services.AddScoped<IUserUpdater, UserUpdater>();

        services.AddSingleton<ILoggerInitializer, LoggerInitializer>();
        services.AddSingleton<IHealthChecker, HealthChecker>();

        services.AddMediatR(
            config =>
            {
                config.RegisterServicesFromAssemblyContaining<Plants.Aggregates.AssemblyMarker>();
            });

        return services;
    }

    private static IServiceCollection AddDomainDependencies(this IServiceCollection services)
    {
        services.AddScoped<EventStoreClientSettingsFactory>();
        services.AddScoped<IElasticSearchClientFactory, ElasticSearchClientFactory>();
        services.AddScoped<IMongoClientFactory, MongoClientFactory>();
        services.AddScoped<IEventStoreClientFactory, EventStoreClientFactory>();
        services.AddScoped<IEventStoreUserManagementClientFactory, EventStoreUserManagementClientFactory>();
        services.AddScoped<IEventStorePersistentSubscriptionsClientFactory, EventStorePersistentSubscriptionsClientFactory>();
        services.AddScoped<IServiceIdentityProvider, ServiceIdentityProvider>();

        return services;
    }

}

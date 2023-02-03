﻿using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using System.Text;

namespace Plants.Presentation;

public static class DIExtensions
{
    public static IServiceCollection AddWebRootFileProvider(this IServiceCollection services)
    {
        services.AddSingleton<IHostingContext, HostingContext>();
        services.AddSingleton(factory => factory.GetRequiredService<IWebHostEnvironment>().WebRootFileProvider);

        return services;
    }

    public static IServiceCollection AddPlantsConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.BindConfigSections(configuration, typeof(StartupCheckingConfigBinder<>));
        return services;
    }

    private class StartupCheckingConfigBinder<T> : GenericConfigBinder<T> where T : class
    {
        public StartupCheckingConfigBinder(IServiceCollection services, IConfiguration configSection, string optionName)
            : base(services, configSection, optionName)
        {
        }

        public override void AdditionalBinding(OptionsBuilder<T> builder)
        {
            builder.ValidateOnStart();
        }
    }

    public static IServiceCollection AddPlantsSwagger(this IServiceCollection services)
    {
        services.AddSwaggerExamplesFromAssemblyOf<LoginRequestExample>();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v2", new OpenApiInfo { Title = "Plants", Version = "2" });
            var securityScheme = new OpenApiSecurityScheme
            {
                Name = "JWT Authentication",
                Description = "Enter JWT Bearer token **_only_**",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Reference = new OpenApiReference
                {
                    Id = JwtBearerDefaults.AuthenticationScheme,
                    Type = ReferenceType.SecurityScheme
                }
            };
            c.AddSecurityDefinition(securityScheme.Reference.Id, securityScheme);
            var requiremenets = new OpenApiSecurityRequirement
            {
                {securityScheme, Array.Empty<string>()}
            };
            c.AddSecurityRequirement(requiremenets);
            c.ExampleFilters();
            c.ResolveConflictingActions(action => action.First());
        });

        return services;
    }

    public static IApplicationBuilder UsePlantsSwagger(this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v2/swagger.json", "Plants v2");
        });

        return app;
    }

    internal static IServiceCollection AddJwtAuthorization(this IServiceCollection services, IConfiguration config)
    {
        string key = GetAuthKey(config);
        services.AddAuthentication(x =>
        {
            x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
       .AddJwtBearer(x =>
       {
           x.RequireHttpsMetadata = false;
           x.SaveToken = true;
           x.TokenValidationParameters = GetValidationParams(key);
       });
        return services;
    }

    internal static TokenValidationParameters GetValidationParams(string key)
    {
        return new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(key)),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    }

    internal static string GetAuthKey(IConfiguration config)
    {
        return config
            .GetSection(AuthConfig.Section)!
            .Get<AuthConfig>()!
            .AuthKey;
    }
}

﻿using Dapper;
using Microsoft.EntityFrameworkCore;
using Plants.Application.Contracts;
using Plants.Application.Requests;
using Plants.Core;
using Plants.Infrastructure.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Plants.Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly PlantsContextFactory _ctx;

        public UserService(PlantsContextFactory contextFactory)
        {
            _ctx = contextFactory;
        }

        public async Task AddRole(string login, UserRole role)
        {
            var ctx = _ctx.CreateDbContext();
            await using (ctx)
            {
                var converter = new UserRoleConverter();
                var roleStr = converter.ConvertToProvider(role) as string;

                using (var connection = ctx.Database.GetDbConnection())
                {
                    string sql = "CALL add_user_to_group(@login, @role::UserRoles);";
                    var p = new
                    {
                        login,
                        role = roleStr
                    };
                    await connection.ExecuteAsync(sql, p);
                }
            }
        }

        public async Task RemoveRole(string login, UserRole role)
        {
            var ctx = _ctx.CreateDbContext();
            await using (ctx)
            {
                var converter = new UserRoleConverter();
                var roleStr = converter.ConvertToProvider(role) as string;

                using (var connection = ctx.Database.GetDbConnection())
                {
                    string sql = "CALL remove_user_from_group(@login, @role::UserRoles);";
                    var p = new
                    {
                        login,
                        role = roleStr
                    };
                    await connection.ExecuteAsync(sql, p);
                }
            }
        }

        public async Task<IEnumerable<FindUsersResultItem>> SearchFor(string FullName, string Contact, UserRole[] roles)
        {
            var ctx = _ctx.CreateDbContext();
            await using (ctx)
            {
                if (roles?.Any() == false)
                {
                    roles = null;
                }

                if (String.IsNullOrEmpty(FullName))
                {
                    FullName = null;
                }

                if (String.IsNullOrEmpty(Contact))
                {
                    Contact = null;
                }
                var converter = new UserRoleConverter();
                var Roles = roles?.Select(x => converter.ConvertToProvider(x) as string)?.ToArray();

                using (var connection = ctx.Database.GetDbConnection())
                {
                    string sql = "SELECT * FROM search_users(@FullName, @Contact, @Roles::UserRoles[])";
                    var p = new
                    {
                        FullName,
                        Contact,
                        Roles
                    };
                    return await connection.QueryAsync<FindUsersResultItem>(sql, p);
                }
            }
        }
    }
}
﻿using Plants.Application.Commands;
using Plants.Application.Requests;
using Plants.Core;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Plants.Application.Contracts
{
    public interface IUserService
    {
        Task<IEnumerable<FindUsersResultItem>> SearchFor(string FullName, string Contact, UserRole[] Roles);
        Task RemoveRole(string login, UserRole role);
        Task AddRole(string login, UserRole role);
        Task<CreateUserResult> CreateUser(string Login, List<UserRole> Roles, string FirstName, 
            string LastName, string PhoneNumber, string Password);
    }
}

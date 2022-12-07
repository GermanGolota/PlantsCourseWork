﻿using Plants.Application.Commands;
using Swashbuckle.AspNetCore.Filters;

namespace Plants.Presentation.Examples;

internal class LoginRequestExample : IExamplesProvider<LoginCommand>
{
    public LoginCommand GetExamples() =>
        new("postgres", "password");
}
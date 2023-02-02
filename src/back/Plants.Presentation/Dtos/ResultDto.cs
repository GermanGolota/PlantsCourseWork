﻿using Plants.Domain.Abstractions;
using Plants.Shared.Model;

namespace Plants.Presentation.Dtos;

public record ResultDto(bool Success, string Message);

public static class ResultDtoExtensions
{
    public static ResultDto ToResult(this OneOf<CommandAcceptedResult, CommandForbidden> cmdResult, string? successMessage = null)
    {
        successMessage ??= "Successfull";

        return cmdResult.Match(_ => new ResultDto(true, successMessage), fail => new ResultDto(false, String.Join('\n', fail.Reasons)));
    }
}
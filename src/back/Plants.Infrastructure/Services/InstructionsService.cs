﻿using Dapper;
using Microsoft.EntityFrameworkCore;
using Plants.Application.Contracts;
using Plants.Application.Requests;
using Plants.Infrastructure.Helpers;

namespace Plants.Infrastructure.Services;

public class InstructionsService : IInstructionsService
{
    private readonly PlantsContextFactory _ctx;

    public InstructionsService(PlantsContextFactory ctxFactory)
    {
        _ctx = ctxFactory;
    }

    public async Task<int> Create(long GroupId, string Text, string Title, string Description, byte[] CoverImage)
    {
        var ctx = _ctx.CreateDbContext();
        await using (ctx)
        {
            await using (var connection = ctx.Database.GetDbConnection())
            {
                string sql = $"SELECT * FROM create_instruction(@GroupId, @Text, @Title, @Description, @CoverImage);";
                var p = new
                {
                    GroupId,
                    Text,
                    Title,
                    Description,
                    CoverImage
                };
                var items = await connection.QueryAsync<int>(sql, p);
                return items.FirstOrDefault();
            }
        }
    }

    public async Task Edit(long InstructionId, long GroupId, string Text, string Title, string Description, byte[] CoverImage)
    {
        var ctx = _ctx.CreateDbContext();
        await using (ctx)
        {
            await using (var connection = ctx.Database.GetDbConnection())
            {
                string sql = $"CALL edit_instruction(@InstructionId, @GroupId, @Text, @Title, @Description, @CoverImage);";
                var p = new
                {
                    InstructionId,
                    GroupId,
                    Text,
                    Title,
                    Description,
                    CoverImage
                };
                await connection.ExecuteAsync(sql, p);
            }
        }
    }

    public async Task<GetInstructionResultItem> GetBy(long Id)
    {
        var ctx = _ctx.CreateDbContext();
        await using (ctx)
        {
            await using (var connection = ctx.Database.GetDbConnection())
            {
                string sql = $"SELECT * FROM instruction_v WHERE id = @Id;";
                var p = new
                {
                    Id
                };
                var items = await connection.QueryAsync<GetInstructionResultItem>(sql, p);
                var item = items.FirstOrDefault();
                GetInstructionResultItem result;
                if (item == default)
                {
                    result = null;
                }
                else
                {
                    result = item;
                }
                return result;
            }
        }
    }

    public async Task<IEnumerable<FindInstructionsResultItem>> GetFor(long GroupId, string Title, string Description)
    {
        var ctx = _ctx.CreateDbContext();
        await using (ctx)
        {
            await using (var connection = ctx.Database.GetDbConnection())
            {
                if (String.IsNullOrEmpty(Title))
                {
                    Title = null;
                }

                if (String.IsNullOrEmpty(Description))
                {
                    Description = null;
                }

                string sql = $"SELECT * FROM search_instructions(@GroupId, @Title, @Description);";
                var p = new
                {
                    GroupId,
                    Title,
                    Description
                };
                return await connection.QueryAsync<FindInstructionsResultItem>(sql, p);
            }
        }
    }
}

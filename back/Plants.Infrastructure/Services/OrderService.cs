﻿using Dapper;
using Microsoft.EntityFrameworkCore;
using Plants.Application.Contracts;
using Plants.Application.Requests;
using System.Linq;
using System.Threading.Tasks;

namespace Plants.Infrastructure.Services
{
    public class OrderService : IOrderService
    {
        private readonly PlantsContextFactory _ctx;

        public OrderService(PlantsContextFactory ctx)
        {
            _ctx = ctx;
        }

        public async Task<OrderResult> GetBy(int orderId)
        {
            var ctx = _ctx.CreateDbContext();
            await using (ctx)
            {
                await using (var connection = ctx.Database.GetDbConnection())
                {
                    string sql = "SELECT * FROM get_post(@orderId)";
                    var p = new
                    {
                        orderId = orderId
                    };
                    var res = await connection.QueryAsync<OrderResult>(sql, p);
                    return res.FirstOrDefault();
                }
            }
        }
    }
}
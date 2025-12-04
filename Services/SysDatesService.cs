// Services/SysDatesService.cs
using System.Data;
using Microsoft.EntityFrameworkCore;
using Starter.Api.Data;
using Starter.Api.DTOs.System;

namespace Starter.Api.Services;

public class SysDatesService : ISysDatesService
{
    private readonly AppDbContext _db;

    public SysDatesService(AppDbContext db)
    {
        _db = db;
    }

    private static void AddParam(IDbCommand cmd, string name, object? value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value ?? DBNull.Value;
        cmd.Parameters.Add(p);
    }

    public async Task<IReadOnlyList<SystemDto>> ListAsync(CancellationToken ct)
    {
        var result = new List<SystemDto>();

        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "dbo.SP_STARTER_SYSDATES_LIST";
        cmd.CommandType = CommandType.StoredProcedure;

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var dto = new SystemDto
            {
                Id             = reader.GetInt32(reader.GetOrdinal("Id")),
                SysCurrentDate = reader.GetDateTime(reader.GetOrdinal("SysCurrentDate")),
                SysClosedDate  = reader.IsDBNull(reader.GetOrdinal("SysClosedDate"))
                                  ? (DateTime?)null
                                  : reader.GetDateTime(reader.GetOrdinal("SysClosedDate")),
                SysName        = reader.IsDBNull(reader.GetOrdinal("SysName"))
                                  ? null
                                  : reader.GetString(reader.GetOrdinal("SysName"))
            };

            result.Add(dto);
        }

        return result;
    }
}

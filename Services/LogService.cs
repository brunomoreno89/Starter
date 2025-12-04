using System.Data;
using Microsoft.EntityFrameworkCore;
using Starter.Api.Data;
using Starter.Api.DTOs.Logs;

namespace Starter.Api.Services;

public class LogService : ILogService
{
    private readonly AppDbContext _db;

    public LogService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<LogListItemDto>> ListAsync(
        string? userTerm,
        DateTime startLocal00,
        DateTime endLocalNextDay00,
        int maxRows,
        CancellationToken ct)
    {
        var result = new List<LogListItemDto>();

        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "dbo.SP_STARTER_LOGS_LIST";
        cmd.CommandType = CommandType.StoredProcedure;

        void Add(string name, object? value)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }

        Add("@UserTerm", userTerm);
        Add("@StartLocal", startLocal00);
        Add("@EndLocal", endLocalNextDay00);
        Add("@MaxRows", maxRows);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var dto = new LogListItemDto
            {
                Id              = reader.GetInt32(reader.GetOrdinal("Id")),
                ExecDate        = reader.GetDateTime(reader.GetOrdinal("ExecDate")),
                UserId          = reader.IsDBNull(reader.GetOrdinal("UserId"))
                                    ? default
                                    : reader.GetInt32(reader.GetOrdinal("UserId")),
                Username        = reader.IsDBNull(reader.GetOrdinal("Username"))
                                    ? null
                                    : reader.GetString(reader.GetOrdinal("Username")),
                Name            = reader.IsDBNull(reader.GetOrdinal("Name"))
                                    ? null
                                    : reader.GetString(reader.GetOrdinal("Name")),
                RoleId          = reader.IsDBNull(reader.GetOrdinal("RoleId"))
                                    ? (int?)null
                                    : reader.GetInt32(reader.GetOrdinal("RoleId")),
                RoleName        = reader.IsDBNull(reader.GetOrdinal("RoleName"))
                                    ? null
                                    : reader.GetString(reader.GetOrdinal("RoleName")),
                PermissionId    = reader.IsDBNull(reader.GetOrdinal("PermissionId"))
                                    ? (int?)null
                                    : reader.GetInt32(reader.GetOrdinal("PermissionId")),
                PermissionName  = reader.IsDBNull(reader.GetOrdinal("PermissionName"))
                                    ? null
                                    : reader.GetString(reader.GetOrdinal("PermissionName")),
                Description     = reader.IsDBNull(reader.GetOrdinal("Description"))
                                    ? null
                                    : reader.GetString(reader.GetOrdinal("Description"))
            };

            result.Add(dto);
        }

        return result;
    }
}

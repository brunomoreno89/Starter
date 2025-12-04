// Services/RoleService.cs
using System.Data;
using Microsoft.EntityFrameworkCore;
using Starter.Api.Data;
using Starter.Api.DTOs.Security;

namespace Starter.Api.Services;

public class RoleService : IRoleService
{
    private readonly AppDbContext _db;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RoleService(AppDbContext db, IDateTimeProvider dateTimeProvider)
    {
        _db = db;
        _dateTimeProvider = dateTimeProvider;
    }

    private static void AddParam(IDbCommand cmd, string name, object? value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value ?? DBNull.Value;
        cmd.Parameters.Add(p);
    }

    private static bool HasColumn(IDataRecord reader, string columnName)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    public async Task<IReadOnlyList<RoleDto>> ListAsync(CancellationToken ct)
    {
        var result = new List<RoleDto>();

        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "dbo.SP_STARTER_ROLES_LIST";
        cmd.CommandType = CommandType.StoredProcedure;

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var dto = new RoleDto
            {
                Id          = reader.GetInt32(reader.GetOrdinal("Id")),
                Name        = reader.GetString(reader.GetOrdinal("Name")),
                Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                                ? null
                                : reader.GetString(reader.GetOrdinal("Description")),
                Active      = reader.IsDBNull(reader.GetOrdinal("Active"))
                                ? null
                                : reader.GetString(reader.GetOrdinal("Active")),
                CreationDt  = reader.GetDateTime(reader.GetOrdinal("CreationDt")),
                CreatedBy   = reader.IsDBNull(reader.GetOrdinal("CreatedBy"))
                                ? (int?)null
                                : reader.GetInt32(reader.GetOrdinal("CreatedBy")),
                UpdateDt    = reader.IsDBNull(reader.GetOrdinal("UpdateDt"))
                                ? (DateTime?)null
                                : reader.GetDateTime(reader.GetOrdinal("UpdateDt")),
                UpdatedBy   = reader.IsDBNull(reader.GetOrdinal("UpdatedBy"))
                                ? (int?)null
                                : reader.GetInt32(reader.GetOrdinal("UpdatedBy")),
                CreatedByName = HasColumn(reader, "CreatedByName") && !reader.IsDBNull(reader.GetOrdinal("CreatedByName"))
                                ? reader.GetString(reader.GetOrdinal("CreatedByName"))
                                : null,
                UpdatedByName = HasColumn(reader, "UpdatedByName") && !reader.IsDBNull(reader.GetOrdinal("UpdatedByName"))
                                ? reader.GetString(reader.GetOrdinal("UpdatedByName"))
                                : null
            };

            result.Add(dto);
        }

        return result;
    }

    public async Task<RoleDto?> GetByIdAsync(int id, CancellationToken ct)
    {
        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "dbo.SP_STARTER_ROLES_GETBYID";
        cmd.CommandType = CommandType.StoredProcedure;

        AddParam(cmd, "@Id", id);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
            return null;

        var dto = new RoleDto
        {
            Id          = reader.GetInt32(reader.GetOrdinal("Id")),
            Name        = reader.GetString(reader.GetOrdinal("Name")),
            Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("Description")),
            Active      = reader.IsDBNull(reader.GetOrdinal("Active"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("Active")),
            CreationDt  = reader.GetDateTime(reader.GetOrdinal("CreationDt")),
            CreatedBy   = reader.IsDBNull(reader.GetOrdinal("CreatedBy"))
                            ? (int?)null
                            : reader.GetInt32(reader.GetOrdinal("CreatedBy")),
            UpdateDt    = reader.IsDBNull(reader.GetOrdinal("UpdateDt"))
                            ? (DateTime?)null
                            : reader.GetDateTime(reader.GetOrdinal("UpdateDt")),
            UpdatedBy   = reader.IsDBNull(reader.GetOrdinal("UpdatedBy"))
                            ? (int?)null
                            : reader.GetInt32(reader.GetOrdinal("UpdatedBy")),
            CreatedByName = HasColumn(reader, "CreatedByName") && !reader.IsDBNull(reader.GetOrdinal("CreatedByName"))
                            ? reader.GetString(reader.GetOrdinal("CreatedByName"))
                            : null,
            UpdatedByName = HasColumn(reader, "UpdatedByName") && !reader.IsDBNull(reader.GetOrdinal("UpdatedByName"))
                            ? reader.GetString(reader.GetOrdinal("UpdatedByName"))
                            : null
        };

        return dto;
    }

    public async Task<bool> NameExistsAsync(string name, int? ignoreId, CancellationToken ct)
    {
        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "dbo.SP_STARTER_ROLES_EXISTS_NAME";
        cmd.CommandType = CommandType.StoredProcedure;

        AddParam(cmd, "@Name", name);
        AddParam(cmd, "@IgnoreId", ignoreId);

        var scalar = await cmd.ExecuteScalarAsync(ct);
        var count  = (scalar == null || scalar == DBNull.Value) ? 0 : Convert.ToInt32(scalar);
        return count > 0;
    }

    public async Task<RoleDto> CreateAsync(
        RoleCreateDto dto,
        int? currentUserId,
        CancellationToken ct)
    {
        var now = _dateTimeProvider.NowLocal;

        var name = dto.Name!.Trim();
        var desc = string.IsNullOrWhiteSpace(dto.Description)
            ? null
            : dto.Description.Trim();

        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "dbo.SP_STARTER_ROLES_CREATE";
        cmd.CommandType = CommandType.StoredProcedure;

        AddParam(cmd, "@Name", name);
        AddParam(cmd, "@Description", desc);
        AddParam(cmd, "@Active", "Yes");
        AddParam(cmd, "@CreationDt", now);
        AddParam(cmd, "@CreatedBy", currentUserId);

        var scalar = await cmd.ExecuteScalarAsync(ct);
        var newId = Convert.ToInt32(scalar);

        var created = await GetByIdAsync(newId, ct);
        if (created == null)
        {
            created = new RoleDto
            {
                Id          = newId,
                Name        = name,
                Description = desc,
                Active      = "Yes",
                CreationDt  = now,
                CreatedBy   = currentUserId
            };
        }

        return created;
    }

    public async Task<RoleDto?> UpdateAsync(
        int id,
        RoleUpdateDto dto,
        int? currentUserId,
        CancellationToken ct)
    {
        var existing = await GetByIdAsync(id, ct);
        if (existing == null) return null;

        var name = string.IsNullOrWhiteSpace(dto.Name)
            ? existing.Name
            : dto.Name!.Trim();

        var desc = dto.Description == null
            ? existing.Description
            : (string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim());

        var active = dto.Active == null
            ? existing.Active
            : dto.Active.Trim();

        if (dto.Active != null)
        {
            var v   = dto.Active.Trim();
            var yes = v.Equals("Yes", StringComparison.OrdinalIgnoreCase);
            var no  = v.Equals("No" , StringComparison.OrdinalIgnoreCase);
            if (!yes && !no)
                throw new InvalidOperationException("Active must be 'Yes' or 'No'.");
            active = yes ? "Yes" : "No";
        }

        var now = _dateTimeProvider.NowLocal;

        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "dbo.SP_STARTER_ROLES_UPDATE";
        cmd.CommandType = CommandType.StoredProcedure;

        AddParam(cmd, "@Id", id);
        AddParam(cmd, "@Name", name);
        AddParam(cmd, "@Description", desc);
        AddParam(cmd, "@Active", active);
        AddParam(cmd, "@UpdatedBy", currentUserId);
        AddParam(cmd, "@UpdateDt", now);

        var scalar = await cmd.ExecuteScalarAsync(ct);
        var rows   = (scalar == null || scalar == DBNull.Value) ? 0 : Convert.ToInt32(scalar);
        if (rows <= 0) return null;

        var updated = await GetByIdAsync(id, ct);
        return updated;
    }

    public async Task<bool> SoftDeleteAsync(
        int id,
        int? currentUserId,
        CancellationToken ct)
    {
        var now = _dateTimeProvider.NowLocal;

        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "dbo.SP_STARTER_ROLES_SOFTDELETE";
        cmd.CommandType = CommandType.StoredProcedure;

        AddParam(cmd, "@Id", id);
        AddParam(cmd, "@UpdatedBy", currentUserId);
        AddParam(cmd, "@UpdateDt", now);

        var scalar = await cmd.ExecuteScalarAsync(ct);
        var rows   = (scalar == null || scalar == DBNull.Value) ? 0 : Convert.ToInt32(scalar);
        return rows > 0;
    }
}

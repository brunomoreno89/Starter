using System.Data;
using Microsoft.EntityFrameworkCore;
using Starter.Api.Data;
using Starter.Api.DTOs.Security;

namespace Starter.Api.Services;

public class PermissionService : IPermissionService
{
    private readonly AppDbContext _db;
    private readonly IDateTimeProvider _dateTimeProvider;

    public PermissionService(AppDbContext db, IDateTimeProvider dateTimeProvider)
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

    public async Task<IReadOnlyList<PermissionDto>> ListAsync(CancellationToken ct)
    {
        var result = new List<PermissionDto>();

        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "dbo.SP_STARTER_PERMISSIONS_LIST";
        cmd.CommandType = CommandType.StoredProcedure;

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var dto = new PermissionDto
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

    public async Task<PermissionDto?> GetByIdAsync(int id, CancellationToken ct)
    {
        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "dbo.SP_STARTER_PERMISSIONS_GETBYID";
        cmd.CommandType = CommandType.StoredProcedure;

        AddParam(cmd, "@Id", id);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
            return null;

        var dto = new PermissionDto
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
        cmd.CommandText = "dbo.SP_STARTER_PERMISSIONS_EXISTS_NAME";
        cmd.CommandType = CommandType.StoredProcedure;

        AddParam(cmd, "@Name", name);
        AddParam(cmd, "@IgnoreId", ignoreId);

        var scalar = await cmd.ExecuteScalarAsync(ct);
        var count  = (scalar == null || scalar == DBNull.Value) ? 0 : Convert.ToInt32(scalar);
        return count > 0;
    }

    public async Task<PermissionDto> CreateAsync(
        PermissionCreateDto dto,
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
        cmd.CommandText = "dbo.SP_STARTER_PERMISSIONS_CREATE";
        cmd.CommandType = CommandType.StoredProcedure;

        AddParam(cmd, "@Name", name);
        AddParam(cmd, "@Description", desc);
        AddParam(cmd, "@Active", "Yes");
        AddParam(cmd, "@CreationDt", now);
        AddParam(cmd, "@CreatedBy", currentUserId);

        // SP retorna SCOPE_IDENTITY() como NewId
        var scalar = await cmd.ExecuteScalarAsync(ct);
        var newId = Convert.ToInt32(scalar);

        var created = await GetByIdAsync(newId, ct);
        if (created == null)
        {
            created = new PermissionDto
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

    public async Task<PermissionDto?> UpdateAsync(
        int id,
        PermissionUpdateDto dto,
        int? currentUserId,
        CancellationToken ct)
    {
        // Busca atual para mesclar campos se necessário (se o DTO for parcial)
        var existing = await GetByIdAsync(id, ct);
        if (existing == null) return null;

        var name = string.IsNullOrWhiteSpace(dto.Name)
            ? existing.Name
            : dto.Name.Trim();

        var desc = dto.Description == null
            ? existing.Description
            : (string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim());

        var active = dto.Active == null
            ? existing.Active
            : dto.Active.Trim();

        // valida Active se veio (Yes/No)
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
        cmd.CommandText = "dbo.SP_STARTER_PERMISSIONS_UPDATE";
        cmd.CommandType = CommandType.StoredProcedure;

        AddParam(cmd, "@Id", id);
        AddParam(cmd, "@Name", name);
        AddParam(cmd, "@Description", desc);
        AddParam(cmd, "@Active", active);
        AddParam(cmd, "@UpdatedBy", currentUserId);
        AddParam(cmd, "@UpdateDt", now);

        // SP faz UPDATE e SELECT @@ROWCOUNT AS RowsAffected
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
        cmd.CommandText = "dbo.SP_STARTER_PERMISSIONS_SOFTDELETE";
        cmd.CommandType = CommandType.StoredProcedure;

        AddParam(cmd, "@Id", id);
        AddParam(cmd, "@UpdatedBy", currentUserId);
        AddParam(cmd, "@UpdateDt", now);

        var scalar = await cmd.ExecuteScalarAsync(ct);
        var rows   = (scalar == null || scalar == DBNull.Value) ? 0 : Convert.ToInt32(scalar);
        return rows > 0;
    }
}

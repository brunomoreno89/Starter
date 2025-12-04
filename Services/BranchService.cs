// Services/BranchService.cs
using System.Data;
using Microsoft.EntityFrameworkCore;
using Starter.Api.Data;
using Starter.Api.DTOs.Branches;

namespace Starter.Api.Services;

public class BranchService : IBranchService
{
    private readonly AppDbContext _db;
    private readonly IDateTimeProvider _dateTimeProvider;

    public BranchService(AppDbContext db, IDateTimeProvider dateTimeProvider)
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

    public async Task<IReadOnlyList<BranchesDto>> ListAsync(CancellationToken ct)
    {
        var result = new List<BranchesDto>();

        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "dbo.SP_STARTER_BRANCHES_LIST";
        cmd.CommandType = CommandType.StoredProcedure;

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var dto = new BranchesDto
            {
                Id              = reader.GetInt32(reader.GetOrdinal("Id")),
                BranchCode      = reader.GetString(reader.GetOrdinal("BranchCode")),
                Description     = reader.IsDBNull(reader.GetOrdinal("Description"))
                                   ? null
                                   : reader.GetString(reader.GetOrdinal("Description")),
                CreatedAt       = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                CreatedByUserId = reader.IsDBNull(reader.GetOrdinal("CreatedByUserId"))
                                   ? (int?)null
                                   : reader.GetInt32(reader.GetOrdinal("CreatedByUserId")),
                UpdatedAt       = reader.IsDBNull(reader.GetOrdinal("UpdatedAt"))
                                   ? (DateTime?)null
                                   : reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                UpdatedByUserId = reader.IsDBNull(reader.GetOrdinal("UpdatedByUserId"))
                                   ? (int?)null
                                   : reader.GetInt32(reader.GetOrdinal("UpdatedByUserId")),
                Active          = reader.IsDBNull(reader.GetOrdinal("Active"))
                                   ? null
                                   : reader.GetString(reader.GetOrdinal("Active")),
                RegionId        = reader.GetInt32(reader.GetOrdinal("RegionId")),
                CreatedByName   = HasColumn(reader, "CreatedByName") &&
                                  !reader.IsDBNull(reader.GetOrdinal("CreatedByName"))
                                   ? reader.GetString(reader.GetOrdinal("CreatedByName"))
                                   : null,
                UpdatedByName   = HasColumn(reader, "UpdatedByName") &&
                                  !reader.IsDBNull(reader.GetOrdinal("UpdatedByName"))
                                   ? reader.GetString(reader.GetOrdinal("UpdatedByName"))
                                   : null
            };

            result.Add(dto);
        }

        return result;
    }

    public async Task<BranchesDto?> GetByIdAsync(int id, CancellationToken ct)
    {
        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "dbo.SP_STARTER_BRANCHES_GETBYID";
        cmd.CommandType = CommandType.StoredProcedure;

        AddParam(cmd, "@Id", id);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
            return null;

        var dto = new BranchesDto
        {
            Id              = reader.GetInt32(reader.GetOrdinal("Id")),
            BranchCode      = reader.GetString(reader.GetOrdinal("BranchCode")),
            Description     = reader.IsDBNull(reader.GetOrdinal("Description"))
                               ? null
                               : reader.GetString(reader.GetOrdinal("Description")),
            CreatedAt       = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            CreatedByUserId = reader.IsDBNull(reader.GetOrdinal("CreatedByUserId"))
                               ? (int?)null
                               : reader.GetInt32(reader.GetOrdinal("CreatedByUserId")),
            UpdatedAt       = reader.IsDBNull(reader.GetOrdinal("UpdatedAt"))
                               ? (DateTime?)null
                               : reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
            UpdatedByUserId = reader.IsDBNull(reader.GetOrdinal("UpdatedByUserId"))
                               ? (int?)null
                               : reader.GetInt32(reader.GetOrdinal("UpdatedByUserId")),
            Active          = reader.IsDBNull(reader.GetOrdinal("Active"))
                               ? null
                               : reader.GetString(reader.GetOrdinal("Active")),
            RegionId        = reader.GetInt32(reader.GetOrdinal("RegionId")),
            CreatedByName   = HasColumn(reader, "CreatedByName") &&
                              !reader.IsDBNull(reader.GetOrdinal("CreatedByName"))
                               ? reader.GetString(reader.GetOrdinal("CreatedByName"))
                               : null,
            UpdatedByName   = HasColumn(reader, "UpdatedByName") &&
                              !reader.IsDBNull(reader.GetOrdinal("UpdatedByName"))
                               ? reader.GetString(reader.GetOrdinal("UpdatedByName"))
                               : null
        };

        return dto;
    }

    public async Task<BranchesDto> CreateAsync(
        BranchesDto dto,
        int? currentUserId,
        CancellationToken ct)
    {
        var now = _dateTimeProvider.NowLocal;

        var code = dto.BranchCode?.Trim();
        var desc = string.IsNullOrWhiteSpace(dto.Description)
            ? null
            : dto.Description.Trim();

        var active = "Yes";

        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "dbo.SP_STARTER_BRANCHES_CREATE";
        cmd.CommandType = CommandType.StoredProcedure;

        AddParam(cmd, "@BranchCode"     , code);
        AddParam(cmd, "@Description"    , desc);
        AddParam(cmd, "@RegionId"       , dto.RegionId);
        AddParam(cmd, "@CreatedAt"      , now);
        AddParam(cmd, "@CreatedByUserId", currentUserId);
        AddParam(cmd, "@Active"         , active);

        var scalar = await cmd.ExecuteScalarAsync(ct);
        var newId  = Convert.ToInt32(scalar);

        var created = await GetByIdAsync(newId, ct);
        if (created == null)
        {
            created = new BranchesDto
            {
                Id              = newId,
                BranchCode      = code,
                Description     = desc,
                RegionId        = dto.RegionId,
                CreatedAt       = now,
                CreatedByUserId = currentUserId,
                Active          = active
            };
        }

        return created;
    }

    public async Task<BranchesDto?> UpdateAsync(
        int id,
        BranchesDto dto,
        int? currentUserId,
        CancellationToken ct)
    {
        // Busca atual pra mesclar campos / validar existência
        var existing = await GetByIdAsync(id, ct);
        if (existing == null) return null;

        var code = string.IsNullOrWhiteSpace(dto.BranchCode)
            ? existing.BranchCode
            : dto.BranchCode!.Trim();

        var desc = dto.Description == null
            ? existing.Description
            : (string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim());

        var regionId = dto.RegionId == 0
            ? existing.RegionId
            : dto.RegionId;

        var active = dto.Active ?? existing.Active;

        if (active != null)
        {
            var v   = active.Trim();
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
        cmd.CommandText = "dbo.SP_STARTER_BRANCHES_UPDATE";
        cmd.CommandType = CommandType.StoredProcedure;

        AddParam(cmd, "@Id"             , id);
        AddParam(cmd, "@BranchCode"     , code);
        AddParam(cmd, "@Description"    , desc);
        AddParam(cmd, "@RegionId"       , regionId);
        AddParam(cmd, "@UpdatedAt"      , now);
        AddParam(cmd, "@UpdatedByUserId", currentUserId);
        AddParam(cmd, "@Active"         , active);

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
        cmd.CommandText = "dbo.SP_STARTER_BRANCHES_SOFTDELETE";
        cmd.CommandType = CommandType.StoredProcedure;

        AddParam(cmd, "@Id"             , id);
        AddParam(cmd, "@UpdatedAt"      , now);
        AddParam(cmd, "@UpdatedByUserId", currentUserId);

        var scalar = await cmd.ExecuteScalarAsync(ct);
        var rows   = (scalar == null || scalar == DBNull.Value) ? 0 : Convert.ToInt32(scalar);
        return rows > 0;
    }
}

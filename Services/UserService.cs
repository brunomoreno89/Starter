using System.Data;
using Microsoft.EntityFrameworkCore;
using Starter.Api.Data;
using Starter.Api.DTOs.Users;
using Starter.Api.Security; // PasswordHasher
using Starter.Api.Services;
using Starter.Api.Extensions; // se você tiver o HasColumn; se não, tiro já já

namespace Starter.Api.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _db;
    private readonly PasswordHasher _hasher;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UserService(
        AppDbContext db,
        PasswordHasher hasher,
        IDateTimeProvider dateTimeProvider)
    {
        _db = db;
        _hasher = hasher;
        _dateTimeProvider = dateTimeProvider;
    }

    private static void AddParam(IDbCommand cmd, string name, object? value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value ?? DBNull.Value;
        cmd.Parameters.Add(p);
    }

    public async Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken ct)
    {
        var result = new List<UserDto>();

        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "dbo.SP_STARTER_USERS_LIST";
        cmd.CommandType = CommandType.StoredProcedure;

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var dto = new UserDto
            {
                Id         = reader.GetInt32(reader.GetOrdinal("Id")),
                Username   = reader.GetString(reader.GetOrdinal("Username")),
                Name       = reader.IsDBNull(reader.GetOrdinal("Name"))
                                ? null
                                : reader.GetString(reader.GetOrdinal("Name")),
                Email      = reader.IsDBNull(reader.GetOrdinal("Email"))
                                ? null
                                : reader.GetString(reader.GetOrdinal("Email")),
                Active     = reader.IsDBNull(reader.GetOrdinal("Active"))
                                ? null
                                : reader.GetString(reader.GetOrdinal("Active")),
                CreationDt = reader.GetDateTime(reader.GetOrdinal("CreationDt")),
                CreatedBy  = reader.IsDBNull(reader.GetOrdinal("CreatedBy"))
                                ? (int?)null
                                : reader.GetInt32(reader.GetOrdinal("CreatedBy")),
                UpdatedDt  = reader.IsDBNull(reader.GetOrdinal("UpdatedDt"))
                                ? (DateTime?)null
                                : reader.GetDateTime(reader.GetOrdinal("UpdatedDt")),
                UpdatedBy  = reader.IsDBNull(reader.GetOrdinal("UpdatedBy"))
                                ? (int?)null
                                : reader.GetInt32(reader.GetOrdinal("UpdatedBy")),

                // Esses campos podem ou não existir, dependendo de como você montar a SP
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

    public async Task<UserDto?> GetByIdAsync(int id, CancellationToken ct)
    {
        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "dbo.SP_STARTER_USERS_GETBYID";
        cmd.CommandType = CommandType.StoredProcedure;

        AddParam(cmd, "@Id", id);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
            return null;

        var dto = new UserDto
        {
            Id         = reader.GetInt32(reader.GetOrdinal("Id")),
            Username   = reader.GetString(reader.GetOrdinal("Username")),
            Name       = reader.IsDBNull(reader.GetOrdinal("Name"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("Name")),
            Email      = reader.IsDBNull(reader.GetOrdinal("Email"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("Email")),
            Active     = reader.IsDBNull(reader.GetOrdinal("Active"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("Active")),
            CreationDt = reader.GetDateTime(reader.GetOrdinal("CreationDt")),
            CreatedBy  = reader.IsDBNull(reader.GetOrdinal("CreatedBy"))
                            ? (int?)null
                            : reader.GetInt32(reader.GetOrdinal("CreatedBy")),
            UpdatedDt  = reader.IsDBNull(reader.GetOrdinal("UpdatedDt"))
                            ? (DateTime?)null
                            : reader.GetDateTime(reader.GetOrdinal("UpdatedDt")),
            UpdatedBy  = reader.IsDBNull(reader.GetOrdinal("UpdatedBy"))
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

    public async Task<UserDto> CreateAsync(UserCreateDto dto, int? currentUserId, CancellationToken ct)
    {
        var now = _dateTimeProvider.NowLocal;
        var hash = _hasher.Hash(dto.Password);

        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "dbo.SP_STARTER_USERS_CREATE";
        cmd.CommandType = CommandType.StoredProcedure;

        AddParam(cmd, "@Username", dto.Username.Trim());
        AddParam(cmd, "@Name", string.IsNullOrWhiteSpace(dto.Name) ? null : dto.Name.Trim());
        AddParam(cmd, "@Email", dto.Email.Trim());
        AddParam(cmd, "@PasswordHash", hash);
        AddParam(cmd, "@Active", "Yes");
        AddParam(cmd, "@CreationDt", now);
        AddParam(cmd, "@CreatedBy", currentUserId);

        // SP deve dar um SELECT SCOPE_IDENTITY() AS NewId;
        var scalar = await cmd.ExecuteScalarAsync(ct);
        var newId = Convert.ToInt32(scalar);

        // Reaproveita GetByIdAsync para montar DTO completo (com nomes, etc.)
        var created = await GetByIdAsync(newId, ct);
        if (created == null)
        {
            // fallback simples se algo muito estranho acontecer
            created = new UserDto
            {
                Id         = newId,
                Username   = dto.Username.Trim(),
                Name       = string.IsNullOrWhiteSpace(dto.Name) ? null : dto.Name.Trim(),
                Email      = dto.Email.Trim(),
                Active     = "Yes",
                CreationDt = now,
                CreatedBy  = currentUserId
            };
        }

        return created;
    }

    public async Task<UserDto?> UpdateAsync(int id, UserUpdateDto body, int? currentUserId, CancellationToken ct)
    {
        // Para reaproveitar a lógica de "campos opcionais",
        // buscamos primeiro o registro atual
        var existing = await GetByIdAsync(id, ct);
        if (existing == null) return null;

        // Aplica a mesma lógica de patches que você tinha no controller
        var username = existing.Username;
        var name     = existing.Name;
        var email    = existing.Email;
        var active   = existing.Active;
        string? passwordHash = null; // só recalculamos se vier nova senha

        if (!string.IsNullOrWhiteSpace(body.Username))
            username = body.Username.Trim();

        if (body.Name != null) 
            name = string.IsNullOrWhiteSpace(body.Name) ? null : body.Name.Trim();

        if (!string.IsNullOrWhiteSpace(body.Email))
            email = body.Email.Trim();

        if (!string.IsNullOrWhiteSpace(body.Password))
            passwordHash = _hasher.Hash(body.Password);

        if (!string.IsNullOrWhiteSpace(body.Active))
        {
            var v   = body.Active.Trim();
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
        cmd.CommandText = "dbo.SP_STARTER_USERS_UPDATE";
        cmd.CommandType = CommandType.StoredProcedure;

        AddParam(cmd, "@Id", id);
        AddParam(cmd, "@Username", username);
        AddParam(cmd, "@Name", name);
        AddParam(cmd, "@Email", email);
        AddParam(cmd, "@Active", active);
        AddParam(cmd, "@UpdatedBy", currentUserId);
        AddParam(cmd, "@UpdatedDt", now);
        AddParam(cmd, "@PasswordHash", (object?)passwordHash ?? DBNull.Value);

        // SP deve fazer UPDATE e no final: SELECT @@ROWCOUNT AS RowsAffected;
        var scalar = await cmd.ExecuteScalarAsync(ct);
        var rows   = (scalar == null || scalar == DBNull.Value) ? 0 : Convert.ToInt32(scalar);
        if (rows <= 0) return null; // alguém deletou ou mudou o registro no meio do caminho

        // Retorna DTO atualizado
        var updated = await GetByIdAsync(id, ct);
        return updated;
    }

    public async Task<bool> SoftDeleteAsync(int id, int? currentUserId, CancellationToken ct)
    {
        var now = _dateTimeProvider.NowLocal;

        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "dbo.SP_STARTER_USERS_SOFTDELETE";
        cmd.CommandType = CommandType.StoredProcedure;

        AddParam(cmd, "@Id", id);
        AddParam(cmd, "@UpdatedBy", currentUserId);
        AddParam(cmd, "@UpdatedDt", now);

        var scalar = await cmd.ExecuteScalarAsync(ct);
        var rows   = (scalar == null || scalar == DBNull.Value) ? 0 : Convert.ToInt32(scalar);
        return rows > 0;
    }

    public async Task<bool> UsernameExistsAsync(string username, int? ignoreId, CancellationToken ct)
    {
        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "dbo.SP_STARTER_USERS_EXISTS_USERNAME";
        cmd.CommandType = CommandType.StoredProcedure;

        AddParam(cmd, "@Username", username);
        AddParam(cmd, "@IgnoreId", ignoreId);

        var scalar = await cmd.ExecuteScalarAsync(ct);
        var count  = (scalar == null || scalar == DBNull.Value) ? 0 : Convert.ToInt32(scalar);
        return count > 0;
    }

    public async Task<bool> EmailExistsAsync(string email, int? ignoreId, CancellationToken ct)
    {
        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "dbo.SP_STARTER_USERS_EXISTS_EMAIL";
        cmd.CommandType = CommandType.StoredProcedure;

        AddParam(cmd, "@Email", email);
        AddParam(cmd, "@IgnoreId", ignoreId);

        var scalar = await cmd.ExecuteScalarAsync(ct);
        var count  = (scalar == null || scalar == DBNull.Value) ? 0 : Convert.ToInt32(scalar);
        return count > 0;
    }

    // Helper local se você ainda não tiver o DataReaderExtensions.HasColumn
    private static bool HasColumn(IDataRecord reader, string columnName)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}

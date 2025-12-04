// Services/UserRoleService.cs
using System.Data;
using Microsoft.EntityFrameworkCore;
using Starter.Api.Data;
using Starter.Api.DTOs.Security;

namespace Starter.Api.Services;

public class UserRoleService : IUserRoleService
{
    private readonly AppDbContext _db;

    public UserRoleService(AppDbContext db)
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

    public async Task<bool> UserExistsAsync(int userId, CancellationToken ct)
    {
        // Validação simples usando EF (se quiser, depois dá pra trocar por SP)
        return await _db.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == userId, ct);
    }

    public async Task<IReadOnlyList<RoleDto>> GetByUserAsync(
        int userId,
        CancellationToken ct)
    {
        var result = new List<RoleDto>();

        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "dbo.SP_STARTER_USERROLES_GETBYUSER";
        cmd.CommandType = CommandType.StoredProcedure;

        AddParam(cmd, "@UserId", userId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var dto = new RoleDto
            {
                Id          = reader.GetInt32(reader.GetOrdinal("Id")),
                Name        = reader.GetString(reader.GetOrdinal("Name")),
                Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                                ? null
                                : reader.GetString(reader.GetOrdinal("Description"))
            };

            result.Add(dto);
        }

        return result;
    }

    public async Task<(int oldCount, int newCount)> AssignAsync(
        UserRoleAssignDto dto,
        CancellationToken ct)
    {
        // 1) Valida roles solicitadas (usando EF, igual no código original)
        var requestedIds = dto.RoleIds?.Distinct().ToArray() ?? Array.Empty<int>();

        var validIds = await _db.Roles
            .AsNoTracking()
            .Where(r => requestedIds.Contains(r.Id))
            .Select(r => r.Id)
            .ToListAsync(ct);

        if (validIds.Count != requestedIds.Length)
            throw new InvalidOperationException("One or more profiles are not valid.");

        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);

        await using var tx = await conn.BeginTransactionAsync(ct);

        try
        {
            int oldCount;

            // 2) CLEAR: remove todas as roles atuais do usuário
            await using (var clearCmd = conn.CreateCommand())
            {
                clearCmd.Transaction = tx;
                clearCmd.CommandText = "dbo.SP_STARTER_USERROLES_CLEAR";
                clearCmd.CommandType = CommandType.StoredProcedure;

                AddParam(clearCmd, "@UserId", dto.UserId);

                var scalar = await clearCmd.ExecuteScalarAsync(ct);
                oldCount = (scalar == null || scalar == DBNull.Value)
                    ? 0
                    : Convert.ToInt32(scalar);
            }

            // 3) INSERT: insere cada RoleId válido
            foreach (var rid in validIds)
            {
                await using var insCmd = conn.CreateCommand();
                insCmd.Transaction = tx;
                insCmd.CommandText = "dbo.SP_STARTER_USERROLES_INSERT";
                insCmd.CommandType = CommandType.StoredProcedure;

                AddParam(insCmd, "@UserId", dto.UserId);
                AddParam(insCmd, "@RoleId", rid);

                await insCmd.ExecuteNonQueryAsync(ct);
            }

            await tx.CommitAsync(ct);

            var newCount = validIds.Count;
            return (oldCount, newCount);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}

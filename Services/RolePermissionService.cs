// Services/RolePermissionService.cs
using System.Data;
using Microsoft.EntityFrameworkCore;
using Starter.Api.Data;
using Starter.Api.DTOs.Security;

namespace Starter.Api.Services;

public class RolePermissionService : IRolePermissionService
{
    private readonly AppDbContext _db;

    public RolePermissionService(AppDbContext db)
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

    public async Task<bool> RoleExistsAsync(int roleId, CancellationToken ct)
    {
        // Usa a SP de Roles já existente: SP_STARTER_ROLES_GETBYID
        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "dbo.SP_STARTER_ROLES_GETBYID";
        cmd.CommandType = CommandType.StoredProcedure;
        AddParam(cmd, "@Id", roleId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var exists = await reader.ReadAsync(ct);
        return exists;
    }

    public async Task<IReadOnlyList<PermissionDto>> GetByRoleAsync(
        int roleId,
        CancellationToken ct)
    {
        var result = new List<PermissionDto>();

        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "dbo.SP_STARTER_ROLEPERMISSIONS_GETBYROLE";
        cmd.CommandType = CommandType.StoredProcedure;

        AddParam(cmd, "@RoleId", roleId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var dto = new PermissionDto
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
        RolePermissionAssignDto dto,
        CancellationToken ct)
    {
        // 1) Valida IDs de permissão (continua usando EF aqui para simplicidade)
        var requestedIds = dto.PermissionIds?.Distinct().ToArray() ?? Array.Empty<int>();

        if (requestedIds.Length == 0)
        {
            // estratégia: permitir "zerar" permissões
            // (se quiser bloquear, lance InvalidOperationException aqui)
        }

        var validIds = await _db.Permissions
            .AsNoTracking()
            .Where(p => requestedIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync(ct);

        if (validIds.Count != requestedIds.Length)
            throw new InvalidOperationException("One or more permissions are not valid.");

        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);

        // Transação para garantir atomicidade do CLEAR + INSERTs
        await using var tx = await conn.BeginTransactionAsync(ct);

        try
        {
            int oldCount;

            // 2) CLEAR: remove todas as permissões atuais do role
            await using (var clearCmd = conn.CreateCommand())
            {
                clearCmd.Transaction = tx;
                clearCmd.CommandText = "dbo.SP_STARTER_ROLEPERMISSIONS_CLEAR";
                clearCmd.CommandType = CommandType.StoredProcedure;

                AddParam(clearCmd, "@RoleId", dto.RoleId);

                var scalar = await clearCmd.ExecuteScalarAsync(ct);
                oldCount = (scalar == null || scalar == DBNull.Value)
                    ? 0
                    : Convert.ToInt32(scalar);
            }

            // 3) INSERT: insere cada PermissionId válido
            foreach (var pid in validIds)
            {
                await using var insCmd = conn.CreateCommand();
                insCmd.Transaction = tx;
                insCmd.CommandText = "dbo.SP_STARTER_ROLEPERMISSIONS_INSERT";
                insCmd.CommandType = CommandType.StoredProcedure;

                AddParam(insCmd, "@RoleId", dto.RoleId);
                AddParam(insCmd, "@PermissionId", pid);

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

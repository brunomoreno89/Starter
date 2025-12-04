/*
    1.3. ItemService (implementação que fala com o banco)
        Arquivo: Services/ItemService.cs
        Função: implementar como os Items são persistidos / lidos.
        Aqui é que entra:
            AppDbContext (pra pegar a connection do EF).
            Stored procedures (SP_STARTER_ITEMS_LIST, SP_STARTER_ITEMS_CREATE, etc.).
            Mapeamento manual de DataReader → ItemDto.
            O ItemService faz o “trabalho sujo”:
                Abre conexão: Database.GetDbConnection().
                Cria DbCommand.
                Define CommandType.StoredProcedure.
                Adiciona parâmetros da SP.
                Executa (ExecuteReaderAsync, ExecuteScalarAsync).
                Constrói ItemDto a partir das colunas retornadas.
            Ou seja, é aqui que você concentra:
                nome das SPs
                nomes dos parâmetros
                nomes das colunas retornadas
                E mantém o resto da aplicação “limpo”.
*/

using System.Data;
using Microsoft.EntityFrameworkCore;
using Starter.Api.Data;
using Starter.Api.DTOs.Items;
using Starter.Api.Extensions; // DataReaderExtensions

// Este cara usa SQL Server + suas stored procedures SP_STARTER_ITEMS_*.


namespace Starter.Api.Services;

public class ItemService : IItemService
{
    private readonly AppDbContext _db;

    public ItemService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ItemDto>> ListAsync(CancellationToken ct)
    {
        var result = new List<ItemDto>();

        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "dbo.SP_STARTER_ITEMS_LIST";
        cmd.CommandType = CommandType.StoredProcedure;

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var dto = new ItemDto
            {
                Id              = reader.GetInt32(reader.GetOrdinal("Id")),
                Name            = reader.GetString(reader.GetOrdinal("Name")),
                Description     = reader.IsDBNull(reader.GetOrdinal("Description"))
                                    ? null
                                    : reader.GetString(reader.GetOrdinal("Description")),
                Active          = reader.IsDBNull(reader.GetOrdinal("Active"))
                                    ? null
                                    : reader.GetString(reader.GetOrdinal("Active")),
                CreatedAt       = reader.IsDBNull(reader.GetOrdinal("CreatedAt"))
                                    ? default
                                    : reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                UpdatedAt       = reader.IsDBNull(reader.GetOrdinal("UpdatedAt"))
                                    ? null
                                    : reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                CreatedByUserId = reader.IsDBNull(reader.GetOrdinal("CreatedByUserId"))
                                    ? null
                                    : reader.GetInt32(reader.GetOrdinal("CreatedByUserId")),
                UpdatedByUserId = reader.IsDBNull(reader.GetOrdinal("UpdatedByUserId"))
                                    ? null
                                    : reader.GetInt32(reader.GetOrdinal("UpdatedByUserId")),
                CreatedByName   = reader.HasColumn("CreatedByName") && !reader.IsDBNull(reader.GetOrdinal("CreatedByName"))
                                    ? reader.GetString(reader.GetOrdinal("CreatedByName"))
                                    : null,
                UpdatedByName   = reader.HasColumn("UpdatedByName") && !reader.IsDBNull(reader.GetOrdinal("UpdatedByName"))
                                    ? reader.GetString(reader.GetOrdinal("UpdatedByName"))
                                    : null
            };

            result.Add(dto);
        }

        return result;
    }

    public async Task<ItemDto?> GetByIdAsync(int id, CancellationToken ct)
    {
        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "dbo.SP_STARTER_ITEMS_GETBYID";
        cmd.CommandType = CommandType.StoredProcedure;

        var pId = cmd.CreateParameter();
        pId.ParameterName = "@Id";
        pId.Value = id;
        cmd.Parameters.Add(pId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
            return null;

        return new ItemDto
        {
            Id              = reader.GetInt32(reader.GetOrdinal("Id")),
            Name            = reader.GetString(reader.GetOrdinal("Name")),
            Description     = reader.IsDBNull(reader.GetOrdinal("Description"))
                                ? null
                                : reader.GetString(reader.GetOrdinal("Description")),
            Active          = reader.IsDBNull(reader.GetOrdinal("Active"))
                                ? null
                                : reader.GetString(reader.GetOrdinal("Active")),
            CreatedAt       = reader.IsDBNull(reader.GetOrdinal("CreatedAt"))
                                ? default
                                : reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            UpdatedAt       = reader.IsDBNull(reader.GetOrdinal("UpdatedAt"))
                                ? null
                                : reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
            CreatedByUserId = reader.IsDBNull(reader.GetOrdinal("CreatedByUserId"))
                                ? null
                                : reader.GetInt32(reader.GetOrdinal("CreatedByUserId")),
            UpdatedByUserId = reader.IsDBNull(reader.GetOrdinal("UpdatedByUserId"))
                                ? null
                                : reader.GetInt32(reader.GetOrdinal("UpdatedByUserId"))
        };
    }

    public async Task<ItemDto> CreateAsync(ItemDto dto, int? currentUserId, CancellationToken ct)
    {
        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "dbo.SP_STARTER_ITEMS_CREATE";
        cmd.CommandType = CommandType.StoredProcedure;

        void AddParam(string name, object? value)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }

        AddParam("@Name", dto.Name);
        AddParam("@Description", dto.Description);
        AddParam("@CreatedByUserId", currentUserId);

        var result = await cmd.ExecuteScalarAsync(ct);
        dto.Id = Convert.ToInt32(result);

        // opcional: recarregar via GetByIdAsync(dto.Id, ct) se quiser campos de data já retornando

        return dto;
    }

    public async Task<bool> UpdateAsync(int id, ItemDto dto, int? currentUserId, CancellationToken ct)
    {
        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "dbo.SP_STARTER_ITEMS_UPDATE";
        cmd.CommandType = CommandType.StoredProcedure;

        void AddParam(string name, object? value)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }

        var active = string.IsNullOrWhiteSpace(dto.Active)
            ? "Yes"
            : dto.Active.Trim();

        AddParam("@Id", id);
        AddParam("@Name", dto.Name);
        AddParam("@Description", dto.Description);
        AddParam("@Active", active);
        AddParam("@UpdatedByUserId", currentUserId);

        var result = await cmd.ExecuteScalarAsync(ct);
        var rows = (result == null || result == DBNull.Value) ? 0 : Convert.ToInt32(result);
        return rows > 0;
    }

    public async Task<bool> SoftDeleteAsync(int id, int? currentUserId, CancellationToken ct)
    {
        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "dbo.SP_STARTER_ITEMS_SOFTDELETE";
        cmd.CommandType = CommandType.StoredProcedure;

        void AddParam(string name, object? value)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }

        AddParam("@Id", id);
        AddParam("@UpdatedByUserId", currentUserId);

        var result = await cmd.ExecuteScalarAsync(ct);
        var rows = (result == null || result == DBNull.Value) ? 0 : Convert.ToInt32(result);
        return rows > 0;

    }
}

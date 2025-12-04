using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Starter.Api.Data;
using Starter.Api.Security;

namespace Starter.Api.Services
{
    public class AuditLogger : IAuditLogger
    {
        private readonly AppDbContext _db;
        private readonly IHttpContextAccessor _http;

        public AuditLogger(AppDbContext db, IHttpContextAccessor http)
        {
            _db = db;
            _http = http;
        }

        /// <summary>
        /// Método principal de log.
        /// Agora grava na tabela Logs via stored procedure SP_STARTER_AUDIT_LOG_INSERT.
        /// </summary>
        public async Task LogAsync(
            string action,
            int? userId = null,
            string? details = null,
            CancellationToken ct = default)
        {
            // Se userId não vier, tenta extrair do token (claims)
            if (userId == null)
            {
                var principal = _http.HttpContext?.User;
                var uid = principal?.TryGetUserId();
                if (uid.HasValue)
                    userId = uid.Value;
            }

            // Monta a mensagem final que vai para a coluna Description (varchar(250))
            // Ex.: [Auth.Login] User 'bruno' logged in
            var baseAction = action ?? string.Empty;
            var message = string.IsNullOrWhiteSpace(details)
                ? baseAction
                : $"{baseAction}: {details}";

            // Garante que cabe em varchar(250)
            if (message.Length > 250)
                message = message[..250];

            // UserId na tabela é NOT NULL, então se continuar nulo, usamos 0
            var finalUserId = userId.GetValueOrDefault(0);

            var conn = _db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync(ct);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "dbo.SP_STARTER_AUDIT_LOG_INSERT";
            cmd.CommandType = CommandType.StoredProcedure;

            AddParam(cmd, "@UserId", finalUserId);
            AddParam(cmd, "@RoleId", DBNull.Value);        // por enquanto não populamos RoleId
            AddParam(cmd, "@PermissionId", DBNull.Value);  // nem PermissionId
            AddParam(cmd, "@Description", message);

            await cmd.ExecuteNonQueryAsync(ct);
        }

        private static void AddParam(IDbCommand cmd, string name, object? value)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }

        // ===== Overloads de compatibilidade =====

        public Task LogAsync(int userId, string description, CancellationToken ct = default)
            => LogAsync(description, userId, null, ct);

        public Task LogAsync(string action, string details, CancellationToken ct = default)
            => LogAsync(action, null, details, ct);

        public Task LogAsync1(string action, CancellationToken ct = default)
            => LogAsync(action, null, null, ct);

        public Task LogAsync1(string action, int userId, CancellationToken ct = default)
            => LogAsync(action, userId, null, ct);

        public Task LogAsync1(string action, int userId, string? details, CancellationToken ct = default)
            => LogAsync(action, userId, details, ct);

        public Task LogAsync1(string action, string details, CancellationToken ct = default) // <- novo
            => LogAsync(action, null, details, ct);
    }
}

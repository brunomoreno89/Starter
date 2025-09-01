using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Starter.Api.Data;
using Starter.Api.Models;
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

        public async Task LogAsync(string action, int? userId = null, string? details = null, CancellationToken ct = default)
        {
            // tenta extrair do token se não vier explícito
            if (userId == null)
            {
                var principal = _http.HttpContext?.User;
                var uid = principal?.TryGetUserId();
                if (uid.HasValue) userId = uid.Value;
            }

            if (action?.Length > 250) action = action[..250];
            if (details?.Length > 2000) details = details[..2000];

            var message = string.IsNullOrWhiteSpace(details)
                ? action ?? string.Empty
                : $"{action}: {details}";

            TimeZoneInfo tz;
            try { tz = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo"); }
            catch { tz = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"); }

            var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);

            var entry = new LogEntry
            {
                // se LogEntry.UserId for int não-nullable, usar GetValueOrDefault()
                UserId      = userId.GetValueOrDefault(),
                ExecDate    = nowLocal,
                Description = message
            };

            _db.Logs.Add(entry);
            await _db.SaveChangesAsync(ct);
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

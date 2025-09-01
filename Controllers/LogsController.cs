using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Starter.Api.Data;
using Starter.Api.DTOs.Logs;
using System.Text;

namespace Starter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "Perm:Logs.Read")]
public class LogsController : ControllerBase
{
    private readonly AppDbContext _db;
    public LogsController(AppDbContext db) => _db = db;

    // ===== Helpers: faixa local [start00:00, nextDay00:00) ===================

    /// <summary>
    /// Constrói a faixa HALF-OPEN em horário LOCAL (São Paulo) sem converter para UTC:
    /// [startLocal 00:00, endLocal + 1 dia 00:00).
    /// Se vierem nulos, usa o mês corrente local.
    /// </summary>
    private static (DateTime startLocal00, DateTime endLocalNextDay00) BuildLocalHalfOpenRange(DateTime? startParam, DateTime? endParam)
    {
        // “Agora” em São Paulo (apenas para pegar mês/ano padrão)
        TimeZoneInfo tz;
        try { tz = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo"); }
        catch { tz = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"); }
        var nowLocal = TimeZoneInfo.ConvertTime(DateTime.UtcNow, tz);

        // Datas (somente componente de data) no calendário local
        var sDate = (startParam?.Date) ?? new DateTime(nowLocal.Year, nowLocal.Month, 1);
        var eDate = (endParam?.Date)   ?? new DateTime(nowLocal.Year, nowLocal.Month, DateTime.DaysInMonth(nowLocal.Year, nowLocal.Month));

        if (eDate < sDate) (sDate, eDate) = (eDate, sDate);

        // IMPORTANTE: retornamos DATETIME “cru” (sem tz) para comparar direto com a coluna DATETIME
        var startLocal00   = sDate;            // 00:00 do dia inicial
        var endLocalNext00 = eDate.AddDays(1); // 00:00 do dia seguinte (fim EXCLUSIVO)

        return (startLocal00, endLocalNext00);
    }

    // ===== GET /api/logs =====================================================

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LogListItemDto>>> List([FromQuery] LogQueryDto q, CancellationToken ct)
    {
        var query =
            from l in _db.Logs.AsNoTracking()
            // LEFT JOIN Users
            join u0 in _db.Users.AsNoTracking() on l.UserId equals u0.Id into uj
            from u in uj.DefaultIfEmpty()
            // LEFT JOIN Roles
            join r0 in _db.Roles.AsNoTracking() on l.RoleId equals r0.Id into rj
            from r in rj.DefaultIfEmpty()
            // LEFT JOIN Permissions
            join p0 in _db.Permissions.AsNoTracking() on l.PermissionId equals p0.Id into pj
            from p in pj.DefaultIfEmpty()
            select new { l, u, r, p };

        if (!string.IsNullOrWhiteSpace(q.User))
        {
            var term = q.User.Trim();
            query = query.Where(x =>
                (x.u != null && x.u.Username != null && x.u.Username.Contains(term)) ||
                (x.u != null && x.u.Name != null && x.u.Name.Contains(term)));
        }

        // FAIXA LOCAL half-open, sem UTC:
        var (startLocal00, endLocalNext00) = BuildLocalHalfOpenRange(q.StartDate, q.EndDate);
        query = query.Where(x => x.l.ExecDate >= startLocal00 && x.l.ExecDate < endLocalNext00);

        var data = await query
            .OrderByDescending(x => x.l.ExecDate)
            .Take(5000) // limite de segurança
            .Select(x => new LogListItemDto
            {
                Id = x.l.Id,
                ExecDate = x.l.ExecDate, // armazenado como LOCAL na tabela
                UserId = x.l.UserId,
                Username = x.u != null ? x.u.Username : null,
                Name = x.u != null ? x.u.Name : null,

                // Se seu DTO usa int? troque por (int?)x.r?.Id / (int?)x.p?.Id
                RoleId = x.r != null ? x.r.Id : 0,
                RoleName = x.r != null ? x.r.Name : null,
                PermissionId = x.p != null ? x.p.Id : 0,
                PermissionName = x.p != null ? x.p.Name : null,

                Description = x.l.Description
            })
            .ToListAsync(ct);

        return Ok(data);
    }

    // ===== GET /api/logs/export.csv =========================================

    [Authorize(Policy = "Perm:Logs.Export")]
    [HttpGet("export.csv")]
    public async Task<IActionResult> ExportCsv([FromQuery] LogQueryDto q, CancellationToken ct)
    {
        var query =
            from l in _db.Logs.AsNoTracking()
            join u0 in _db.Users.AsNoTracking() on l.UserId equals u0.Id into uj
            from u in uj.DefaultIfEmpty()
            join r0 in _db.Roles.AsNoTracking() on l.RoleId equals r0.Id into rj
            from r in rj.DefaultIfEmpty()
            join p0 in _db.Permissions.AsNoTracking() on l.PermissionId equals p0.Id into pj
            from p in pj.DefaultIfEmpty()
            select new { l, u, r, p };

        if (!string.IsNullOrWhiteSpace(q.User))
        {
            var term = q.User.Trim();
            query = query.Where(x =>
                (x.u != null && x.u.Username != null && x.u.Username.Contains(term)) ||
                (x.u != null && x.u.Name != null && x.u.Name.Contains(term)));
        }

        // Mesma FAIXA LOCAL half-open
        var (startLocal00, endLocalNext00) = BuildLocalHalfOpenRange(q.StartDate, q.EndDate);
        query = query.Where(x => x.l.ExecDate >= startLocal00 && x.l.ExecDate < endLocalNext00);

        var rows = await query
            .OrderByDescending(x => x.l.ExecDate)
            .ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("Id,ExecDate,UserId,Username,Name,RoleId,RoleName,PermissionId,PermissionName,Description");

        foreach (var x in rows)
        {
            string Csv(string? s) => "\"" + (s ?? "").Replace("\"", "\"\"") + "\"";

            sb.Append(x.l.Id).Append(',')
              .Append(x.l.ExecDate.ToString("yyyy-MM-dd HH:mm:ss")).Append(',')
              .Append(x.u?.Id ?? 0).Append(',')
              .Append(Csv(x.u?.Username)).Append(',')
              .Append(Csv(x.u?.Name)).Append(',')
              .Append(x.r != null ? x.r.Id.ToString() : "").Append(',')
              .Append(Csv(x.r?.Name)).Append(',')
              .Append(x.p != null ? x.p.Id.ToString() : "").Append(',')
              .Append(Csv(x.p?.Name)).Append(',')
              .Append(Csv(x.l.Description)).AppendLine();
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var fname = $"logs_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
        return File(bytes, "text/csv", fname);
    }
}

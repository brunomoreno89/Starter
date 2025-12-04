using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Starter.Api.DTOs.Logs;
using Starter.Api.Services;
using System.Text;

namespace Starter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "Perm:Logs.Read")]
public class LogsController : ControllerBase
{
    private readonly ILogService _logs;

    public LogsController(ILogService logs)
    {
        _logs = logs;
    }

    // ===== Helpers: faixa local [start00:00, nextDay00:00) ===================

    /// <summary>
    /// Constrói a faixa HALF-OPEN em horário LOCAL (São Paulo) sem converter para UTC:
    /// [startLocal 00:00, endLocal + 1 dia 00:00).
    /// Se vierem nulos, usa o mês corrente local.
    /// </summary>
    private static (DateTime startLocal00, DateTime endLocalNextDay00) BuildLocalHalfOpenRange(
        DateTime? startParam,
        DateTime? endParam)
    {
        TimeZoneInfo tz;
        try { tz = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo"); }
        catch { tz = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"); }

        var nowLocal = TimeZoneInfo.ConvertTime(DateTime.UtcNow, tz);

        var sDate = (startParam?.Date) ?? new DateTime(nowLocal.Year, nowLocal.Month, 1);
        var eDate = (endParam?.Date)   ?? new DateTime(nowLocal.Year, nowLocal.Month,
                                DateTime.DaysInMonth(nowLocal.Year, nowLocal.Month));

        if (eDate < sDate) (sDate, eDate) = (eDate, sDate);

        var startLocal00   = sDate;
        var endLocalNext00 = eDate.AddDays(1);

        return (startLocal00, endLocalNext00);
    }

    // ===== GET /api/logs =====================================================

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LogListItemDto>>> List(
        [FromQuery] LogQueryDto q,
        CancellationToken ct)
    {
        var (startLocal00, endLocalNext00) = BuildLocalHalfOpenRange(q.StartDate, q.EndDate);

        var data = await _logs.ListAsync(
            q.User,
            startLocal00,
            endLocalNext00,
            maxRows: 5000,
            ct);

        return Ok(data);
    }

    // ===== GET /api/logs/export.csv =========================================

    [Authorize(Policy = "Perm:Logs.Export")]
    [HttpGet("export.csv")]
    public async Task<IActionResult> ExportCsv(
        [FromQuery] LogQueryDto q,
        CancellationToken ct)
    {
        var (startLocal00, endLocalNext00) = BuildLocalHalfOpenRange(q.StartDate, q.EndDate);

        // aqui você pode escolher o limite (por ex. 100k)
        var rows = await _logs.ListAsync(
            q.User,
            startLocal00,
            endLocalNext00,
            maxRows: 100000,
            ct);

        var sb = new StringBuilder();
        sb.AppendLine("Id,ExecDate,UserId,Username,Name,RoleId,RoleName,PermissionId,PermissionName,Description");

        foreach (var x in rows)
        {
            string Csv(string? s) => "\"" + (s ?? "").Replace("\"", "\"\"") + "\"";

            sb.Append(x.Id).Append(',')
              .Append(x.ExecDate.ToString("yyyy-MM-dd HH:mm:ss")).Append(',')
              .Append(x.UserId.ToString() ?? "").Append(',')
              .Append(Csv(x.Username)).Append(',')
              .Append(Csv(x.Name)).Append(',')
              .Append(x.RoleId?.ToString() ?? "").Append(',')
              .Append(Csv(x.RoleName)).Append(',')
              .Append(x.PermissionId?.ToString() ?? "").Append(',')
              .Append(Csv(x.PermissionName)).Append(',')
              .Append(Csv(x.Description)).AppendLine();
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var fname = $"logs_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
        return File(bytes, "text/csv", fname);
    }
}

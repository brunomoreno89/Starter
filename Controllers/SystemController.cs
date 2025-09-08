// Controllers/SystemController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Starter.Api.Data;
using Starter.Api.DTOs.System;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SystemController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SystemController(AppDbContext db, IDateTimeProvider clock, IDateTimeProvider dateTimeProvider)
    {
        _db = db;
        _clock = clock;
        _dateTimeProvider = dateTimeProvider;
    }

    [HttpGet("dates")]
    public async Task<ActionResult<IEnumerable<SystemDto>>> List(CancellationToken ct)
    {
        var sysDates = await _db.SysDates
            .AsNoTracking()
            .ToListAsync(ct);

        var result = sysDates.Select(sysDt => new SysDates
        {
            SysCurrentDate = sysDt.SysCurrentDate,
            SysClosedDate = sysDt.SysClosedDate,
            SysName = sysDt.SysName

        }).ToList();

        return Ok(result);
    }    
}

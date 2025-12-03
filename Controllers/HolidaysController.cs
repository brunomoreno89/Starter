using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Starter.Api.Data;
using Starter.Api.DTOs.Holidays;
using Starter.Api.Models;
using Starter.Api.Security;
using Starter.Api.Services;      // IAuditLogger
using System.Threading;          // CancellationToken

namespace Starter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HolidaysController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly IDateTimeProvider _dateTimeProvider;

    public HolidaysController(AppDbContext db, IAuditLogger audit, IDateTimeProvider dateTimeProvider)
    {
        _db = db;
        _audit = audit;
        _dateTimeProvider = dateTimeProvider;
    }

    [Authorize(Policy = "Perm:Holidays.Read")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<HolidaysDto>>> List(CancellationToken ct)
    {
        // 1) carrega todos os nomes (Id -> DisplayName) de uma vez
        var names = await _db.Users
            .AsNoTracking()
            .Select(x => new { x.Id, Display = x.Name ?? x.Username })
            .ToDictionaryAsync(x => x.Id, x => x.Display, ct);

        // 2) carrega usuários
        var branch = await _db.Holidays
            .AsNoTracking()
            .OrderBy(u => u.Id)
            .ToListAsync(ct);

        var result = branch.Select(Holidays => new HolidaysDto
        {
            Id = Holidays.Id,
            Description    = Holidays.Description,
            HolidayDate    = Holidays.HolidayDate,
            BranchId = Holidays.BranchId,
            CreatedAt = Holidays.CreatedAt,
            CreatedByUserId = Holidays.CreatedByUserId,
            UpdatedAt = Holidays.UpdatedAt,
            UpdatedByUserId = Holidays.UpdatedByUserId,
            Active = Holidays.Active
        }).ToList();

        return Ok(result);
    }   

    [Authorize(Policy = "Perm:Holidays.Read")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<HolidaysDto>> GetOne(int id, CancellationToken ct)
    {
        var i = await _db.Items.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (i == null) return NotFound();
        return new HolidaysDto { Id = i.Id, Description = i.Description };
    }

    [Authorize(Policy = "Perm:Holidays.Create")]
    [HttpPost]
    public async Task<ActionResult<HolidaysDto>> Create(
        [FromBody] HolidaysDto dto,
        [FromServices] IValidator<HolidaysDto>? validator = null,
        CancellationToken ct = default)
    {
        if (dto is null) return BadRequest("Body obrigatório.");

        if (validator is not null)
        {
            var val = await validator.ValidateAsync(dto, ct);
            if (!val.IsValid) return BadRequest(val.Errors);
        }

        var entity = new Holiday
        {
            Description = dto.Description,
            HolidayDate    = dto.HolidayDate,
            BranchId = dto.BranchId,
            CreatedAt = _dateTimeProvider.NowLocal,
            Active = "Yes",
            CreatedByUserId = User.TryGetUserId()
        };

        _db.Holidays.Add(entity);
        await _db.SaveChangesAsync(ct);

        // Log
        await _audit.LogAsync("Holidays.Create", $"Created item {entity.Description}", ct);

        dto.Id = entity.Id;
        return CreatedAtAction(nameof(GetOne), new { id = entity.Id }, dto);
    }

    [Authorize(Policy = "Perm:Holidays.Update")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] HolidaysDto dto,
        [FromServices] IValidator<HolidaysDto>? validator = null,
        CancellationToken ct = default)
    {
        if (dto is null) return BadRequest("Body obrigatório.");

        if (validator is not null)
        {
            var val = await validator.ValidateAsync(dto, ct);
            if (!val.IsValid) return BadRequest(val.Errors);
        }

        var entity = await _db.Holidays.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity == null) return NotFound();

        
        entity.Description = dto.Description;
        entity.HolidayDate    = dto.HolidayDate;
        entity.BranchId = dto.BranchId;
        entity.UpdatedAt = _dateTimeProvider.NowLocal;
        entity.UpdatedByUserId = User.TryGetUserId(); 
        entity.Active = dto.Active?.Trim();

        await _db.SaveChangesAsync(ct);

        // Log
        await _audit.LogAsync("Holidays.Update", $"Updated item {entity.Description}", ct);

        return NoContent();
    }

    [Authorize(Policy = "Perm:Holidays.Delete")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.Items.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity == null) return NotFound();

        entity.Active    = "No";
        entity.UpdatedByUserId = User.TryGetUserId();
        entity.UpdatedAt = _dateTimeProvider.NowLocal;
        await _db.SaveChangesAsync(ct);

        // Log
        await _audit.LogAsync("Holidays.Delete", $"Deleted item #{id}", ct);

        return NoContent();
    }
}

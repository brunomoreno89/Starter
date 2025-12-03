using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Starter.Api.Data;
using Starter.Api.DTOs.Regions;
using Starter.Api.Models;
using Starter.Api.Security;
using Starter.Api.Services;      // IAuditLogger
using System.Threading;          // CancellationToken

namespace Starter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RegionsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RegionsController(AppDbContext db, IAuditLogger audit, IDateTimeProvider dateTimeProvider)
    {
        _db = db;
        _audit = audit;
        _dateTimeProvider = dateTimeProvider;
    }

    [Authorize(Policy = "Perm:Regions.Read")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RegionsDto>>> List(CancellationToken ct)
    {
        // 1) carrega todos os nomes (Id -> DisplayName) de uma vez
        var names = await _db.Users
            .AsNoTracking()
            .Select(x => new { x.Id, Display = x.Name ?? x.Username })
            .ToDictionaryAsync(x => x.Id, x => x.Display, ct);

        // 2) carrega usuários
        var region = await _db.Regions
            .AsNoTracking()
            .OrderBy(u => u.Id)
            .ToListAsync(ct);

        var result = region.Select(regions => new RegionsDto
        {
            Id = regions.Id,
            Description    = regions.Description,
            CreatedAt = regions.CreatedAt,
            CreatedByUserId = regions.CreatedByUserId,
            UpdatedAt = regions.UpdatedAt,
            UpdatedByUserId = regions.UpdatedByUserId,
            Active = regions.Active
        }).ToList();

        return Ok(result);
    }   

    [Authorize(Policy = "Perm:Regions.Read")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<RegionsDto>> GetOne(int id, CancellationToken ct)
    {
        var i = await _db.Items.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (i == null) return NotFound();
        return new RegionsDto { Id = i.Id, Description = i.Description };
    }

    [Authorize(Policy = "Perm:Regions.Create")]
    [HttpPost]
    public async Task<ActionResult<RegionsDto>> Create(
        [FromBody] RegionsDto dto,
        [FromServices] IValidator<RegionsDto>? validator = null,
        CancellationToken ct = default)
    {
        if (dto is null) return BadRequest("Body obrigatório.");

        if (validator is not null)
        {
            var val = await validator.ValidateAsync(dto, ct);
            if (!val.IsValid) return BadRequest(val.Errors);
        }

        var entity = new Region
        {
            Description = dto.Description,
            CreatedAt = _dateTimeProvider.NowLocal,
            Active = "Yes",
            CreatedByUserId = User.TryGetUserId()
        };

        _db.Regions.Add(entity);
        await _db.SaveChangesAsync(ct);

        // Log
        await _audit.LogAsync("Regions.Create", $"Created item {entity.Description}", ct);

        dto.Id = entity.Id;
        return CreatedAtAction(nameof(GetOne), new { id = entity.Id }, dto);
    }

    [Authorize(Policy = "Perm:Regions.Update")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] RegionsDto dto,
        [FromServices] IValidator<RegionsDto>? validator = null,
        CancellationToken ct = default)
    {
        if (dto is null) return BadRequest("Body obrigatório.");

        if (validator is not null)
        {
            var val = await validator.ValidateAsync(dto, ct);
            if (!val.IsValid) return BadRequest(val.Errors);
        }

        var entity = await _db.Regions.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity == null) return NotFound();

        
        entity.Description = dto.Description;
        entity.UpdatedAt = _dateTimeProvider.NowLocal;
        entity.UpdatedByUserId = User.TryGetUserId(); 
        entity.Active = dto.Active?.Trim();

        await _db.SaveChangesAsync(ct);

        // Log
        await _audit.LogAsync("Regions.Update", $"Updated item {entity.Description}", ct);

        return NoContent();
    }

    [Authorize(Policy = "Perm:Regions.Delete")]
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
        await _audit.LogAsync("Items.Delete", $"Deleted item #{id}", ct);

        return NoContent();
    }
}

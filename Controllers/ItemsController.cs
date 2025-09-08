using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Starter.Api.Data;
using Starter.Api.DTOs.Items;
using Starter.Api.Models;
using Starter.Api.Security;
using Starter.Api.Services;      // IAuditLogger
using System.Threading;          // CancellationToken

namespace Starter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ItemsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ItemsController(AppDbContext db, IAuditLogger audit, IDateTimeProvider dateTimeProvider)
    {
        _db = db;
        _audit = audit;
        _dateTimeProvider = dateTimeProvider;
    }

    [Authorize(Policy = "Perm:Items.Read")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ItemDto>>> List(CancellationToken ct)
    {
        // 1) carrega todos os nomes (Id -> DisplayName) de uma vez
        var names = await _db.Users
            .AsNoTracking()
            .Select(x => new { x.Id, Display = x.Name ?? x.Username })
            .ToDictionaryAsync(x => x.Id, x => x.Display, ct);

        // 2) carrega usuários
        var item = await _db.Items
            .AsNoTracking()
            .OrderBy(u => u.Id)
            .ToListAsync(ct);

        var result = item.Select(items => new ItemDto
        {
            Id = items.Id,
            Name = items.Name,
            Description    = items.Description,
            Active         = items.Active,
            CreatedAt     = items.CreatedAt,
            UpdatedAt      = items.UpdatedAt,
            CreatedByUserId = items.CreatedByUserId,
            CreatedByName  = (items.CreatedByUserId.HasValue && names.TryGetValue(items.CreatedByUserId.Value, out var cName)) ? cName : null,
            UpdatedByUserId = items.UpdatedByUserId,
            UpdatedByName  = (items.UpdatedByUserId.HasValue && names.TryGetValue(items.UpdatedByUserId.Value, out var uName)) ? uName : null

        }).ToList();

        return Ok(result);
    }   

    [Authorize(Policy = "Perm:Items.Read")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ItemDto>> GetOne(int id, CancellationToken ct)
    {
        var i = await _db.Items.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (i == null) return NotFound();
        return new ItemDto { Id = i.Id, Name = i.Name, Description = i.Description };
    }

    [Authorize(Policy = "Perm:Items.Create")]
    [HttpPost]
    public async Task<ActionResult<ItemDto>> Create(
        [FromBody] ItemDto dto,
        [FromServices] IValidator<ItemDto>? validator = null,
        CancellationToken ct = default)
    {
        if (dto is null) return BadRequest("Body obrigatório.");

        if (validator is not null)
        {
            var val = await validator.ValidateAsync(dto, ct);
            if (!val.IsValid) return BadRequest(val.Errors);
        }

        var entity = new Item
        {
            Name = dto.Name,
            Description = dto.Description,
            CreatedAt = _dateTimeProvider.NowLocal,
            Active = "Yes",
            CreatedByUserId = User.TryGetUserId()
        };

        _db.Items.Add(entity);
        await _db.SaveChangesAsync(ct);

        // Log
        await _audit.LogAsync("Items.Create", $"Created item {entity.Name}", ct);

        dto.Id = entity.Id;
        return CreatedAtAction(nameof(GetOne), new { id = entity.Id }, dto);
    }

    [Authorize(Policy = "Perm:Items.Update")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] ItemDto dto,
        [FromServices] IValidator<ItemDto>? validator = null,
        CancellationToken ct = default)
    {
        if (dto is null) return BadRequest("Body obrigatório.");

        if (validator is not null)
        {
            var val = await validator.ValidateAsync(dto, ct);
            if (!val.IsValid) return BadRequest(val.Errors);
        }

        var entity = await _db.Items.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity == null) return NotFound();

        entity.Name = dto.Name;
        entity.Description = dto.Description;
        entity.UpdatedAt = _dateTimeProvider.NowLocal;
        entity.UpdatedByUserId = User.TryGetUserId(); 
        entity.Active = dto.Active?.Trim();

        await _db.SaveChangesAsync(ct);

        // Log
        await _audit.LogAsync("Items.Update", $"Updated item {entity.Name}", ct);

        return NoContent();
    }

    [Authorize(Policy = "Perm:Items.Delete")]
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

using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Starter.Api.Data;
using Starter.Api.DTOs.Items;
using Starter.Api.Models;
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

    public ItemsController(AppDbContext db, IAuditLogger audit)
    {
        _db = db;
        _audit = audit;
    }

    [Authorize(Policy = "Perm:Items.Read")]
    [HttpGet]
    public async Task<IEnumerable<ItemDto>> GetAll(CancellationToken ct)
    {
        return await _db.Items
            .AsNoTracking()
            .Select(i => new ItemDto { Id = i.Id, Name = i.Name, Description = i.Description })
            .ToListAsync(ct);
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
            CreatedAt = DateTime.UtcNow
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
        entity.UpdatedAt = DateTime.UtcNow;

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

        _db.Items.Remove(entity);
        await _db.SaveChangesAsync(ct);

        // Log
        await _audit.LogAsync("Items.Delete", $"Deleted item #{id}", ct);

        return NoContent();
    }
}

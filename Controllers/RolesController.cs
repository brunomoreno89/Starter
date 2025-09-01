// Controllers/RolesController.cs
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Starter.Api.Data;
using Starter.Api.DTOs.Security;
using Starter.Api.Models;
using Starter.Api.Services;      // <- IAuditLogger
using System.Threading;          // <- CancellationToken

namespace Starter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAuditLogger _audit;

    public RolesController(AppDbContext db, IAuditLogger audit)
    {
        _db = db;
        _audit = audit;
    }

    [Authorize(Policy = "Perm:Roles.Read")]
    [HttpGet]
    public async Task<IEnumerable<RoleDto>> GetAll(CancellationToken ct)
        => await _db.Roles.AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => new RoleDto { Id = r.Id, Name = r.Name, Description = r.Description })
            .ToListAsync(ct);

    [Authorize(Policy = "Perm:Roles.Read")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<RoleDto>> GetOne(int id, CancellationToken ct)
    {
        var r = await _db.Roles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (r is null) return NotFound();
        return new RoleDto { Id = r.Id, Name = r.Name, Description = r.Description };
    }

    [Authorize(Policy = "Perm:Roles.Create")]
    [HttpPost]
    public async Task<ActionResult<RoleDto>> Create(
        [FromBody] RoleCreateDto dto,
        [FromServices] IValidator<RoleCreateDto>? validator = null,
        CancellationToken ct = default)
    {
        if (dto is null) return BadRequest("Body is mandatory.");

        if (validator is not null)
        {
            var val = await validator.ValidateAsync(dto, ct);
            if (!val.IsValid) return BadRequest(val.Errors);
        }

        var name = dto.Name?.Trim();
        var desc = dto.Description?.Trim();

        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Name is mandatory.");

        if (await _db.Roles.AsNoTracking().AnyAsync(x => x.Name == name, ct))
            return Conflict("Role name already exists.");

        var entity = new Role { Name = name!, Description = desc };
        _db.Roles.Add(entity);
        await _db.SaveChangesAsync(ct);

        // 🔎 Log
        await _audit.LogAsync("Roles.Create", $"Created role {entity.Name} (Id={entity.Id})", ct);

        return CreatedAtAction(nameof(GetOne), new { id = entity.Id },
            new RoleDto { Id = entity.Id, Name = entity.Name, Description = entity.Description });
    }

    [Authorize(Policy = "Perm:Roles.Update")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] RoleUpdateDto dto,
        [FromServices] IValidator<RoleUpdateDto>? validator = null,
        CancellationToken ct = default)
    {
        if (dto is null) return BadRequest("Body is mandatory.");

        if (validator is not null)
        {
            var val = await validator.ValidateAsync(dto, ct);
            if (!val.IsValid) return BadRequest(val.Errors);
        }

        var entity = await _db.Roles.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();

        var name = dto.Name?.Trim();
        var desc = dto.Description?.Trim();

        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Name is mandatory.");

        var dup = await _db.Roles.AsNoTracking().AnyAsync(x => x.Id != id && x.Name == name, ct);
        if (dup) return Conflict("Role name already exists.");

        entity.Name = name!;
        entity.Description = desc;
        await _db.SaveChangesAsync(ct);

        // 🔎 Log
        await _audit.LogAsync("Roles.Update", $"Updated role {entity.Name} (Id={entity.Id})", ct);

        return NoContent();
    }

    [Authorize(Policy = "Perm:Roles.Delete")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.Roles.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();

        _db.Roles.Remove(entity);
        await _db.SaveChangesAsync(ct);

        // 🔎 Log
        await _audit.LogAsync("Roles.Delete", $"Deleted role {entity.Name} (Id={entity.Id})", ct);

        return NoContent();
    }
}

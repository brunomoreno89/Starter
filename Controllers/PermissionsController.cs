// Controllers/PermissionsController.cs
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
public class PermissionsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAuditLogger _audit;

    public PermissionsController(AppDbContext db, IAuditLogger audit)
    {
        _db = db;
        _audit = audit;
    }

    [Authorize(Policy = "Perm:Permissions.Read")]
    [HttpGet]
    public async Task<IEnumerable<PermissionDto>> GetAll(CancellationToken ct)
        => await _db.Permissions.AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new PermissionDto { Id = p.Id, Name = p.Name, Description = p.Description })
            .ToListAsync(ct);

    [Authorize(Policy = "Perm:Users.Read")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PermissionDto>> GetOne(int id, CancellationToken ct)
    {
        var p = await _db.Permissions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return NotFound();
        return new PermissionDto { Id = p.Id, Name = p.Name, Description = p.Description };
    }

    [Authorize(Policy = "Perm:Permissions.Create")]
    [HttpPost]
    public async Task<ActionResult<PermissionDto>> Create(
        [FromBody] PermissionCreateDto dto,
        [FromServices] IValidator<PermissionCreateDto>? validator = null,
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

        if (await _db.Permissions.AnyAsync(x => x.Name == name, ct))
            return Conflict("Permission name already exists.");

        var entity = new Permission { Name = name!, Description = desc };
        _db.Permissions.Add(entity);
        await _db.SaveChangesAsync(ct);

        // 🔎 Log
        await _audit.LogAsync("Permissions.Create", $"Created permission {entity.Name} (Id={entity.Id})", ct);

        return CreatedAtAction(nameof(GetOne), new { id = entity.Id },
            new PermissionDto { Id = entity.Id, Name = entity.Name, Description = entity.Description });
    }

    [Authorize(Policy = "Perm:Permissions.Update")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] PermissionUpdateDto dto,
        [FromServices] IValidator<PermissionUpdateDto>? validator = null,
        CancellationToken ct = default)
    {
        if (dto is null) return BadRequest("Body is mandatory.");

        if (validator is not null)
        {
            var val = await validator.ValidateAsync(dto, ct);
            if (!val.IsValid) return BadRequest(val.Errors);
        }

        var entity = await _db.Permissions.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();

        var name = dto.Name?.Trim();
        var desc = dto.Description?.Trim();

        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Name is mandatory.");

        var dup = await _db.Permissions.AnyAsync(x => x.Id != id && x.Name == name, ct);
        if (dup) return Conflict("Permission name already exists.");

        entity.Name = name!;
        entity.Description = desc;

        await _db.SaveChangesAsync(ct);

        // 🔎 Log
        await _audit.LogAsync("Permissions.Update", $"Updated permission {entity.Name} (Id={entity.Id})", ct);

        return NoContent();
    }

    // NOTE: esta policy parece destoar do padrão; confira se não deveria ser "Perm:Permissions.Delete"
    [Authorize(Policy = "Perm:UserRoles.Assign.Delete")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.Permissions.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();

        _db.Permissions.Remove(entity);
        await _db.SaveChangesAsync(ct);

        // 🔎 Log
        await _audit.LogAsync("Permissions.Delete", $"Deleted permission {entity.Name} (Id={entity.Id})", ct);

        return NoContent();
    }
}

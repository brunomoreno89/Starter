// Controllers/PermissionsController.cs
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Crypto.Prng;
using Starter.Api.Data;
using Starter.Api.DTOs.Security;
using Starter.Api.Models;
using Starter.Api.Security;
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
    private readonly IDateTimeProvider _dateTimeProvider;

    public PermissionsController(AppDbContext db, IAuditLogger audit, IDateTimeProvider dateTimeProvider)
    {
        _db = db;
        _audit = audit;
        _dateTimeProvider = dateTimeProvider;
    }

    [Authorize(Policy = "Perm:Permissions.Read")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PermissionDto>>> List(CancellationToken ct)
    {
        // 1) carrega todos os nomes (Id -> DisplayName) de uma vez
        var names = await _db.Users
            .AsNoTracking()
            .Select(x => new { x.Id, Display = x.Name ?? x.Username })
            .ToDictionaryAsync(x => x.Id, x => x.Display, ct);

        // 2) carrega usuários
        var permission = await _db.Permissions
            .AsNoTracking()
            .OrderBy(u => u.Id)
            .ToListAsync(ct);

        var result = permission.Select(p => new PermissionDto
        {
            Id = p.Id,
            Name = p.Name,
            Description    = p.Description,
            Active         = p.Active,
            CreationDt     = p.CreationDt,
            CreatedBy      = p.CreatedBy,
            CreatedByName  = (p.CreatedBy.HasValue && names.TryGetValue(p.CreatedBy.Value, out var cName)) ? cName : null,
            UpdateDt        = p.UpdateDt,
            UpdatedBy      = p.UpdatedBy,
            UpdatedByName  = (p.UpdatedBy.HasValue && names.TryGetValue(p.UpdatedBy.Value, out var uName)) ? uName : null

        }).ToList();

        return Ok(result);
    }

    [Authorize(Policy = "Perm:Users.Read")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PermissionDto>> GetOne(int id, CancellationToken ct)
    {
        var p = await _db.Permissions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return NotFound();
        return new PermissionDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description
        };
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

        var creatorId = User.TryGetUserId(); 

        var entity = new Permission
        {
            Name = name!,
            Description = desc,
            CreationDt = _dateTimeProvider.NowLocal,
            Active = "Yes",
            CreatedBy = creatorId
        };
        _db.Permissions.Add(entity);
        await _db.SaveChangesAsync(ct);

        // Log
        await _audit.LogAsync("Permissions.Create", $"Created permission {entity.Name} (Id={entity.Id})", ct);

        return CreatedAtAction(nameof(GetOne), new { id = entity.Id },
            new PermissionDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Description = entity.Description,
                Active = entity.Active,
                CreationDt = entity.CreationDt,
                CreatedBy = entity.CreatedBy
            });
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
        var active = dto.Active?.Trim();

        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Name is mandatory.");

        var dup = await _db.Permissions.AnyAsync(x => x.Id != id && x.Name == name, ct);
        if (dup) return Conflict("Permission name already exists.");

        var updateId = User.TryGetUserId(); 

        entity.Name = name!;
        entity.Description = desc;
        entity.Active = active;
        entity.UpdateDt = _dateTimeProvider.NowLocal;
        entity.UpdatedBy = updateId;
        

        await _db.SaveChangesAsync(ct);

        // 🔎 Log
        await _audit.LogAsync("Permissions.Update", $"Updated permission {entity.Name} (Id={entity.Id})", ct);

        return NoContent();
    }

    // NOTE: esta policy parece destoar do padrão; confira se não deveria ser "Perm:Permissions.Delete"
    [Authorize(Policy = "Perm:Permissions.Delete")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.Permissions.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();

        // Soft delete + auditoria
        entity.Active    = "No";
        entity.UpdatedBy = User.TryGetUserId();
        entity.UpdateDt = _dateTimeProvider.NowLocal;

        await _db.SaveChangesAsync(ct);

        // Log
        await _audit.LogAsync("Permissions.Delete", $"Deleted permission {entity.Name} (Id={entity.Id})", ct);

        return NoContent();
    }
}

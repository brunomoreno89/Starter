// Controllers/RolesController.cs
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
public class RolesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RolesController(AppDbContext db, IAuditLogger audit, IDateTimeProvider dateTimeProvider)
    {
        _db = db;
        _audit = audit;
        _dateTimeProvider = dateTimeProvider;
    }

    [Authorize(Policy = "Perm:Roles.Read")]
    [HttpGet]

    public async Task<ActionResult<IEnumerable<RoleDto>>> List(CancellationToken ct)
    {
        // 1) carrega todos os nomes (Id -> DisplayName) de uma vez
        var names = await _db.Users
            .AsNoTracking()
            .Select(x => new { x.Id, Display = x.Name ?? x.Username })
            .ToDictionaryAsync(x => x.Id, x => x.Display, ct);

        // 2) carrega usuários
        var role = await _db.Roles
            .AsNoTracking()
            .OrderBy(u => u.Id)
            .ToListAsync(ct);

        var result = role.Select(r => new RoleDto
        {
            Id = r.Id,
            Name = r.Name,
            Description    = r.Description,
            Active         = r.Active,
            CreationDt     = r.CreationDt,
            CreatedBy      = r.CreatedBy,
            CreatedByName  = (r.CreatedBy.HasValue && names.TryGetValue(r.CreatedBy.Value, out var cName)) ? cName : null,
            UpdateDt        = r.UpdateDt,
            UpdatedBy      = r.UpdatedBy,
            UpdatedByName  = (r.UpdatedBy.HasValue && names.TryGetValue(r.UpdatedBy.Value, out var uName)) ? uName : null

        }).ToList();

        return Ok(result);
    }        

    [Authorize(Policy = "Perm:Roles.Read")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<RoleDto>> GetOne(int id, CancellationToken ct)
    {
        var r = await _db.Roles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (r is null) return NotFound();
        return new RoleDto
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description
        };
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

        var creatorId = User.TryGetUserId(); 

        var entity = new Role
        {
            Name = name!,
            Description = desc,
            CreationDt = _dateTimeProvider.NowLocal,
            Active = "Yes",
            CreatedBy = creatorId
        };
        _db.Roles.Add(entity);
        await _db.SaveChangesAsync(ct);

        // 🔎 Log
        await _audit.LogAsync("Roles.Create", $"Created role {entity.Name} (Id={entity.Id})", ct);

        return CreatedAtAction(nameof(GetOne), new { id = entity.Id },
            new RoleDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Description = entity.Description,
                Active = entity.Active,
                CreationDt = entity.CreationDt,
                CreatedBy = entity.CreatedBy
            });
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
        var active = dto.Active?.Trim();

        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Name is mandatory.");

        var dup = await _db.Roles.AsNoTracking().AnyAsync(x => x.Id != id && x.Name == name, ct);
        if (dup) return Conflict("Role name already exists.");

        var updateId = User.TryGetUserId(); 

        entity.Name = name!;
        entity.Description = desc;
        entity.Active = active;
        entity.UpdateDt = _dateTimeProvider.NowLocal;
        entity.UpdatedBy = updateId;

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

        entity.Active    = "No";
        entity.UpdatedBy = User.TryGetUserId();
        entity.UpdateDt = _dateTimeProvider.NowLocal;
        await _db.SaveChangesAsync(ct);

        // 🔎 Log
        await _audit.LogAsync("Roles.Delete", $"Deleted role {entity.Name} (Id={entity.Id})", ct);

        return NoContent();
    }
}

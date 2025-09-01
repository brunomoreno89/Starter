// Controllers/RolePermissionsController.cs
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
public class RolePermissionsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAuditLogger _audit;

    public RolePermissionsController(AppDbContext db, IAuditLogger audit)
    {
        _db = db;
        _audit = audit;
    }

    // GET /api/rolepermissions/{roleId}
    [Authorize(Policy = "Perm:RolePermissions.Assign")]
    [HttpGet("{roleId:int}")]
    public async Task<ActionResult<IEnumerable<PermissionDto>>> GetByRole(int roleId, CancellationToken ct)
    {
        var exists = await _db.Roles.AsNoTracking().AnyAsync(r => r.Id == roleId, ct);
        if (!exists) return NotFound();

        var perms = await _db.RolePermissions
            .AsNoTracking()
            .Where(rp => rp.RoleId == roleId)
            .OrderBy(rp => rp.Permission.Name)
            .Select(rp => new PermissionDto
            {
                Id = rp.PermissionId,
                Name = rp.Permission.Name,
                Description = rp.Permission.Description
            })
            .ToListAsync(ct);

        return Ok(perms);
    }

    // POST /api/rolepermissions/assign
    [Authorize(Policy = "Perm:RolePermissions.Assign")]
    [HttpPost("assign")]
    public async Task<IActionResult> Assign(
        [FromBody] RolePermissionAssignDto dto,
        [FromServices] IValidator<RolePermissionAssignDto>? validator = null,
        CancellationToken ct = default)
    {
        if (dto is null) return BadRequest("Body is mandatory.");

        if (validator is not null)
        {
            var val = await validator.ValidateAsync(dto, ct);
            if (!val.IsValid) return BadRequest(val.Errors);
        }

        // role existe?
        var roleExists = await _db.Roles.AsNoTracking().AnyAsync(r => r.Id == dto.RoleId, ct);
        if (!roleExists) return BadRequest("Ivalid Role.");

        // valida IDs de permissão
        var requestedIds = dto.PermissionIds?.Distinct().ToArray() ?? Array.Empty<int>();
        var validIds = await _db.Permissions
            .AsNoTracking()
            .Where(p => requestedIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync(ct);

        if (validIds.Count != requestedIds.Length)
            return BadRequest("One or more permissins are not valid.");

        // remove tudo e reatribui (estratégia replace-all)
        var current = await _db.RolePermissions
            .Where(rp => rp.RoleId == dto.RoleId)
            .ToListAsync(ct);

        _db.RolePermissions.RemoveRange(current);

        foreach (var pid in validIds)
            _db.RolePermissions.Add(new RolePermission { RoleId = dto.RoleId, PermissionId = pid });

        await _db.SaveChangesAsync(ct);

        // 🔎 Log
        await _audit.LogAsync(
            "RolePermissions.Assign",
            $"Replaced role {dto.RoleId} permissions: old={current.Count}, new={validIds.Count}",
            ct);

        return NoContent();
    }
}

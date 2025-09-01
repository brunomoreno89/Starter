// Controllers/UserRolesController.cs
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
public class UserRolesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAuditLogger _audit;

    public UserRolesController(AppDbContext db, IAuditLogger audit)
    {
        _db = db;
        _audit = audit;
    }

    // GET /api/userroles/{userId}
    [Authorize(Policy = "Perm:UserRoles.Assign")]
    [HttpGet("{userId:int}")]
    public async Task<ActionResult<IEnumerable<RoleDto>>> GetByUser(int userId, CancellationToken ct)
    {
        var exists = await _db.Users.AsNoTracking().AnyAsync(u => u.Id == userId, ct);
        if (!exists) return NotFound();

        var roles = await _db.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .OrderBy(ur => ur.Role.Name)
            .Select(ur => new RoleDto
            {
                Id = ur.RoleId,
                Name = ur.Role.Name,
                Description = ur.Role.Description
            })
            .ToListAsync(ct);

        return Ok(roles);
    }

    // POST /api/userroles/assign
    [Authorize(Policy = "Perm:UserRoles.Assign")]
    [HttpPost("assign")]
    public async Task<IActionResult> Assign(
        [FromBody] UserRoleAssignDto dto,
        [FromServices] IValidator<UserRoleAssignDto>? validator = null,
        CancellationToken ct = default)
    {
        if (dto is null) return BadRequest("Body is mandatory.");

        if (validator is not null)
        {
            var val = await validator.ValidateAsync(dto, ct);
            if (!val.IsValid) return BadRequest(val.Errors);
        }

        // user existe?
        var userExists = await _db.Users.AsNoTracking().AnyAsync(u => u.Id == dto.UserId, ct);
        if (!userExists) return BadRequest("Invalid user.");

        // valida roles solicitadas
        var requestedIds = dto.RoleIds?.Distinct().ToArray() ?? Array.Empty<int>();
        var validIds = await _db.Roles
            .AsNoTracking()
            .Where(r => requestedIds.Contains(r.Id))
            .Select(r => r.Id)
            .ToListAsync(ct);

        if (validIds.Count != requestedIds.Length)
            return BadRequest("One or more profiles are not valid.");

        // estado atual
        var current = await _db.UserRoles
            .Where(ur => ur.UserId == dto.UserId)
            .ToListAsync(ct);

        // replace-all
        _db.UserRoles.RemoveRange(current);
        foreach (var rid in validIds)
            _db.UserRoles.Add(new UserRole { UserId = dto.UserId, RoleId = rid });

        await _db.SaveChangesAsync(ct);

        // 🔎 Log
        await _audit.LogAsync(
            "UserRoles.Assign",
            $"Replaced user {dto.UserId} roles: old={current.Count}, new={validIds.Count}",
            ct);

        return NoContent();
    }
}

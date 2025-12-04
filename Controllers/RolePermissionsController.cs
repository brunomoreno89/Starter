// Controllers/RolePermissionsController.cs
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Starter.Api.DTOs.Security;
using Starter.Api.Security;
using Starter.Api.Services;
using System.Threading;

namespace Starter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RolePermissionsController : ControllerBase
{
    private readonly IRolePermissionService _rolePermissions;
    private readonly IAuditLogger _audit;

    public RolePermissionsController(
        IRolePermissionService rolePermissions,
        IAuditLogger audit)
    {
        _rolePermissions = rolePermissions;
        _audit = audit;
    }

    // GET /api/rolepermissions/{roleId}
    [Authorize(Policy = "Perm:RolePermissions.Assign")]
    [HttpGet("{roleId:int}")]
    public async Task<ActionResult<IEnumerable<PermissionDto>>> GetByRole(
        int roleId,
        CancellationToken ct)
    {
        var exists = await _rolePermissions.RoleExistsAsync(roleId, ct);
        if (!exists) return NotFound();

        var perms = await _rolePermissions.GetByRoleAsync(roleId, ct);
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

        // Role existe?
        var roleExists = await _rolePermissions.RoleExistsAsync(dto.RoleId, ct);
        if (!roleExists) return BadRequest("Invalid Role.");

        (int oldCount, int newCount) counters;
        try
        {
            counters = await _rolePermissions.AssignAsync(dto, ct);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("permissions are not valid"))
        {
            return BadRequest(ex.Message);
        }

        var currentUserId = User.TryGetUserId();

        await _audit.LogAsync(
            "RolePermissions.Assign",
            currentUserId,
            $"Replaced role {dto.RoleId} permissions: old={counters.oldCount}, new={counters.newCount}",
            ct);

        return NoContent();
    }
}

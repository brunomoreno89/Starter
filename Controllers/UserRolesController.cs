// Controllers/UserRolesController.cs
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
public class UserRolesController : ControllerBase
{
    private readonly IUserRoleService _userRoles;
    private readonly IAuditLogger _audit;

    public UserRolesController(
        IUserRoleService userRoles,
        IAuditLogger audit)
    {
        _userRoles = userRoles;
        _audit = audit;
    }

    // GET /api/userroles/{userId}
    [Authorize(Policy = "Perm:UserRoles.Assign")]
    [HttpGet("{userId:int}")]
    public async Task<ActionResult<IEnumerable<RoleDto>>> GetByUser(
        int userId,
        CancellationToken ct)
    {
        var exists = await _userRoles.UserExistsAsync(userId, ct);
        if (!exists) return NotFound();

        var roles = await _userRoles.GetByUserAsync(userId, ct);
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
        var userExists = await _userRoles.UserExistsAsync(dto.UserId, ct);
        if (!userExists) return BadRequest("Invalid user.");

        (int oldCount, int newCount) counters;
        try
        {
            counters = await _userRoles.AssignAsync(dto, ct);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("profiles are not valid"))
        {
            return BadRequest(ex.Message);
        }

        var currentUserId = User.TryGetUserId();

        await _audit.LogAsync(
            "UserRoles.Assign",
            currentUserId,
            $"Replaced user {dto.UserId} roles: old={counters.oldCount}, new={counters.newCount}",
            ct);

        return NoContent();
    }
}

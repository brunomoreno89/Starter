// Controllers/RolesController.cs
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Starter.Api.DTOs.Security;
using Starter.Api.Security;
using Starter.Api.Services;

namespace Starter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roles;
    private readonly IAuditLogger _audit;

    public RolesController(
        IRoleService roles,
        IAuditLogger audit)
    {
        _roles = roles;
        _audit = audit;
    }

    [Authorize(Policy = "Perm:Roles.Read")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RoleDto>>> List(CancellationToken ct)
    {
        var data = await _roles.ListAsync(ct);
        return Ok(data);
    }

    [Authorize(Policy = "Perm:Roles.Read")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<RoleDto>> GetOne(int id, CancellationToken ct)
    {
        var dto = await _roles.GetByIdAsync(id, ct);
        if (dto is null) return NotFound();
        return Ok(dto);
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
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Name is mandatory.");

        if (await _roles.NameExistsAsync(name!, null, ct))
            return Conflict("Role name already exists.");

        var currentUserId = User.TryGetUserId();

        var created = await _roles.CreateAsync(dto, currentUserId, ct);

        await _audit.LogAsync("Roles.Create", currentUserId,
            $"Created role {created.Name} (Id={created.Id})", ct);

        return CreatedAtAction(nameof(GetOne), new { id = created.Id }, created);
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

        var name = dto.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Name is mandatory.");

        if (await _roles.NameExistsAsync(name!, id, ct))
            return Conflict("Role name already exists.");

        var currentUserId = User.TryGetUserId();

        RoleDto? updated;
        try
        {
            updated = await _roles.UpdateAsync(id, dto, currentUserId, ct);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Active must be"))
        {
            return BadRequest(ex.Message);
        }

        if (updated is null) return NotFound();

        await _audit.LogAsync("Roles.Update", currentUserId,
            $"Updated role {updated.Name} (Id={updated.Id})", ct);

        return NoContent();
    }

    [Authorize(Policy = "Perm:Roles.Delete")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var currentUserId = User.TryGetUserId();

        var ok = await _roles.SoftDeleteAsync(id, currentUserId, ct);
        if (!ok) return NotFound();

        await _audit.LogAsync("Roles.Delete", currentUserId,
            $"Deleted role Id={id}", ct);

        return NoContent();
    }
}

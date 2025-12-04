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
public class PermissionsController : ControllerBase
{
    private readonly IPermissionService _permissions;
    private readonly IAuditLogger _audit;

    public PermissionsController(
        IPermissionService permissions,
        IAuditLogger audit)
    {
        _permissions = permissions;
        _audit = audit;
    }

    [Authorize(Policy = "Perm:Permissions.Read")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PermissionDto>>> List(CancellationToken ct)
    {
        var data = await _permissions.ListAsync(ct);
        return Ok(data);
    }

    [Authorize(Policy = "Perm:Permissions.Read")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PermissionDto>> GetOne(int id, CancellationToken ct)
    {
        var dto = await _permissions.GetByIdAsync(id, ct);
        if (dto is null) return NotFound();
        return Ok(dto);
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
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Name is mandatory.");

        if (await _permissions.NameExistsAsync(name, null, ct))
            return Conflict("Permission name already exists.");

        var currentUserId = User.TryGetUserId();

        var created = await _permissions.CreateAsync(dto, currentUserId, ct);

        await _audit.LogAsync("Permissions.Create", currentUserId,
            $"Created permission {created.Name} (Id={created.Id})", ct);

        return CreatedAtAction(nameof(GetOne), new { id = created.Id }, created);
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

        var name = dto.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Name is mandatory.");

        if (await _permissions.NameExistsAsync(name, id, ct))
            return Conflict("Permission name already exists.");

        var currentUserId = User.TryGetUserId();

        PermissionDto? updated;
        try
        {
            updated = await _permissions.UpdateAsync(id, dto, currentUserId, ct);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Active must be"))
        {
            return BadRequest(ex.Message);
        }

        if (updated is null) return NotFound();

        await _audit.LogAsync("Permissions.Update", currentUserId,
            $"Updated permission {updated.Name} (Id={updated.Id})", ct);

        return NoContent();
    }

    [Authorize(Policy = "Perm:Permissions.Delete")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var currentUserId = User.TryGetUserId();

        var ok = await _permissions.SoftDeleteAsync(id, currentUserId, ct);
        if (!ok) return NotFound();

        await _audit.LogAsync("Permissions.Delete", currentUserId,
            $"Deleted permission Id={id}", ct);

        return NoContent();
    }
}

using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Starter.Api.DTOs.Users;
using Starter.Api.Security;
using Starter.Api.Services;

namespace Starter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] 
public class UsersController : ControllerBase
{
    private readonly IUserService _users;
    private readonly IAuditLogger _audit;

    public UsersController(
        IUserService users,
        IAuditLogger audit)
    {
        _users = users;
        _audit = audit;
    }

    // GET /api/users
    [Authorize(Policy = "Perm:Users.Read")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> List(CancellationToken ct)
    {
        var data = await _users.ListAsync(ct);
        return Ok(data);
    }

    // GET /api/users/{id}
    [Authorize(Policy = "Perm:Users.Read")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserDto>> GetById(int id, CancellationToken ct)
    {
        var dto = await _users.GetByIdAsync(id, ct);
        if (dto == null) return NotFound();
        return Ok(dto);
    }

    // POST /api/users
    [Authorize(Policy = "Perm:Users.Create")]
    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(
        [FromBody] UserCreateDto body,
        [FromServices] IValidator<UserCreateDto>? validator = null,
        CancellationToken ct = default)
    {
        if (body is null) return BadRequest("Mandatory Body");

        if (validator is not null)
        {
            var val = await validator.ValidateAsync(body, ct);
            if (!val.IsValid) return BadRequest(val.Errors);
        }

        // Duplicidades (via serviço)
        if (await _users.UsernameExistsAsync(body.Username.Trim(), null, ct))
            return Conflict("Username already in use.");

        if (await _users.EmailExistsAsync(body.Email.Trim(), null, ct))
            return Conflict("Email already in use.");

        var creatorId = User.TryGetUserId();

        var created = await _users.CreateAsync(body, creatorId, ct);

        await _audit.LogAsync("Users.Create", creatorId, $"Created user {created.Username}", ct);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // PUT /api/users/{id}
    [Authorize(Policy = "Perm:Users.Update")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserDto>> Update(
        int id,
        [FromBody] UserUpdateDto body,
        [FromServices] IValidator<UserUpdateDto>? validator = null,
        CancellationToken ct = default)
    {
        if (body is null) return BadRequest("Mandatory Body");

        if (validator is not null)
        {
            var val = await validator.ValidateAsync(body, ct);
            if (!val.IsValid) return BadRequest(val.Errors);
        }

        // Duplicidades (se vier username/email novos)
        if (!string.IsNullOrWhiteSpace(body.Username))
        {
            var newUsername = body.Username.Trim();
            if (await _users.UsernameExistsAsync(newUsername, id, ct))
                return Conflict("Username already in use.");
        }

        if (!string.IsNullOrWhiteSpace(body.Email))
        {
            var newEmail = body.Email.Trim();
            if (await _users.EmailExistsAsync(newEmail, id, ct))
                return Conflict("Email already in use.");
        }

        var updaterId = User.TryGetUserId();

        UserDto? updated;
        try
        {
            updated = await _users.UpdateAsync(id, body, updaterId, ct);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Active must be"))
        {
            return BadRequest(ex.Message);
        }

        if (updated == null) return NotFound();

        await _audit.LogAsync("Users.Update", updaterId, $"Updated user {updated.Username}", ct);

        return Ok(updated);
    }

    // DELETE /api/users/{id}
    [Authorize(Policy = "Perm:Users.Delete")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        // (Opcional) impedir auto-exclusão pelo username:
        // aqui você teria que buscar o usuário primeiro se quiser essa regra via service

        var deleterId = User.TryGetUserId();

        var ok = await _users.SoftDeleteAsync(id, deleterId, ct);
        if (!ok) return NotFound();

        await _audit.LogAsync("Users.Delete", deleterId, $"Soft-deleted user #{id}", ct);

        return NoContent();
    }
}

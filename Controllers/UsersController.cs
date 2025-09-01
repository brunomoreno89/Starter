using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Starter.Api.Data;
using Starter.Api.DTOs.Users;
using Starter.Api.Models;
using Starter.Api.Services;
using System.Collections.Generic;
using Starter.Api.Security;

namespace Starter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // exige auth por padrão (suas policies continuam nos endpoints)
public class UsersController : ControllerBase
{
    private static readonly HashSet<string> AllowedRoles = new(StringComparer.OrdinalIgnoreCase)
    { "Admin", "User" };

    private readonly AppDbContext _db;
    private readonly PasswordHasher _hasher;
    private readonly IAuditLogger _audit;



    public UsersController(AppDbContext db, PasswordHasher hasher, IAuditLogger audit)
    {
        _db = db;
        _hasher = hasher;
        _audit = audit;
    }

    // GET /api/users
    [Authorize(Policy = "Perm:Users.Read")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> List(CancellationToken ct)
    {
        // 1) carrega todos os nomes (Id -> DisplayName) de uma vez
        var names = await _db.Users
            .AsNoTracking()
            .Select(x => new { x.Id, Display = x.Name ?? x.Username })
            .ToDictionaryAsync(x => x.Id, x => x.Display, ct);

        // 2) carrega usuários
        var users = await _db.Users
            .AsNoTracking()
            .OrderBy(u => u.Id)
            .ToListAsync(ct);

        // 3) projeta em memória sem N+1
        var result = users.Select(u => new UserDto
        {
            Id             = u.Id,
            Username       = u.Username,
            Name           = u.Name,
            Email          = u.Email,
            Active         = u.Active,
            CreationDt     = u.CreationDt,
            CreatedBy      = u.CreatedBy,
            CreatedByName  = (u.CreatedBy.HasValue && names.TryGetValue(u.CreatedBy.Value, out var cName)) ? cName : null,
            UpdatedDt      = u.UpdatedDt,
            UpdatedBy      = u.UpdatedBy,
            UpdatedByName  = (u.UpdatedBy.HasValue && names.TryGetValue(u.UpdatedBy.Value, out var uName)) ? uName : null
        }).ToList();

        return Ok(result);
    }

    // GET /api/users/{id}
    [Authorize(Policy = "Perm:Users.Read")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserDto>> GetById(int id, CancellationToken ct)
    {
        var u = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (u == null) return NotFound();

        // carrega nomes apenas se necessário
        string? createdByName = null, updatedByName = null;

        if (u.CreatedBy.HasValue)
            createdByName = await _db.Users
                .AsNoTracking()
                .Where(x => x.Id == u.CreatedBy.Value)
                .Select(x => x.Name ?? x.Username)
                .FirstOrDefaultAsync(ct);

        if (u.UpdatedBy.HasValue)
            updatedByName = await _db.Users
                .AsNoTracking()
                .Where(x => x.Id == u.UpdatedBy.Value)
                .Select(x => x.Name ?? x.Username)
                .FirstOrDefaultAsync(ct);

        var dto = new UserDto
        {
            Id             = u.Id,
            Username       = u.Username,
            Name           = u.Name,
            Email          = u.Email,
            Active         = u.Active,
            CreationDt     = u.CreationDt,
            CreatedBy      = u.CreatedBy,
            CreatedByName  = createdByName,
            UpdatedDt      = u.UpdatedDt,
            UpdatedBy      = u.UpdatedBy,
            UpdatedByName  = updatedByName
        };

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

       

        // Duplicidades
        if (await _db.Users.AnyAsync(x => x.Username == body.Username, ct))
            return Conflict("Username already in use.");

        if (await _db.Users.AnyAsync(x => x.Email == body.Email, ct))
            return Conflict("Email already in use.");

        var nowUtc = DateTime.UtcNow;
        var creatorId = User.TryGetUserId(); 

        var user = new User
        {
            Username = body.Username.Trim(),
            Email = body.Email.Trim(),
            Name = body.Name.Trim(),
            //Role     = role,
            PasswordHash = _hasher.Hash(body.Password),
            CreationDt = nowUtc,
            Active = "Yes",
            CreatedBy = creatorId
            
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("Users.Create", $"Created user {user.Username}", ct);

        var dto = new UserDto
        {
            Id        = user.Id,
            Username  = user.Username,
            Name      = user.Name,
            Email     = user.Email,
            Active    = user.Active,
            CreatedBy = user.CreatedBy,
            // Role   = user.Role
            CreationDt = user.CreationDt
        };

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, dto);
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

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (user == null) return NotFound();

        // ---- Duplicidades e updates opcionais ----

        // Username
        if (!string.IsNullOrWhiteSpace(body.Username))
        {
            var newUsername = body.Username.Trim();
            var usernameExists = await _db.Users
                .AnyAsync(x => x.Id != id && x.Username == newUsername, ct);
            if (usernameExists) return Conflict("Username already in use.");
            user.Username = newUsername;
        }

        // Name (pode ser null)
        if (body.Name != null) // atenção: aqui diferenciamos "não enviado" de "enviado vazio"
        {
            user.Name = string.IsNullOrWhiteSpace(body.Name) ? null : body.Name.Trim();
        }

        // Email
        if (!string.IsNullOrWhiteSpace(body.Email))
        {
            var newEmail = body.Email.Trim();
            var emailExists = await _db.Users
                .AnyAsync(x => x.Id != id && x.Email == newEmail, ct);
            if (emailExists) return Conflict("Email already in use.");
            user.Email = newEmail;
        }

        // Password
        if (!string.IsNullOrWhiteSpace(body.Password))
        {
            user.PasswordHash = _hasher.Hash(body.Password);
        }

        // Active (Yes/No)
        if (!string.IsNullOrWhiteSpace(body.Active))
        {
            var v = body.Active.Trim();
            var yes = v.Equals("Yes", StringComparison.OrdinalIgnoreCase);
            var no  = v.Equals("No" , StringComparison.OrdinalIgnoreCase);
            if (!yes && !no) return BadRequest("Active must be 'Yes' or 'No'.");
            user.Active = yes ? "Yes" : "No";
        }

        // ---- Auditoria ----
        user.UpdatedBy = User.TryGetUserId();
        user.UpdatedDt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("Users.Update", $"Updated user {user.Username}", ct);


        // ---- Monta DTO de retorno ----
        string? createdByName = null, updatedByName = null;

        if (user.CreatedBy.HasValue)
            createdByName = await _db.Users.AsNoTracking()
                .Where(x => x.Id == user.CreatedBy.Value)
                .Select(x => x.Name ?? x.Username)
                .FirstOrDefaultAsync(ct);

        if (user.UpdatedBy.HasValue)
            updatedByName = await _db.Users.AsNoTracking()
                .Where(x => x.Id == user.UpdatedBy.Value)
                .Select(x => x.Name ?? x.Username)
                .FirstOrDefaultAsync(ct);

        var dto = new UserDto
        {
            Id             = user.Id,
            Username       = user.Username,
            Name           = user.Name,
            Email          = user.Email,
            Active         = user.Active,
            CreationDt     = user.CreationDt,
            CreatedBy      = user.CreatedBy,
            CreatedByName  = createdByName,
            UpdatedDt      = user.UpdatedDt,
            UpdatedBy      = user.UpdatedBy,
            UpdatedByName  = updatedByName
        };

        return Ok(dto);
    }

    // DELETE /api/users/{id}
    [Authorize(Policy = "Perm:Users.Delete")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (user == null) return NotFound();

        // (Opcional) impedir auto-exclusão pelo username
        if (string.Equals(User?.Identity?.Name, user.Username, StringComparison.Ordinal))
            return BadRequest("You cannot delete your own account.");

        // Soft delete + auditoria
        user.Active    = "No";
        user.UpdatedBy = User.TryGetUserId();
        user.UpdatedDt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("Users.Delete", $"Soft-deleted user {user.Username}", ct);

        return NoContent();
    }

}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Starter.Api.Data;
using Starter.Api.DTOs;
using Starter.Api.DTOs.Auth;
using Starter.Api.Services;

namespace Starter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly PasswordHasher _hasher;
    private readonly JwtService _jwt;
    private readonly IAuditLogger _audit;

    public AuthController(AppDbContext db, PasswordHasher hasher, JwtService jwt, IAuditLogger audit)
    {
        _db = db;
        _hasher = hasher;
        _jwt = jwt;
        _audit = audit;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest req, CancellationToken ct = default)
    {
        if (req is null || string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest("Missing credentials.");

        var login = req.Username.Trim();

        var user = await _db.Users.FirstOrDefaultAsync(
            u => u.Username == login || u.Email == login, ct);

        const string invalid = "Invalid credentials.";

        if (user is null)
        {
            await _audit.LogAsync("Auth.Login.Fail", null, $"Login failed for '{login}': user not found", ct);
            return Unauthorized(invalid);
        }

        if (!_hasher.Verify(req.Password, user.PasswordHash))
        {
            await _audit.LogAsync("Auth.Login.Fail", user.Id, $"Login failed for '{login}': invalid password", ct);
            return Unauthorized(invalid);
        }

        // Ativo?
        var isActive = string.Equals(user.Active?.Trim(), "Yes", StringComparison.OrdinalIgnoreCase);
        if (!isActive)
        {
            await _audit.LogAsync("Auth.Login.Blocked", user.Id, $"Inactive user '{login}'", ct);
            return Unauthorized("User blocked.");
        }

        // Sucesso
        await _audit.LogAsync("Auth.Login", user.Id, $"User '{user.Username}' logged in", ct);

        var token = _jwt.CreateToken(user);

        return Ok(new AuthResponse
        {
            Token    = token,
            Username = user.Username,
            Role     = user.Role
        });
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.CurrentPassword) || string.IsNullOrWhiteSpace(req.NewPassword))
            return BadRequest(new { message = "Both current and new passwords are required." });
        if (req.NewPassword.Length < 6)
            return BadRequest(new { message = "New password must have at least 6 characters." });

        // Identifica o usuário via Name / UniqueName / Identity.Name…
        var username =
            User.FindFirstValue(ClaimTypes.Name) ??
            User.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value ??
            User.FindFirst("unique_name")?.Value ??
            User.Identity?.Name;

        Starter.Api.Models.User? user = null;

        if (!string.IsNullOrWhiteSpace(username))
        {
            user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username, ct);
        }
        else
        {
            // fallback por ID (sub/nameidentifier)
            var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
                      User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(sub, out var uid))
                user = await _db.Users.FirstOrDefaultAsync(u => u.Id == uid, ct);
        }

        if (user == null) return Unauthorized();

        if (!_hasher.Verify(req.CurrentPassword, user.PasswordHash))
            return BadRequest(new { message = "Current password is incorrect." });

        user.PasswordHash = _hasher.Hash(req.NewPassword);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync("Auth.ChangePassword", user.Id, "Password updated", ct);

        return NoContent();
    }
}

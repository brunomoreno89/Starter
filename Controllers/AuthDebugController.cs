// Controllers/AuthDebugController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[Route("api/auth")]
public class AuthDebugController : ControllerBase
{
    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var list = User.Claims
            .Select(c => new { c.Type, c.Value })
            .OrderBy(c => c.Type)
            .ToList();

        return Ok(new {
            name = User.Identity?.Name,
            claims = list
        });
    }
}


namespace Starter.Api.DTOs;

public class LoginRequest
{
    public string Username { get; set; } = default!;
    public string Password { get; set; } = default!;
}

public class AuthResponse
{
    public string Token { get; set; } = default!;
    public string Username { get; set; } = default!;
    public string Role { get; set; } = default!;
}

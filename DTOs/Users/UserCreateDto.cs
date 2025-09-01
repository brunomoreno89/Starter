// DTOs/Users/UserCreateDto.cs
using System.ComponentModel.DataAnnotations;

namespace Starter.Api.DTOs.Users;

public class UserCreateDto
{
    [Required, MinLength(3)]
    public string Username { get; set; } = default!;

    // Name é opcional no seu schema (NULL no banco)
    public string? Name { get; set; }

    [Required, EmailAddress]
    public string Email { get; set; } = default!;

    [Required, MinLength(6)]
    public string Password { get; set; } = default!;
}

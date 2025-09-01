using System.ComponentModel.DataAnnotations;

namespace Starter.Api.DTOs.Users;

public class UserUpdateDto
{
    [Required, MinLength(3)]
    public string Username { get; set; } = default!;
    public string? Name { get; set; } = default!;

    [Required, EmailAddress]
    public string Email { get; set; } = default!;

    // "Admin" ou "User" (case-insensitive). Se nulo/empty, mantém o atual.
    //public string? Role { get; set; }

    public string? Active { get; set; }

    // Opcional: se enviado, troca a senha
    public string? Password { get; set; }

    public DateTime? UpdatetdDt { get; set; } = default!;
    public int? UpdatedBy { get; set; } = default!;
}

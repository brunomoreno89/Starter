namespace Starter.Api.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = default!;

    // No banco é NULLABLE, então aqui também deve ser
    public string? Name { get; set; }    

    public string Email { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public string Role { get; set; } = "User";

    // No banco: CreationDt é NULLABLE → DateTime?
    public DateTime? CreationDt { get; set; }  

    // No banco: UpdatedDt é NULLABLE → DateTime?
    public DateTime? UpdatedDt { get; set; }  

    // No banco: CreatedBy/UpdatedBy são NULLABLE → int?
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }

    // No banco: Active é NOT NULL, mas pode vir vazio → string normal
    public string Active { get; set; }

    // Relações
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

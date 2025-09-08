namespace Starter.Api.Models;

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public DateTime? CreationDt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdateDt { get; set; }
    public int? UpdatedBy { get; set; }
    public string? Active { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

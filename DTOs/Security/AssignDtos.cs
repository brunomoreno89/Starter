// DTOs/Security/AssignDtos.cs
namespace Starter.Api.DTOs.Security;
public class RolePermissionAssignDto
{
    public int RoleId { get; set; }
    public List<int> PermissionIds { get; set; } = new();
}
public class UserRoleAssignDto
{
    public int UserId { get; set; }
    public List<int> RoleIds { get; set; } = new();
}
namespace Starter.Api.DTOs.Logs;
public class LogListItemDto
{
    public int Id { get; set; }
    public DateTime ExecDate { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = "";
    public string? Name { get; set; }
    public int? RoleId { get; set; }
    public string RoleName { get; set; } = "";
    public int? PermissionId { get; set; }
    public string PermissionName { get; set; } = "";
    public string Description { get; set; } = "";
}
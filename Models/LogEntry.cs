namespace Starter.Api.Models;

public class LogEntry
{
    public int Id { get; set; }
    public int UserId { get; set; }          // NOT NULL
    public int? RoleId { get; set; }          // NOT NULL
    public int? PermissionId { get; set; }    // NOT NULL
    public DateTime ExecDate { get; set; }   // DATE no MySQL
    public string Description { get; set; } = default!;
}

namespace Starter.Api.DTOs.Users;

public class UserDto
{
    public int Id { get; set; }
    public string? Username { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    // public string? Role     { get; set; }

    public DateTime? CreationDt { get; set; }
    public int? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public string? Active { get; set; }

    public DateTime? UpdatedDt { get; set; }
    public int? UpdatedBy { get; set; }
    public string? UpdatedByName  { get; set; } 
}

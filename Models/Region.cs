
namespace Starter.Api.Models;

public class Region
{
    public int Id { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedByUserId { get; set; }
    public string? Active { get; set; }
    
}

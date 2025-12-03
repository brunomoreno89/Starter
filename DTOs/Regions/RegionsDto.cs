
namespace Starter.Api.DTOs.Regions;

public class RegionsDto
{
    public int Id { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedByUserId { get; set; }
    public string? Active { get; set; }

    public string? CreatedByName { get; set; }
    public string? UpdatedByName { get; set; }
    
}

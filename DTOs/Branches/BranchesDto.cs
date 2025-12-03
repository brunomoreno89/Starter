
namespace Starter.Api.DTOs.Branches;

public class BranchesDto
{
    public int Id { get; set; }
    public string? BranchCode { get; set; }
    public string? Description { get; set; }
    public int RegionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedByUserId { get; set; }
    public string? Active { get; set; }

    public string? CreatedByName { get; set; }
    public string? UpdatedByName { get; set; }
    
}

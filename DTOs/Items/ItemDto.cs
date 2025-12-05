
namespace Starter.Api.DTOs.Items;

public class ItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? Active { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? CreatedByUserId { get; set; }
    public int? UpdatedByUserId { get; set; }

    public string? CreatedByName { get; set; }
    public string? UpdatedByName { get; set; }

    
}

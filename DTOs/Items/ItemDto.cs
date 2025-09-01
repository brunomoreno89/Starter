
namespace Starter.Api.DTOs.Items;

public class ItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
}

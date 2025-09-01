namespace Starter.Api.DTOs.Security;

public class RoleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}

public class RoleCreateDto
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}

public class RoleUpdateDto
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}

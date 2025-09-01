namespace Starter.Api.DTOs.Security;

public class PermissionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}

public class PermissionCreateDto
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}

public class PermissionUpdateDto
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}

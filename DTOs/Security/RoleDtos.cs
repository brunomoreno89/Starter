namespace Starter.Api.DTOs.Security;

public class RoleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime? CreationDt { get; set; }
    public int? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public string? Active { get; set; }

    public DateTime? UpdateDt { get; set; }
    public int? UpdatedBy { get; set; }
    public string? UpdatedByName  { get; set; }
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
    public string? Active { get; set; }
    public DateTime? UpdateDt { get; set; } = default!;
    public int? UpdatedBy { get; set; } = default!;
}

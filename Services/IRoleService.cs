// Services/IRoleService.cs
using Starter.Api.DTOs.Security;

namespace Starter.Api.Services;

public interface IRoleService
{
    Task<IReadOnlyList<RoleDto>> ListAsync(CancellationToken ct);
    Task<RoleDto?> GetByIdAsync(int id, CancellationToken ct);

    Task<bool> NameExistsAsync(string name, int? ignoreId, CancellationToken ct);

    Task<RoleDto> CreateAsync(
        RoleCreateDto dto,
        int? currentUserId,
        CancellationToken ct);

    Task<RoleDto?> UpdateAsync(
        int id,
        RoleUpdateDto dto,
        int? currentUserId,
        CancellationToken ct);

    Task<bool> SoftDeleteAsync(
        int id,
        int? currentUserId,
        CancellationToken ct);
}

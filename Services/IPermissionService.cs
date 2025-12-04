using Starter.Api.DTOs.Security;

namespace Starter.Api.Services;

public interface IPermissionService
{
    Task<IReadOnlyList<PermissionDto>> ListAsync(CancellationToken ct);
    Task<PermissionDto?> GetByIdAsync(int id, CancellationToken ct);

    Task<bool> NameExistsAsync(string name, int? ignoreId, CancellationToken ct);

    Task<PermissionDto> CreateAsync(
        PermissionCreateDto dto,
        int? currentUserId,
        CancellationToken ct);

    Task<PermissionDto?> UpdateAsync(
        int id,
        PermissionUpdateDto dto,
        int? currentUserId,
        CancellationToken ct);

    Task<bool> SoftDeleteAsync(
        int id,
        int? currentUserId,
        CancellationToken ct);
}

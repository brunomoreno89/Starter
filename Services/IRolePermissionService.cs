// Services/IRolePermissionService.cs
using Starter.Api.DTOs.Security;

namespace Starter.Api.Services;

public interface IRolePermissionService
{
    Task<bool> RoleExistsAsync(int roleId, CancellationToken ct);

    Task<IReadOnlyList<PermissionDto>> GetByRoleAsync(
        int roleId,
        CancellationToken ct);

    /// <summary>
    /// Valida role/permissões e faz o replace-all.
    /// Retorna (oldCount, newCount).
    /// </summary>
    Task<(int oldCount, int newCount)> AssignAsync(
        RolePermissionAssignDto dto,
        CancellationToken ct);
}

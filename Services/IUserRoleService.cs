// Services/IUserRoleService.cs
using Starter.Api.DTOs.Security;

namespace Starter.Api.Services;

public interface IUserRoleService
{
    Task<bool> UserExistsAsync(int userId, CancellationToken ct);

    Task<IReadOnlyList<RoleDto>> GetByUserAsync(
        int userId,
        CancellationToken ct);

    /// <summary>
    /// Valida usuário/roles e faz o replace-all.
    /// Retorna (oldCount, newCount).
    /// </summary>
    Task<(int oldCount, int newCount)> AssignAsync(
        UserRoleAssignDto dto,
        CancellationToken ct);
}

// Services/IBranchService.cs
using Starter.Api.DTOs.Branches;

namespace Starter.Api.Services;

public interface IBranchService
{
    Task<IReadOnlyList<BranchesDto>> ListAsync(CancellationToken ct);
    Task<BranchesDto?> GetByIdAsync(int id, CancellationToken ct);

    Task<BranchesDto> CreateAsync(
        BranchesDto dto,
        int? currentUserId,
        CancellationToken ct);

    Task<BranchesDto?> UpdateAsync(
        int id,
        BranchesDto dto,
        int? currentUserId,
        CancellationToken ct);

    Task<bool> SoftDeleteAsync(
        int id,
        int? currentUserId,
        CancellationToken ct);
}

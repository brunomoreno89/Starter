// Services/IRegionService.cs
using Starter.Api.DTOs.Regions;

namespace Starter.Api.Services;

public interface IRegionService
{
    Task<IReadOnlyList<RegionsDto>> ListAsync(CancellationToken ct);
    Task<RegionsDto?> GetByIdAsync(int id, CancellationToken ct);

    Task<RegionsDto> CreateAsync(
        RegionsDto dto,
        int? currentUserId,
        CancellationToken ct);

    Task<RegionsDto?> UpdateAsync(
        int id,
        RegionsDto dto,
        int? currentUserId,
        CancellationToken ct);

    Task<bool> SoftDeleteAsync(
        int id,
        int? currentUserId,
        CancellationToken ct);
}

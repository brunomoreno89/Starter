// Services/IHolidaysService.cs
using Starter.Api.DTOs.Holidays;

namespace Starter.Api.Services;

public interface IHolidaysService
{
    Task<IReadOnlyList<HolidaysDto>> ListAsync(CancellationToken ct);
    Task<HolidaysDto?> GetByIdAsync(int id, CancellationToken ct);

    Task<HolidaysDto> CreateAsync(
        HolidaysDto dto,
        int? currentUserId,
        CancellationToken ct);

    Task<HolidaysDto?> UpdateAsync(
        int id,
        HolidaysDto dto,
        int? currentUserId,
        CancellationToken ct);

    Task<bool> SoftDeleteAsync(
        int id,
        int? currentUserId,
        CancellationToken ct);
}

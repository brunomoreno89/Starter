using Starter.Api.DTOs.Logs;

namespace Starter.Api.Services;

public interface ILogService
{
    Task<IReadOnlyList<LogListItemDto>> ListAsync(
        string? userTerm,
        DateTime startLocal00,
        DateTime endLocalNextDay00,
        int maxRows,
        CancellationToken ct);
}

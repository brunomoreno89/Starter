// Services/ISysDatesService.cs
using Starter.Api.DTOs.System;

namespace Starter.Api.Services;

public interface ISysDatesService
{
    Task<IReadOnlyList<SystemDto>> ListAsync(CancellationToken ct);
}

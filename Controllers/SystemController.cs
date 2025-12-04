// Controllers/SystemController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Starter.Api.DTOs.System;
using Starter.Api.Services;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SystemController : ControllerBase
{
    private readonly ISysDatesService _sysDates;

    public SystemController(ISysDatesService sysDates)
    {
        _sysDates = sysDates;
    }

    [HttpGet("dates")]
    public async Task<ActionResult<IEnumerable<SystemDto>>> List(CancellationToken ct)
    {
        var data = await _sysDates.ListAsync(ct);
        return Ok(data);
    }
}

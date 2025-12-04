// Controllers/HolidaysController.cs
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Starter.Api.DTOs.Holidays;
using Starter.Api.Security;
using Starter.Api.Services;
using System.Threading;

namespace Starter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HolidaysController : ControllerBase
{
    private readonly IHolidaysService _holidays;
    private readonly IAuditLogger _audit;

    public HolidaysController(
        IHolidaysService holidays,
        IAuditLogger audit)
    {
        _holidays = holidays;
        _audit = audit;
    }

    [Authorize(Policy = "Perm:Holidays.Read")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<HolidaysDto>>> List(CancellationToken ct)
    {
        var data = await _holidays.ListAsync(ct);
        return Ok(data);
    }

    [Authorize(Policy = "Perm:Holidays.Read")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<HolidaysDto>> GetOne(int id, CancellationToken ct)
    {
        var dto = await _holidays.GetByIdAsync(id, ct);
        if (dto is null) return NotFound();
        return Ok(dto);
    }

    [Authorize(Policy = "Perm:Holidays.Create")]
    [HttpPost]
    public async Task<ActionResult<HolidaysDto>> Create(
        [FromBody] HolidaysDto dto,
        [FromServices] IValidator<HolidaysDto>? validator = null,
        CancellationToken ct = default)
    {
        if (dto is null) return BadRequest("Body obrigatório.");

        if (validator is not null)
        {
            var val = await validator.ValidateAsync(dto, ct);
            if (!val.IsValid) return BadRequest(val.Errors);
        }

        var currentUserId = User.TryGetUserId();

        var created = await _holidays.CreateAsync(dto, currentUserId, ct);

        await _audit.LogAsync("Holidays.Create", currentUserId,
            $"Created holiday {created.Description} (Id={created.Id})", ct);

        return CreatedAtAction(nameof(GetOne), new { id = created.Id }, created);
    }

    [Authorize(Policy = "Perm:Holidays.Update")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] HolidaysDto dto,
        [FromServices] IValidator<HolidaysDto>? validator = null,
        CancellationToken ct = default)
    {
        if (dto is null) return BadRequest("Body obrigatório.");

        if (validator is not null)
        {
            var val = await validator.ValidateAsync(dto, ct);
            if (!val.IsValid) return BadRequest(val.Errors);
        }

        var currentUserId = User.TryGetUserId();

        HolidaysDto? updated;
        try
        {
            updated = await _holidays.UpdateAsync(id, dto, currentUserId, ct);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Active must be"))
        {
            return BadRequest(ex.Message);
        }

        if (updated is null) return NotFound();

        await _audit.LogAsync("Holidays.Update", currentUserId,
            $"Updated holiday {updated.Description} (Id={updated.Id})", ct);

        return NoContent();
    }

    [Authorize(Policy = "Perm:Holidays.Delete")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var currentUserId = User.TryGetUserId();

        var ok = await _holidays.SoftDeleteAsync(id, currentUserId, ct);
        if (!ok) return NotFound();

        await _audit.LogAsync("Holidays.Delete", currentUserId,
            $"Deleted holiday Id={id}", ct);

        return NoContent();
    }
}

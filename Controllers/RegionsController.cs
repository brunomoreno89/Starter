// Controllers/RegionsController.cs
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Starter.Api.DTOs.Regions;
using Starter.Api.Security;
using Starter.Api.Services;
using System.Threading;

namespace Starter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RegionsController : ControllerBase
{
    private readonly IRegionService _regions;
    private readonly IAuditLogger _audit;

    public RegionsController(
        IRegionService regions,
        IAuditLogger audit)
    {
        _regions = regions;
        _audit = audit;
    }

    [Authorize(Policy = "Perm:Regions.Read")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RegionsDto>>> List(CancellationToken ct)
    {
        var data = await _regions.ListAsync(ct);
        return Ok(data);
    }

    [Authorize(Policy = "Perm:Regions.Read")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<RegionsDto>> GetOne(int id, CancellationToken ct)
    {
        var dto = await _regions.GetByIdAsync(id, ct);
        if (dto is null) return NotFound();
        return Ok(dto);
    }

    [Authorize(Policy = "Perm:Regions.Create")]
    [HttpPost]
    public async Task<ActionResult<RegionsDto>> Create(
        [FromBody] RegionsDto dto,
        [FromServices] IValidator<RegionsDto>? validator = null,
        CancellationToken ct = default)
    {
        if (dto is null) return BadRequest("Body obrigatório.");

        if (validator is not null)
        {
            var val = await validator.ValidateAsync(dto, ct);
            if (!val.IsValid) return BadRequest(val.Errors);
        }

        var currentUserId = User.TryGetUserId();

        var created = await _regions.CreateAsync(dto, currentUserId, ct);

        await _audit.LogAsync("Regions.Create", currentUserId,
            $"Created region {created.Description} (Id={created.Id})", ct);

        return CreatedAtAction(nameof(GetOne), new { id = created.Id }, created);
    }

    [Authorize(Policy = "Perm:Regions.Update")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] RegionsDto dto,
        [FromServices] IValidator<RegionsDto>? validator = null,
        CancellationToken ct = default)
    {
        if (dto is null) return BadRequest("Body obrigatório.");

        if (validator is not null)
        {
            var val = await validator.ValidateAsync(dto, ct);
            if (!val.IsValid) return BadRequest(val.Errors);
        }

        var currentUserId = User.TryGetUserId();

        RegionsDto? updated;
        try
        {
            updated = await _regions.UpdateAsync(id, dto, currentUserId, ct);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Active must be"))
        {
            return BadRequest(ex.Message);
        }

        if (updated is null) return NotFound();

        await _audit.LogAsync("Regions.Update", currentUserId,
            $"Updated region {updated.Description} (Id={updated.Id})", ct);

        return NoContent();
    }

    [Authorize(Policy = "Perm:Regions.Delete")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var currentUserId = User.TryGetUserId();

        var ok = await _regions.SoftDeleteAsync(id, currentUserId, ct);
        if (!ok) return NotFound();

        await _audit.LogAsync("Regions.Delete", currentUserId,
            $"Deleted region Id={id}", ct);

        return NoContent();
    }
}

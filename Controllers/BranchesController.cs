// Controllers/BranchesController.cs
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Starter.Api.DTOs.Branches;
using Starter.Api.Security;
using Starter.Api.Services;
using System.Threading;

namespace Starter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BranchesController : ControllerBase
{
    private readonly IBranchService _branches;
    private readonly IAuditLogger _audit;

    public BranchesController(
        IBranchService branches,
        IAuditLogger audit)
    {
        _branches = branches;
        _audit = audit;
    }

    [Authorize(Policy = "Perm:Branches.Read")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BranchesDto>>> List(CancellationToken ct)
    {
        var data = await _branches.ListAsync(ct);
        return Ok(data);
    }

    [Authorize(Policy = "Perm:Branches.Read")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<BranchesDto>> GetOne(int id, CancellationToken ct)
    {
        var dto = await _branches.GetByIdAsync(id, ct);
        if (dto is null) return NotFound();
        return Ok(dto);
    }

    [Authorize(Policy = "Perm:Branches.Create")]
    [HttpPost]
    public async Task<ActionResult<BranchesDto>> Create(
        [FromBody] BranchesDto dto,
        [FromServices] IValidator<BranchesDto>? validator = null,
        CancellationToken ct = default)
    {
        if (dto is null) return BadRequest("Body obrigatório.");

        if (validator is not null)
        {
            var val = await validator.ValidateAsync(dto, ct);
            if (!val.IsValid) return BadRequest(val.Errors);
        }

        var currentUserId = User.TryGetUserId();

        var created = await _branches.CreateAsync(dto, currentUserId, ct);

        await _audit.LogAsync("Branches.Create", currentUserId,
            $"Created branch {created.BranchCode} - {created.Description} (Id={created.Id})", ct);

        return CreatedAtAction(nameof(GetOne), new { id = created.Id }, created);
    }

    [Authorize(Policy = "Perm:Branches.Update")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] BranchesDto dto,
        [FromServices] IValidator<BranchesDto>? validator = null,
        CancellationToken ct = default)
    {
        if (dto is null) return BadRequest("Body obrigatório.");

        if (validator is not null)
        {
            var val = await validator.ValidateAsync(dto, ct);
            if (!val.IsValid) return BadRequest(val.Errors);
        }

        var currentUserId = User.TryGetUserId();

        BranchesDto? updated;
        try
        {
            updated = await _branches.UpdateAsync(id, dto, currentUserId, ct);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Active must be"))
        {
            return BadRequest(ex.Message);
        }

        if (updated is null) return NotFound();

        await _audit.LogAsync("Branches.Update", currentUserId,
            $"Updated branch {updated.BranchCode} - {updated.Description} (Id={updated.Id})", ct);

        return NoContent();
    }

    [Authorize(Policy = "Perm:Branches.Delete")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var currentUserId = User.TryGetUserId();

        var ok = await _branches.SoftDeleteAsync(id, currentUserId, ct);
        if (!ok) return NotFound();

        await _audit.LogAsync("Branches.Delete", currentUserId,
            $"Deleted branch Id={id}", ct);

        return NoContent();
    }
}

using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Starter.Api.Data;
using Starter.Api.DTOs.Branches;
using Starter.Api.Models;
using Starter.Api.Security;
using Starter.Api.Services;      // IAuditLogger
using System.Threading;          // CancellationToken

namespace Starter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BranchesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly IDateTimeProvider _dateTimeProvider;

    public BranchesController(AppDbContext db, IAuditLogger audit, IDateTimeProvider dateTimeProvider)
    {
        _db = db;
        _audit = audit;
        _dateTimeProvider = dateTimeProvider;
    }

    [Authorize(Policy = "Perm:Branches.Read")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BranchesDto>>> List(CancellationToken ct)
    {
        // 1) carrega todos os nomes (Id -> DisplayName) de uma vez
        var names = await _db.Users
            .AsNoTracking()
            .Select(x => new { x.Id, Display = x.Name ?? x.Username })
            .ToDictionaryAsync(x => x.Id, x => x.Display, ct);

        // 2) carrega usuários
        var branch = await _db.Branches
            .AsNoTracking()
            .OrderBy(u => u.Id)
            .ToListAsync(ct);

        var result = branch.Select(Branches => new BranchesDto
        {
            Id = Branches.Id,
            Description    = Branches.Description,
            BranchCode    = Branches.BranchCode,
            RegionId = Branches.RegionId,
            CreatedAt = Branches.CreatedAt,
            CreatedByUserId = Branches.CreatedByUserId,
            UpdatedAt = Branches.UpdatedAt,
            UpdatedByUserId = Branches.UpdatedByUserId,
            Active = Branches.Active
        }).ToList();

        return Ok(result);
    }   

    [Authorize(Policy = "Perm:Branches.Read")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<BranchesDto>> GetOne(int id, CancellationToken ct)
    {
        var i = await _db.Items.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (i == null) return NotFound();
        return new BranchesDto { Id = i.Id, Description = i.Description };
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

        var entity = new Branch
        {
            Description = dto.Description,
            BranchCode    = dto.BranchCode,
            RegionId = dto.RegionId,
            CreatedAt = _dateTimeProvider.NowLocal,
            Active = "Yes",
            CreatedByUserId = User.TryGetUserId()
        };

        _db.Branches.Add(entity);
        await _db.SaveChangesAsync(ct);

        // Log
        await _audit.LogAsync("Branches.Create", $"Created item {entity.Description}", ct);

        dto.Id = entity.Id;
        return CreatedAtAction(nameof(GetOne), new { id = entity.Id }, dto);
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

        var entity = await _db.Branches.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity == null) return NotFound();

        
        entity.Description = dto.Description;
        entity.BranchCode    = dto.BranchCode;
        entity.RegionId = dto.RegionId;
        entity.UpdatedAt = _dateTimeProvider.NowLocal;
        entity.UpdatedByUserId = User.TryGetUserId(); 
        entity.Active = dto.Active?.Trim();

        await _db.SaveChangesAsync(ct);

        // Log
        await _audit.LogAsync("Branches.Update", $"Updated item {entity.Description}", ct);

        return NoContent();
    }

    [Authorize(Policy = "Perm:Branches.Delete")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.Items.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity == null) return NotFound();

        entity.Active    = "No";
        entity.UpdatedByUserId = User.TryGetUserId();
        entity.UpdatedAt = _dateTimeProvider.NowLocal;
        await _db.SaveChangesAsync(ct);

        // Log
        await _audit.LogAsync("Branches.Delete", $"Deleted item #{id}", ct);

        return NoContent();
    }
}

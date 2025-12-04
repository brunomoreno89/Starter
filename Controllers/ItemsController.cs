/*
    1.1. ItemsController (camada de API)
        Arquivo: Controllers/ItemsController.cs
        Função: falar com o “mundo externo” (HTTP).
        O que ele faz:
            Recebe a requisição HTTP (GET/POST/PUT/DELETE).
            Aplica [Authorize] e policies (Perm:Items.Read, etc.).
            Resolve validação (FluentValidation).
            Converte o usuário logado em userId (User.TryGetUserId()).
            Chama o serviço (IItemService) com os dados certos.
            Define o código de retorno HTTP:
                200 OK para GET
                201 Created para POST
                204 NoContent para PUT/DELETE quando deu certo
                404 NotFound quando o service indica que não achou o registro
        Repare:
            Ele não sabe se por baixo é EF, stored procedure, Dapper, arquivo texto, etc.
            Só sabe: “quero listar, criar, editar, excluir”.
*/

using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Starter.Api.DTOs.Items;
using Starter.Api.Security;
using Starter.Api.Services;      // IAuditLogger, IItemService
using System.Threading;

namespace Starter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ItemsController : ControllerBase
{
    private readonly IItemService _items;
    private readonly IAuditLogger _audit;

    public ItemsController(IItemService items, IAuditLogger audit)
    {
        _items = items;
        _audit = audit;
    }

    [Authorize(Policy = "Perm:Items.Read")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ItemDto>>> List(CancellationToken ct)
    {
        var list = await _items.ListAsync(ct);
        return Ok(list);
    }

    [Authorize(Policy = "Perm:Items.Read")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ItemDto>> GetOne(int id, CancellationToken ct)
    {
        var dto = await _items.GetByIdAsync(id, ct);
        if (dto is null) return NotFound();
        return Ok(dto);
    }

    [Authorize(Policy = "Perm:Items.Create")]
    [HttpPost]
    public async Task<ActionResult<ItemDto>> Create(
        [FromBody] ItemDto dto,
        [FromServices] IValidator<ItemDto>? validator = null,
        CancellationToken ct = default)
    {
        if (dto is null) return BadRequest("Body obrigatório.");

        if (validator is not null)
        {
            var val = await validator.ValidateAsync(dto, ct);
            if (!val.IsValid) return BadRequest(val.Errors);
        }

        var userId = User.TryGetUserId();
        var created = await _items.CreateAsync(dto, userId, ct);

        await _audit.LogAsync("Items.Create", $"Created item {created.Name}", ct);

        return CreatedAtAction(nameof(GetOne), new { id = created.Id }, created);
    }

    [Authorize(Policy = "Perm:Items.Update")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] ItemDto dto,
        [FromServices] IValidator<ItemDto>? validator = null,
        CancellationToken ct = default)
    {
        if (dto is null) return BadRequest("Body obrigatório.");

        if (validator is not null)
        {
            var val = await validator.ValidateAsync(dto, ct);
            if (!val.IsValid) return BadRequest(val.Errors);
        }

        var userId = User.TryGetUserId();
        var updated = await _items.UpdateAsync(id, dto, userId, ct);
        if (!updated) return NotFound();

        await _audit.LogAsync("Items.Update", $"Updated item #{id}", ct);

        return NoContent();
    }

    [Authorize(Policy = "Perm:Items.Delete")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var userId = User.TryGetUserId();
        var deleted = await _items.SoftDeleteAsync(id, userId, ct);
        if (!deleted) return NotFound();

        await _audit.LogAsync("Items.Delete", $"Deleted item #{id}", ct);

        return NoContent();
    }
}

/*
    1.2. IItemService (contrato da regra de negócio/dados)
        Arquivo: Services/IItemService.cs
        Função: definir o contrato do que um “serviço de Items” precisa oferecer.

        Isso:
            Abstrai a regra de negócio de Items.
            Desacopla o controller da implementação concreta.
            Permite trocar a implementação depois (ex.: de SP para EF puro) sem mudar o controller.
            Facilita teste unitário (você consegue mockar IItemService).
*/

using Starter.Api.DTOs.Items;

namespace Starter.Api.Services;

public interface IItemService
{
    Task<IReadOnlyList<ItemDto>> ListAsync(CancellationToken ct);
    Task<ItemDto?> GetByIdAsync(int id, CancellationToken ct);
    Task<ItemDto> CreateAsync(ItemDto dto, int? currentUserId, CancellationToken ct);
    Task<bool> UpdateAsync(int id, ItemDto dto, int? currentUserId, CancellationToken ct);
    Task<bool> SoftDeleteAsync(int id, int? currentUserId, CancellationToken ct);
}

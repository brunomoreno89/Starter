using Starter.Api.DTOs.Users;

namespace Starter.Api.Services;

public interface IUserService
{
    Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken ct);
    Task<UserDto?> GetByIdAsync(int id, CancellationToken ct);

    /// <summary>
    /// Cria usuário (já hash de senha, datas, etc.).
    /// Lança exceção se algo inesperado ocorrer.
    /// </summary>
    Task<UserDto> CreateAsync(UserCreateDto dto, int? currentUserId, CancellationToken ct);

    /// <summary>
    /// Atualiza usuário. Retorna null se não encontrar.
    /// </summary>
    Task<UserDto?> UpdateAsync(int id, UserUpdateDto dto, int? currentUserId, CancellationToken ct);

    /// <summary>
    /// Soft delete (Active = 'No'). Retorna false se não encontrar.
    /// </summary>
    Task<bool> SoftDeleteAsync(int id, int? currentUserId, CancellationToken ct);

    /// <summary>
    /// Verifica se já existe username (ignorando um Id opcional).
    /// </summary>
    Task<bool> UsernameExistsAsync(string username, int? ignoreId, CancellationToken ct);

    /// <summary>
    /// Verifica se já existe email (ignorando um Id opcional).
    /// </summary>
    Task<bool> EmailExistsAsync(string email, int? ignoreId, CancellationToken ct);
}

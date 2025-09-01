using System.Threading;
using System.Threading.Tasks;

namespace Starter.Api.Services
{
    public interface IAuditLogger
    {
        // Padrão novo
        Task LogAsync(string action, int? userId = null, string? details = null, CancellationToken ct = default);

        // Compat antigos
        Task LogAsync(int userId, string description, CancellationToken ct = default);
        Task LogAsync(string action, string details, CancellationToken ct = default);

        // Compat para controllers que chamam LogAsync1(...)
        Task LogAsync1(string action, CancellationToken ct = default);
        Task LogAsync1(string action, int userId, CancellationToken ct = default);
        Task LogAsync1(string action, int userId, string? details, CancellationToken ct = default);
        Task LogAsync1(string action, string details, CancellationToken ct = default); // <- novo
    }
}

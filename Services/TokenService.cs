using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Starter.Api.Data;
using Starter.Api.Models;

namespace Starter.Api.Services
{
    public interface ITokenService
    {
        Task RevokeAccessTokenAsync(string jti, DateTime expiresAt, CancellationToken ct);
        Task RevokeRefreshTokenAsync(int userId, string refreshToken, string? ip, CancellationToken ct);
    }

    public class TokenService : ITokenService
    {
        private readonly AppDbContext _db;
        public TokenService(AppDbContext db) => _db = db;

        public async Task RevokeAccessTokenAsync(string jti, DateTime expiresAt, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(jti)) return;
            var exists = await _db.RevokedAccessTokens.AnyAsync(x => x.Jti == jti, ct);
            if (!exists)
            {
                _db.RevokedAccessTokens.Add(new RevokedAccessToken
                {
                    Jti = jti,
                    ExpiresAt = expiresAt.ToUniversalTime()
                });
                await _db.SaveChangesAsync(ct);
            }
        }

        // Opcional (se usar refresh token)
        private static string Hash(string token)
        {
            using var sha = SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(token)));
        }

        public async Task RevokeRefreshTokenAsync(int userId, string refreshToken, string? ip, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(refreshToken)) return;
            var hash = Hash(refreshToken);
            var rt = await _db.RefreshTokens
                .FirstOrDefaultAsync(x => x.UserId == userId && x.TokenHash == hash, ct);
            if (rt == null) return;
            rt.RevokedAt = DateTime.UtcNow;
            rt.RevokedByIp = ip;
            await _db.SaveChangesAsync(ct);
        }
    }
}

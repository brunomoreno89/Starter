using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Starter.Api.Data;

namespace Starter.Api.Middleware
{
    public class JwtRevocationMiddleware
    {
        private readonly RequestDelegate _next;
        public JwtRevocationMiddleware(RequestDelegate next) => _next = next;

        public async Task Invoke(HttpContext ctx, AppDbContext db)
        {
            var jti = ctx.User?.Claims?.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
            if (!string.IsNullOrWhiteSpace(jti))
            {
                var revoked = await db.RevokedAccessTokens
                    .AnyAsync(x => x.Jti == jti && x.ExpiresAt > System.DateTime.UtcNow);

                if (revoked)
                {
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }
            }

            await _next(ctx);
        }
    }
}

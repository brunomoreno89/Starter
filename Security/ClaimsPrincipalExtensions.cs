using System.Security.Claims;

namespace Starter.Api.Security
{
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>
        /// Tenta obter o UserId das claims (NameIdentifier, "sub" ou URL legacy).
        /// Retorna null se não encontrar ou se não for inteiro.
        /// </summary>
        public static int? TryGetUserId(this ClaimsPrincipal? user)
        {
            if (user == null) return null;

            var keys = new[]
            {
                ClaimTypes.NameIdentifier,
                "sub",
                "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"
            };

            foreach (var key in keys)
            {
                var val = user.FindFirstValue(key);
                if (!string.IsNullOrWhiteSpace(val) && int.TryParse(val, out var id))
                    return id;
            }

            return null;
        }
    }
}

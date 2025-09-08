using System;

namespace Starter.Api.Models
{
    public class RevokedAccessToken
    {
        public int Id { get; set; }
        public string Jti { get; set; } = "";
        public DateTime ExpiresAt { get; set; } // validade original do access token
    }
}

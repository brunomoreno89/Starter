using System;

namespace Starter.Api.Models
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string TokenHash { get; set; } = ""; // guarde hash (SHA-256)
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedByIp { get; set; } = "";
        public DateTime? RevokedAt { get; set; }
        public string? RevokedByIp { get; set; }
        public string? ReplacedByTokenHash { get; set; }
        public bool IsActive => RevokedAt == null && DateTime.UtcNow < ExpiresAt;
    }
}

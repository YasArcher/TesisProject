namespace tesisproject.backend.Data.UnifiedEntities.Auth
{
    public class RefreshToken
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // FK to AspNetUsers (IdentityUser<int>)
        public int UserId { get; set; }

        // Store only the hash, never the raw token
        public string TokenHash { get; set; } = string.Empty;

        public DateTime ExpiresAtUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? RevokedAtUtc { get; set; }

        // Hash of the new token when rotating
        public string? ReplacedByTokenHash { get; set; }

        public string? CreatedByIp { get; set; }
        public string? RevokedByIp { get; set; }

        public bool IsActive => RevokedAtUtc == null && DateTime.UtcNow < ExpiresAtUtc;
    }
}

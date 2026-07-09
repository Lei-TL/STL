using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using STL.Infrastructure.Interfaces;

namespace STL.Entities.IdentityModule;

[Table("user_tokens")]
public class UserToken : IAuditableEntity
{
    [Key]
    [Column("id")]
    public string Id { get; set; } = string.Empty;

    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    public User? User { get; set; }

    [Column("refresh_token")]
    public string RefreshToken { get; set; } = string.Empty;

    [Column("refresh_token_expiry_time")]
    public DateTimeOffset RefreshTokenExpiryTime { get; set; }

    [Column("is_revoked")]
    public bool IsRevoked { get; set; }

    [Column("revoked_at")]
    public DateTimeOffset? RevokedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}

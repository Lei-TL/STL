using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using STL.Infrastructure.Interfaces;
using STL.Models.Auth;

namespace STL.Entities.IdentityModule;

[Table(name: "users")]
public class User : IAuditableEntity, IHaveSoftDelete
{
    [Key]
    [Column(name: "id")]
    public string Id { get; set; } = string.Empty;

    [Column(name: "email")]
    public string Email { get; set; } = string.Empty;

    [Column(name: "password")]
    public string PasswordHash { get; set; } = string.Empty;

    [Column(name: "role_level")]
    public UserRoleLevel RoleLevel { get; set; } = UserRoleLevel.User;

    [Column("full_name")]
    public string? FullName { get; set; }

    [Column("phone_number")]
    public string? PhoneNumber { get; set; }

    [Column("email_confirmed")]
    public bool EmailConfirmed { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("last_login_at")]
    public DateTime? LastLoginAt { get; set; }

    [Column("deleted")]
    public bool Deleted { get; set; } = false;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}

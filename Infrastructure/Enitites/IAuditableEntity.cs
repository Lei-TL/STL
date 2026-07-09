using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace STL.Infrastructure.Interfaces;

public abstract class IAuditableEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public Guid Id { get; init; }
    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }
    [Column("updated_at")]
    public DateTimeOffset? UpdatedAt { get; set; }
}

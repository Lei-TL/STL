using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using STL.Infrastructure.Interfaces;

namespace STL.Entities.CatalogModule;

[Table("Products")]
public class Product : IHaveSoftDelete, IAuditableEntity
{
    [Key]
    [Column("product_id")]
    public string Id { get; set; } = string.Empty;

    [Column("product_name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("category_id")]
    public string CategoryId { get; set; } = string.Empty;

    public Category? Category { get; set; }


    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("deleted")]
    public bool Deleted { get; set; } = false;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}

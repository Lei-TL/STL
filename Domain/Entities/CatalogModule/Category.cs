using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using STL.Infrastructure.Interfaces;

namespace STL.Entities.CatalogModule;

[Table("Category")]
public class Category : IHaveSoftDelete, IAuditableEntity
{
    [Key]
    [Column("category_id")]
    public string Id { get; set; } = string.Empty;

    [Column("category_name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("slug")]
    public string? Slug { get; set; }

    [Column("display_order")]
    public int DisplayOrder { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    public List<Product> Products { get; set; } = [];

    [Column("deleted")]
    public bool Deleted { get; set; } = false;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}

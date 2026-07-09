using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using STL.Entities.CatalogModule;
using STL.Entities.IdentityModule;
using STL.Infrastructure.Interfaces;

namespace STL.Entities.RecommendationModule;

[Table("product_interactions")]
public class ProductInteraction : IAuditableEntity
{
    [Key]
    [Column("interaction_id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Column("user_id")]
    public string? UserId { get; set; }

    public User? User { get; set; }

    [Column("session_id")]
    public string? SessionId { get; set; }

    [Column("product_id")]
    public string ProductId { get; set; } = string.Empty;

    public Product? Product { get; set; }

    [Column("interaction_type")]
    public ProductInteractionType InteractionType { get; set; } = ProductInteractionType.View;

    [Column("weight")]
    public decimal Weight { get; set; } = 1;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}

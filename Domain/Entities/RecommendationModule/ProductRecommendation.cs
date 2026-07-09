using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using STL.Entities.CatalogModule;
using STL.Infrastructure.Interfaces;

namespace STL.Entities.RecommendationModule;

[Table("product_recommendations")]
public class ProductRecommendation : IAuditableEntity
{
    [Key]
    [Column("product_recommendation_id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Column("product_id")]
    public string ProductId { get; set; } = string.Empty;

    public Product? Product { get; set; }

    [Column("recommended_product_id")]
    public string RecommendedProductId { get; set; } = string.Empty;

    public Product? RecommendedProduct { get; set; }

    [Column("recommendation_type")]
    public ProductRecommendationType RecommendationType { get; set; }

    [Column("score")]
    public decimal Score { get; set; }

    [Column("model_version")]
    public string ModelVersion { get; set; } = "fallback";

    [Column("reason")]
    public string? Reason { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}

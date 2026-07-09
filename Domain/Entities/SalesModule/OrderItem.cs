using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using STL.Entities.CatalogModule;
using STL.Infrastructure.Interfaces;

namespace STL.Entities.SalesModule;

[Table("order_items")]
public class OrderItem : IHaveSoftDelete, IAuditableEntity
{
    [Key]
    [Column("order_item_id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Column("order_id")]
    public string OrderId { get; set; } = string.Empty;

    public Order? Order { get; set; }

    [Column("product_id")]
    public string ProductId { get; set; } = string.Empty;

    public Product? Product { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; } = 1;

    [Column("unit_price")]
    public decimal UnitPrice { get; set; }

    [Column("line_total")]
    public decimal LineTotal { get; set; }

    [Column("deleted")]
    public bool Deleted { get; set; } = false;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}

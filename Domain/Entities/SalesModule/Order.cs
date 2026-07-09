using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using STL.Entities.IdentityModule;
using STL.Infrastructure.Interfaces;

namespace STL.Entities.SalesModule;

[Table("orders")]
public class Order : IHaveSoftDelete, IAuditableEntity
{

    [Column("order_number")]
    public string OrderNumber { get; set; } = string.Empty;

    [Column("user_id")]
    public string? UserId { get; set; }

    public User? User { get; set; }

    [Column("session_id")]
    public string? SessionId { get; set; }

    [Column("status")]
    public OrderStatus Status { get; set; } = OrderStatus.Placed;

    [Column("total_amount")]
    public decimal TotalAmount { get; set; }

    [Column("ordered_at")]
    public DateTime OrderedAt { get; set; } = DateTime.Now;

    public List<OrderItem> Items { get; set; } = [];

}

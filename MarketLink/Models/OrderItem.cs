using System.ComponentModel.DataAnnotations.Schema;

namespace MarketLink.Models;

/// <summary>
/// One line of an order. The unit price is stored so the order total stays
/// correct even if the farmer later changes the product price.
/// </summary>
public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal LineTotal { get; set; }

    /// <summary>Set once the customer has left a review for this line.</summary>
    public bool IsReviewed { get; set; }
}

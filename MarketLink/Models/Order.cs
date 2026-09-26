using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketLink.Models;

/// <summary>
/// A customer pre-order for pickup. Each order belongs to a single farmer, so
/// the pickup window and stock all come from that farmer. A cart that spans
/// multiple farmers is split into one order per farmer at checkout.
/// </summary>
public class Order
{
    public int Id { get; set; }

    [Required]
    public string CustomerId { get; set; } = string.Empty;
    public ApplicationUser? Customer { get; set; }

    public int FarmerProfileId { get; set; }
    public FarmerProfile? Farmer { get; set; }

    /// <summary>Market where the order will be collected.</summary>
    public int? MarketId { get; set; }
    public Market? Market { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Placed;

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    [Display(Name = "Pickup Date")]
    public DateTime PickupDate { get; set; }

    [StringLength(40)]
    [Display(Name = "Pickup Time Slot")]
    public string PickupTimeSlot { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalAmount { get; set; }

    /// <summary>Orders can be cancelled or modified only before this time.</summary>
    [Display(Name = "Cut-off Time")]
    public DateTime CutoffTime { get; set; }

    [StringLength(300)]
    public string? CustomerNote { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

    /// <summary>True while the customer is still allowed to cancel or edit.</summary>
    [NotMapped]
    public bool CanModify =>
        DateTime.UtcNow < CutoffTime &&
        (Status == OrderStatus.Placed || Status == OrderStatus.Accepted);
}

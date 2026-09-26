using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketLink.Models;

/// <summary>
/// A product a farmer lists for sale (part of their weekly stock).
/// </summary>
public class Product
{
    public int Id { get; set; }

    public int FarmerProfileId { get; set; }
    public FarmerProfile? Farmer { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(600)]
    public string? Description { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    [Range(0.01, 100000)]
    public decimal Price { get; set; }

    /// <summary>Unit the price refers to, e.g. "kg", "dozen", "bunch", "500g".</summary>
    [Required, StringLength(30)]
    public string Unit { get; set; } = "kg";

    [Display(Name = "Quantity Available")]
    [Range(0, 100000)]
    public int StockQuantity { get; set; }

    [StringLength(250)]
    public string? ImageUrl { get; set; }

    /// <summary>Farmers can mark an item temporarily unavailable even if stock exists.</summary>
    public bool IsAvailable { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>True when the farmer has no units left.</summary>
    [NotMapped]
    public bool IsSoldOut => StockQuantity <= 0;

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}

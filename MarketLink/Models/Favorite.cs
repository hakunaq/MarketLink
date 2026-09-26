namespace MarketLink.Models;

/// <summary>
/// A customer's saved farmer or product for quick access and restock alerts.
/// Exactly one of FarmerProfileId / ProductId is set.
/// </summary>
public class Favorite
{
    public int Id { get; set; }

    public string CustomerId { get; set; } = string.Empty;
    public ApplicationUser? Customer { get; set; }

    public int? FarmerProfileId { get; set; }
    public FarmerProfile? FarmerProfile { get; set; }

    public int? ProductId { get; set; }
    public Product? Product { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

namespace MarketLink.Models;

/// <summary>
/// A single line held in the shopper's session cart. Product details are
/// copied in so the cart can be displayed without a database hit each time.
/// </summary>
public class CartItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal UnitPrice { get; set; }
    public string Unit { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int StockQuantity { get; set; }

    public int FarmerProfileId { get; set; }
    public string FarmerName { get; set; } = string.Empty;

    public decimal LineTotal => UnitPrice * Quantity;
}

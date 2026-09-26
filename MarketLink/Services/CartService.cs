using System.Text.Json;
using MarketLink.Data;
using MarketLink.Models;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Services;

public interface ICartService
{
    List<CartItem> GetItems();
    int GetCount();
    decimal GetTotal();
    Task<bool> AddAsync(int productId, int quantity);
    void UpdateQuantity(int productId, int quantity);
    void Remove(int productId);
    void Clear();
    /// <summary>Groups the cart by farmer so checkout can create one order per farmer.</summary>
    Dictionary<int, List<CartItem>> GroupByFarmer();
}

/// <summary>
/// Stores the shopping cart in the user's session as JSON. No database table is
/// needed, which keeps the cart simple and works even before login.
/// </summary>
public class CartService : ICartService
{
    private const string CartKey = "MarketLink_Cart";

    private readonly IHttpContextAccessor _httpContext;
    private readonly MarketLinkDbContext _db;

    public CartService(IHttpContextAccessor httpContext, MarketLinkDbContext db)
    {
        _httpContext = httpContext;
        _db = db;
    }

    private ISession Session => _httpContext.HttpContext!.Session;

    public List<CartItem> GetItems()
    {
        var json = Session.GetString(CartKey);
        if (string.IsNullOrEmpty(json)) return new List<CartItem>();
        return JsonSerializer.Deserialize<List<CartItem>>(json) ?? new List<CartItem>();
    }

    private void Save(List<CartItem> items)
    {
        Session.SetString(CartKey, JsonSerializer.Serialize(items));
    }

    public int GetCount() => GetItems().Sum(i => i.Quantity);

    public decimal GetTotal() => GetItems().Sum(i => i.LineTotal);

    public async Task<bool> AddAsync(int productId, int quantity)
    {
        if (quantity <= 0) return false;

        var product = await _db.Products
            .Include(p => p.Farmer)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product is null || !product.IsAvailable || product.IsSoldOut)
            return false;

        var items = GetItems();
        var existing = items.FirstOrDefault(i => i.ProductId == productId);

        var newQty = (existing?.Quantity ?? 0) + quantity;
        if (newQty > product.StockQuantity)
            newQty = product.StockQuantity; // never let the cart exceed stock

        if (existing is null)
        {
            items.Add(new CartItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ImageUrl = product.ImageUrl,
                UnitPrice = product.Price,
                Unit = product.Unit,
                Quantity = newQty,
                StockQuantity = product.StockQuantity,
                FarmerProfileId = product.FarmerProfileId,
                FarmerName = product.Farmer?.StallName ?? "Farmer"
            });
        }
        else
        {
            existing.Quantity = newQty;
        }

        Save(items);
        return true;
    }

    public void UpdateQuantity(int productId, int quantity)
    {
        var items = GetItems();
        var item = items.FirstOrDefault(i => i.ProductId == productId);
        if (item is null) return;

        if (quantity <= 0)
        {
            items.Remove(item);
        }
        else
        {
            item.Quantity = Math.Min(quantity, item.StockQuantity);
        }
        Save(items);
    }

    public void Remove(int productId)
    {
        var items = GetItems();
        var item = items.FirstOrDefault(i => i.ProductId == productId);
        if (item != null)
        {
            items.Remove(item);
            Save(items);
        }
    }

    public void Clear() => Session.Remove(CartKey);

    public Dictionary<int, List<CartItem>> GroupByFarmer()
        => GetItems().GroupBy(i => i.FarmerProfileId).ToDictionary(g => g.Key, g => g.ToList());
}

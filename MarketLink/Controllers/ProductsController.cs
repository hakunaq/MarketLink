using MarketLink.Controllers;
using MarketLink.Data;
using MarketLink.Models;
using MarketLink.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Controllers;

/// <summary>Browsing, searching and filtering the product catalogue (read-only).</summary>
public class ProductsController : AppControllerBase
{
    public ProductsController(MarketLinkDbContext db) : base(db) { }

    // GET: /Products
    public async Task<IActionResult> Index(ProductFilterViewModel filter)
    {
        var query = Db.Products
            .Include(p => p.Farmer)
            .Include(p => p.Category)
            .Where(p => p.IsAvailable && p.Farmer!.IsApproved && !p.Farmer.IsSuspended);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            query = query.Where(p => p.Name.Contains(filter.SearchTerm) ||
                                     (p.Description != null && p.Description.Contains(filter.SearchTerm)));

        if (filter.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == filter.CategoryId.Value);

        if (filter.MinPrice.HasValue)
            query = query.Where(p => p.Price >= filter.MinPrice.Value);

        if (filter.MaxPrice.HasValue)
            query = query.Where(p => p.Price <= filter.MaxPrice.Value);

        // Market / day filters go through the farmer's market schedule.
        if (filter.MarketId.HasValue)
            query = query.Where(p => p.Farmer!.FarmerMarkets.Any(fm => fm.MarketId == filter.MarketId.Value));

        if (!string.IsNullOrWhiteSpace(filter.Day))
            query = query.Where(p => p.Farmer!.FarmerMarkets.Any(fm => fm.OperatingDay == filter.Day));

        query = filter.SortBy switch
        {
            "price_asc" => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "name" => query.OrderBy(p => p.Name),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        filter.Products = await query.ToListAsync();

        var categories = await Db.Categories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
        var markets = await Db.Markets.Where(m => m.IsActive).OrderBy(m => m.Name).ToListAsync();
        filter.Categories = new SelectList(categories, "Id", "Name", filter.CategoryId);
        filter.Markets = new SelectList(markets, "Id", "Name", filter.MarketId);

        return View(filter);
    }

    // GET: /Products/Details/7
    public async Task<IActionResult> Details(int id)
    {
        var product = await Db.Products
            .Include(p => p.Farmer).ThenInclude(f => f!.FarmerMarkets).ThenInclude(fm => fm.Market)
            .Include(p => p.Category)
            .Include(p => p.Reviews).ThenInclude(r => r.Customer)
            .FirstOrDefaultAsync(p => p.Id == id && p.Farmer!.IsApproved && !p.Farmer.IsSuspended);

        if (product == null) return NotFound();

        var visibleReviews = product.Reviews.Where(r => !r.IsHidden).OrderByDescending(r => r.ReviewDate).ToList();
        ViewBag.Reviews = visibleReviews;
        ViewBag.AverageRating = visibleReviews.Count == 0 ? 0 : Math.Round(visibleReviews.Average(r => r.Rating), 1);

        ViewBag.IsFavorite = false;
        if (CurrentUserId != null)
            ViewBag.IsFavorite = await Db.Favorites.AnyAsync(fav =>
                fav.CustomerId == CurrentUserId && fav.ProductId == id);

        // Suggest a few similar products from the same category.
        ViewBag.Related = await Db.Products
            .Include(p => p.Farmer)
            .Where(p => p.CategoryId == product.CategoryId && p.Id != product.Id && p.IsAvailable)
            .Take(4)
            .ToListAsync();

        return View(product);
    }

    // GET: /Products/ByCategory/2
    public async Task<IActionResult> ByCategory(int id)
    {
        return await Index(new ProductFilterViewModel { CategoryId = id });
    }
}

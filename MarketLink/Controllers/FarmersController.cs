using MarketLink.Controllers;
using MarketLink.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Controllers;

/// <summary>Public browsing of approved farmers and their profiles.</summary>
public class FarmersController : AppControllerBase
{
    public FarmersController(MarketLinkDbContext db) : base(db) { }

    // GET: /Farmers
    public async Task<IActionResult> Index(string? search, int? marketId)
    {
        var query = Db.FarmerProfiles
            .Include(f => f.Products)
            .Where(f => f.IsApproved && !f.IsSuspended);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(f => f.StallName.Contains(search) ||
                                     f.ContactPerson.Contains(search) ||
                                     (f.Bio != null && f.Bio.Contains(search)));

        if (marketId.HasValue)
            query = query.Where(f => f.FarmerMarkets.Any(fm => fm.MarketId == marketId.Value));

        var farmers = await query.OrderBy(f => f.StallName).ToListAsync();

        ViewBag.Search = search;
        ViewBag.MarketId = marketId;
        ViewBag.Markets = await Db.Markets.Where(m => m.IsActive).OrderBy(m => m.Name).ToListAsync();
        return View(farmers);
    }

    // GET: /Farmers/Details/3
    public async Task<IActionResult> Details(int id)
    {
        var farmer = await Db.FarmerProfiles
            .Include(f => f.Products).ThenInclude(p => p.Category)
            .Include(f => f.FarmerMarkets).ThenInclude(fm => fm.Market)
            .Include(f => f.Reviews).ThenInclude(r => r.Customer)
            .FirstOrDefaultAsync(f => f.Id == id && f.IsApproved && !f.IsSuspended);

        if (farmer == null) return NotFound();

        var visibleReviews = farmer.Reviews.Where(r => !r.IsHidden).OrderByDescending(r => r.ReviewDate).ToList();
        ViewBag.Reviews = visibleReviews;
        ViewBag.AverageRating = visibleReviews.Count == 0 ? 0 : Math.Round(visibleReviews.Average(r => r.Rating), 1);
        ViewBag.Products = farmer.Products.Where(p => p.IsAvailable).OrderBy(p => p.Name).ToList();

        // Is this farmer favorited by the current customer?
        ViewBag.IsFavorite = false;
        if (CurrentUserId != null)
            ViewBag.IsFavorite = await Db.Favorites.AnyAsync(fav =>
                fav.CustomerId == CurrentUserId && fav.FarmerProfileId == id);

        return View(farmer);
    }
}

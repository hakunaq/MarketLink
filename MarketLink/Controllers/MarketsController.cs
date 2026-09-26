using MarketLink.Controllers;
using MarketLink.Data;
using MarketLink.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Controllers;

/// <summary>Browsing farmers markets, their schedules and their map locations.</summary>
public class MarketsController : AppControllerBase
{
    public MarketsController(MarketLinkDbContext db) : base(db) { }

    // GET: /Markets
    public async Task<IActionResult> Index(string? day)
    {
        var markets = await Db.Markets
            .Where(m => m.IsActive)
            .OrderBy(m => m.Name)
            .ToListAsync();

        if (!string.IsNullOrWhiteSpace(day))
            markets = markets.Where(m => m.OperatingDays.Contains(day, StringComparison.OrdinalIgnoreCase)).ToList();

        ViewBag.Day = day;
        ViewBag.Days = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
        return View(markets);
    }

    // GET: /Markets/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var market = await Db.Markets
            .Include(m => m.FarmerMarkets)
                .ThenInclude(fm => fm.FarmerProfile)
            .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);

        if (market == null) return NotFound();

        var farmers = market.FarmerMarkets
            .Where(fm => fm.FarmerProfile!.IsApproved && !fm.FarmerProfile.IsSuspended)
            .Select(fm => fm.FarmerProfile!)
            .DistinctBy(f => f.Id)
            .ToList();

        ViewBag.Farmers = farmers;
        return View(market);
    }

    // GET: /Markets/Map  — all markets and farmer stalls on one map
    public async Task<IActionResult> Map()
    {
        var markets = await Db.Markets.Where(m => m.IsActive).ToListAsync();
        var farmers = await Db.FarmerProfiles
            .Where(f => f.IsApproved && !f.IsSuspended && f.Latitude != 0 && f.Longitude != 0)
            .ToListAsync();

        ViewBag.Farmers = farmers;
        return View(markets);
    }
}

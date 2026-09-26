using MarketLink.Data;
using MarketLink.Models;
using MarketLink.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Controllers;

public class HomeController : Controller
{
    private readonly MarketLinkDbContext _db;

    public HomeController(MarketLinkDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var model = new HomeViewModel
        {
            FeaturedProducts = await _db.Products
                .Include(p => p.Farmer)
                .Include(p => p.Category)
                .Where(p => p.IsAvailable && p.StockQuantity > 0 && p.Farmer!.IsApproved && !p.Farmer.IsSuspended)
                .OrderByDescending(p => p.CreatedAt)
                .Take(8)
                .ToListAsync(),
            Markets = await _db.Markets.Where(m => m.IsActive).OrderBy(m => m.Name).Take(6).ToListAsync(),
            Categories = await _db.Categories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync(),
            Announcements = await _db.Announcements
                .Where(a => a.IsActive)
                .OrderByDescending(a => a.CreatedAt)
                .Take(3)
                .ToListAsync(),
            FarmerCount = await _db.FarmerProfiles.CountAsync(f => f.IsApproved && !f.IsSuspended),
            ProductCount = await _db.Products.CountAsync(p => p.IsAvailable && p.StockQuantity > 0)
        };
        return View(model);
    }

    public IActionResult About() => View();

    public IActionResult Contact() => View();

    public IActionResult Privacy() => View();

    public IActionResult NotFoundPage()
    {
        Response.StatusCode = 404;
        return View("NotFound");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}

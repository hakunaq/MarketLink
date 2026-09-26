using MarketLink.Controllers;
using MarketLink.Data;
using MarketLink.Models;
using MarketLink.Models.ViewModels;
using MarketLink.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Controllers;

/// <summary>Platform administration: metrics, users, markets, moderation and settings.</summary>
[Authorize(Roles = Roles.Admin)]
public class AdminController : AppControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly INotificationService _notifications;

    public AdminController(MarketLinkDbContext db, UserManager<ApplicationUser> userManager, INotificationService notifications)
        : base(db)
    {
        _userManager = userManager;
        _notifications = notifications;
    }

    // GET: /Admin
    public async Task<IActionResult> Index()
    {
        var customerCount = (await _userManager.GetUsersInRoleAsync(Roles.Customer)).Count;

        var model = new AdminDashboardViewModel
        {
            TotalFarmers = await Db.FarmerProfiles.CountAsync(f => f.IsApproved && !f.IsSuspended),
            PendingFarmers = await Db.FarmerProfiles.CountAsync(f => !f.IsApproved),
            TotalCustomers = customerCount,
            TotalMarkets = await Db.Markets.CountAsync(m => m.IsActive),
            TotalProducts = await Db.Products.CountAsync(),
            TotalOrders = await Db.Orders.CountAsync(),
            TotalRevenue = await Db.Orders.Where(o => o.Status == OrderStatus.Completed).SumAsync(o => (decimal?)o.TotalAmount) ?? 0,
            RecentOrders = await Db.Orders.Include(o => o.Customer).Include(o => o.Farmer)
                .OrderByDescending(o => o.OrderDate).Take(8).ToListAsync()
        };

        model.FarmersAwaitingApproval = await Db.FarmerProfiles
            .Include(f => f.User)
            .Where(f => !f.IsApproved)
            .OrderBy(f => f.CreatedAt)
            .Select(f => new AdminFarmerRow
            {
                FarmerProfileId = f.Id,
                StallName = f.StallName,
                ContactPerson = f.ContactPerson,
                Email = f.User!.Email!,
                CreatedAt = f.CreatedAt
            })
            .ToListAsync();

        model.MostActiveFarmers = await Db.Orders
            .Where(o => o.Status == OrderStatus.Completed)
            .GroupBy(o => o.FarmerProfileId)
            .Select(g => new TopFarmerRow
            {
                StallName = g.First().Farmer!.StallName,
                OrderCount = g.Count(),
                Revenue = g.Sum(o => o.TotalAmount)
            })
            .OrderByDescending(x => x.OrderCount)
            .Take(5)
            .ToListAsync();

        return View(model);
    }

    // ------------------------------------------------------- Manage farmers
    public async Task<IActionResult> Farmers()
    {
        var farmers = await Db.FarmerProfiles
            .Include(f => f.User)
            .OrderByDescending(f => !f.IsApproved)
            .ThenBy(f => f.StallName)
            .ToListAsync();
        return View(farmers);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveFarmer(int id)
    {
        var farmer = await Db.FarmerProfiles.FirstOrDefaultAsync(f => f.Id == id);
        if (farmer != null)
        {
            farmer.IsApproved = true;
            farmer.IsSuspended = false;
            await Db.SaveChangesAsync();
            await _notifications.NotifyAsync(farmer.UserId, "Stall approved",
                "Great news — your MarketLink stall has been approved. You can now list products.", "/Farmer");
            TempData["Success"] = "Farmer approved.";
        }
        return RedirectToAction(nameof(Farmers));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SuspendFarmer(int id)
    {
        var farmer = await Db.FarmerProfiles.FirstOrDefaultAsync(f => f.Id == id);
        if (farmer != null)
        {
            farmer.IsSuspended = true;
            await Db.SaveChangesAsync();
            await _notifications.NotifyAsync(farmer.UserId, "Stall suspended",
                "Your stall has been temporarily suspended. Please contact support.", "/Farmer/Suspended");
            TempData["Success"] = "Farmer suspended.";
        }
        return RedirectToAction(nameof(Farmers));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReinstateFarmer(int id)
    {
        var farmer = await Db.FarmerProfiles.FirstOrDefaultAsync(f => f.Id == id);
        if (farmer != null)
        {
            farmer.IsSuspended = false;
            await Db.SaveChangesAsync();
            TempData["Success"] = "Farmer reinstated.";
        }
        return RedirectToAction(nameof(Farmers));
    }

    // ----------------------------------------------------- Manage customers
    public async Task<IActionResult> Customers()
    {
        var customers = await _userManager.GetUsersInRoleAsync(Roles.Customer);
        var list = customers.OrderByDescending(u => u.CreatedAt).ToList();
        return View(list);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleCustomer(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user != null)
        {
            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);
            TempData["Success"] = user.IsActive ? "Customer activated." : "Customer deactivated.";
        }
        return RedirectToAction(nameof(Customers));
    }

    // -------------------------------------------------------- Manage markets
    public async Task<IActionResult> Markets()
    {
        var markets = await Db.Markets.OrderBy(m => m.Name).ToListAsync();
        return View(markets);
    }

    public IActionResult CreateMarket() => View("MarketForm", new Market());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateMarket(Market market)
    {
        if (!ModelState.IsValid) return View("MarketForm", market);
        market.CreatedAt = DateTime.UtcNow;
        Db.Markets.Add(market);
        await Db.SaveChangesAsync();
        TempData["Success"] = "Market added.";
        return RedirectToAction(nameof(Markets));
    }

    public async Task<IActionResult> EditMarket(int id)
    {
        var market = await Db.Markets.FirstOrDefaultAsync(m => m.Id == id);
        if (market == null) return NotFound();
        return View("MarketForm", market);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditMarket(int id, Market market)
    {
        var existing = await Db.Markets.FirstOrDefaultAsync(m => m.Id == id);
        if (existing == null) return NotFound();
        if (!ModelState.IsValid) return View("MarketForm", market);

        existing.Name = market.Name;
        existing.Address = market.Address;
        existing.Latitude = market.Latitude;
        existing.Longitude = market.Longitude;
        existing.MapProvider = market.MapProvider;
        existing.OperatingDays = market.OperatingDays;
        existing.OpenTime = market.OpenTime;
        existing.CloseTime = market.CloseTime;
        existing.IsActive = market.IsActive;
        await Db.SaveChangesAsync();

        TempData["Success"] = "Market updated.";
        return RedirectToAction(nameof(Markets));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMarket(int id)
    {
        var market = await Db.Markets.FirstOrDefaultAsync(m => m.Id == id);
        if (market != null)
        {
            // Soft-delete so existing orders that reference the market stay valid.
            market.IsActive = false;
            await Db.SaveChangesAsync();
            TempData["Success"] = "Market removed.";
        }
        return RedirectToAction(nameof(Markets));
    }

    // ------------------------------------------------------ Content moderation
    public async Task<IActionResult> Moderation()
    {
        ViewBag.Products = await Db.Products
            .Include(p => p.Farmer).Include(p => p.Category)
            .OrderByDescending(p => p.CreatedAt).Take(100).ToListAsync();
        var reviews = await Db.Reviews
            .Include(r => r.Customer).Include(r => r.Product).Include(r => r.Farmer)
            .OrderByDescending(r => r.ReviewDate).Take(100).ToListAsync();
        return View(reviews);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleReviewHidden(int id)
    {
        var review = await Db.Reviews.FirstOrDefaultAsync(r => r.Id == id);
        if (review != null)
        {
            review.IsHidden = !review.IsHidden;
            await Db.SaveChangesAsync();
            TempData["Success"] = review.IsHidden ? "Review hidden." : "Review restored.";
        }
        return RedirectToAction(nameof(Moderation));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveProduct(int id)
    {
        var product = await Db.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product != null)
        {
            product.IsAvailable = false;
            product.StockQuantity = 0;
            await Db.SaveChangesAsync();
            TempData["Success"] = "Listing removed from the catalogue.";
        }
        return RedirectToAction(nameof(Moderation));
    }

    // -------------------------------------------------------------- Categories
    public async Task<IActionResult> Categories()
    {
        var categories = await Db.Categories.Include(c => c.Products).OrderBy(c => c.Name).ToListAsync();
        return View(categories);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCategory(string name, string? description, string? iconClass)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Category name is required.";
            return RedirectToAction(nameof(Categories));
        }
        Db.Categories.Add(new Category { Name = name.Trim(), Description = description, IconClass = iconClass });
        await Db.SaveChangesAsync();
        TempData["Success"] = "Category added.";
        return RedirectToAction(nameof(Categories));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await Db.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == id);
        if (category != null)
        {
            if (category.Products.Any())
                TempData["Error"] = "Cannot delete a category that still has products.";
            else
            {
                Db.Categories.Remove(category);
                await Db.SaveChangesAsync();
                TempData["Success"] = "Category deleted.";
            }
        }
        return RedirectToAction(nameof(Categories));
    }

    // ------------------------------------------------------------ Announcements
    public async Task<IActionResult> Announcements()
    {
        var list = await Db.Announcements.OrderByDescending(a => a.CreatedAt).ToListAsync();
        return View(list);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAnnouncement(string title, string message)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(message))
        {
            TempData["Error"] = "Title and message are required.";
            return RedirectToAction(nameof(Announcements));
        }
        Db.Announcements.Add(new Announcement { Title = title, Message = message, CreatedAt = DateTime.UtcNow });
        await Db.SaveChangesAsync();
        TempData["Success"] = "Announcement published.";
        return RedirectToAction(nameof(Announcements));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleAnnouncement(int id)
    {
        var a = await Db.Announcements.FirstOrDefaultAsync(x => x.Id == id);
        if (a != null)
        {
            a.IsActive = !a.IsActive;
            await Db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Announcements));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAnnouncement(int id)
    {
        var a = await Db.Announcements.FirstOrDefaultAsync(x => x.Id == id);
        if (a != null)
        {
            Db.Announcements.Remove(a);
            await Db.SaveChangesAsync();
            TempData["Success"] = "Announcement deleted.";
        }
        return RedirectToAction(nameof(Announcements));
    }

    // ------------------------------------------------------------------ Reports
    public async Task<IActionResult> Reports(string? type)
    {
        ViewBag.Type = type;

        if (type == "orders")
            ViewBag.Orders = await Db.Orders.Include(o => o.Customer).Include(o => o.Farmer)
                .OrderByDescending(o => o.OrderDate).ToListAsync();

        if (type == "revenue")
            ViewBag.RevenueByMarket = await Db.Orders
                .Where(o => o.Status == OrderStatus.Completed && o.MarketId != null)
                .GroupBy(o => o.Market!.Name)
                .Select(g => new MarketRevenueRow { Market = g.Key, Orders = g.Count(), Revenue = g.Sum(o => o.TotalAmount) })
                .OrderByDescending(x => x.Revenue)
                .ToListAsync();

        if (type == "farmers")
            ViewBag.TopFarmers = await Db.Orders
                .Where(o => o.Status == OrderStatus.Completed)
                .GroupBy(o => o.FarmerProfileId)
                .Select(g => new TopFarmerRow
                {
                    StallName = g.First().Farmer!.StallName,
                    OrderCount = g.Count(),
                    Revenue = g.Sum(o => o.TotalAmount)
                })
                .OrderByDescending(x => x.OrderCount).ToListAsync();

        ViewBag.History = await Db.PlatformReports.OrderByDescending(r => r.GeneratedAt).Take(20).ToListAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateReport(string type)
    {
        Db.PlatformReports.Add(new PlatformReport
        {
            GeneratedBy = CurrentUserId,
            ReportType = type,
            GeneratedAt = DateTime.UtcNow
        });
        await Db.SaveChangesAsync();
        TempData["Success"] = "Report generated.";
        return RedirectToAction(nameof(Reports), new { type });
    }
}

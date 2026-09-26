using MarketLink.Controllers;
using MarketLink.Data;
using MarketLink.Models;
using MarketLink.Models.ViewModels;
using MarketLink.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Controllers;

/// <summary>Everything a farmer does: dashboard, profile, market schedule and reviews.</summary>
[Authorize(Roles = Roles.Farmer)]
public class FarmerController : AppControllerBase
{
    private readonly INotificationService _notifications;

    public FarmerController(MarketLinkDbContext db, INotificationService notifications) : base(db)
    {
        _notifications = notifications;
    }

    /// <summary>Loads the current farmer's profile or redirects if missing/suspended.</summary>
    private async Task<FarmerProfile?> RequireProfileAsync()
    {
        var profile = await GetCurrentUserFarmerProfileAsync();
        if (profile == null)
        {
            TempData["Error"] = "No farmer profile found for your account.";
            return null;
        }
        if (profile.IsSuspended)
        {
            Response.Redirect(Url.Action("Suspended")!);
            return null;
        }
        return profile;
    }

    // GET: /Farmer
    public async Task<IActionResult> Index()
    {
        var profile = await GetCurrentUserFarmerProfileAsync();
        if (profile == null) return RedirectToAction("RegisterFarmer", "Account");
        if (profile.IsSuspended) return RedirectToAction(nameof(Suspended));

        var orders = await Db.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .Where(o => o.FarmerProfileId == profile.Id)
            .ToListAsync();

        var completed = orders.Where(o => o.Status == OrderStatus.Completed).ToList();

        var model = new FarmerDashboardViewModel
        {
            Profile = profile,
            TotalOrders = orders.Count,
            PendingOrders = orders.Count(o => o.Status == OrderStatus.Placed),
            CompletedOrders = completed.Count,
            TotalRevenue = completed.Sum(o => o.TotalAmount),
            ActiveProducts = await Db.Products.CountAsync(p => p.FarmerProfileId == profile.Id && p.IsAvailable && p.StockQuantity > 0),
            SoldOutProducts = await Db.Products.CountAsync(p => p.FarmerProfileId == profile.Id && p.StockQuantity <= 0),
            RecentOrders = orders.OrderByDescending(o => o.OrderDate).Take(6).ToList()
        };

        // Best-selling products (from completed orders).
        model.BestSellers = completed
            .SelectMany(o => o.Items)
            .GroupBy(i => i.ProductId)
            .Select(g => new BestSellerRow
            {
                ProductName = g.First().Product?.Name ?? "Product",
                QuantitySold = g.Sum(i => i.Quantity),
                Revenue = g.Sum(i => i.LineTotal)
            })
            .OrderByDescending(b => b.QuantitySold)
            .Take(5)
            .ToList();

        var reviews = await Db.Reviews
            .Include(r => r.Customer)
            .Include(r => r.Product)
            .Where(r => (r.FarmerProfileId == profile.Id ||
                        (r.Product != null && r.Product.FarmerProfileId == profile.Id)) && !r.IsHidden)
            .OrderByDescending(r => r.ReviewDate)
            .ToListAsync();

        model.RecentReviews = reviews.Take(5).ToList();
        model.AverageRating = reviews.Count == 0 ? 0 : Math.Round(reviews.Average(r => r.Rating), 1);

        return View(model);
    }

    public IActionResult Suspended() => View();

    // GET: /Farmer/Profile
    public async Task<IActionResult> Profile()
    {
        var profile = await RequireProfileAsync();
        if (profile == null) return RedirectToAction(nameof(Index));
        return View(profile);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(FarmerProfile form)
    {
        var profile = await RequireProfileAsync();
        if (profile == null) return RedirectToAction(nameof(Index));

        if (!ModelState.IsValid) return View(profile);

        profile.StallName = form.StallName;
        profile.ContactPerson = form.ContactPerson;
        profile.ContactNumber = form.ContactNumber;
        profile.Bio = form.Bio;
        profile.AddressText = form.AddressText;
        profile.Latitude = form.Latitude;
        profile.Longitude = form.Longitude;
        profile.ProfileImageUrl = form.ProfileImageUrl;

        await Db.SaveChangesAsync();
        TempData["Success"] = "Profile updated.";
        return RedirectToAction(nameof(Profile));
    }

    // GET: /Farmer/Markets — manage the markets/days this farmer sells at
    public async Task<IActionResult> Markets()
    {
        var profile = await RequireProfileAsync();
        if (profile == null) return RedirectToAction(nameof(Index));

        var schedule = await Db.FarmerMarkets
            .Include(fm => fm.Market)
            .Where(fm => fm.FarmerProfileId == profile.Id)
            .OrderBy(fm => fm.OperatingDay)
            .ToListAsync();

        ViewBag.AllMarkets = await Db.Markets.Where(m => m.IsActive).OrderBy(m => m.Name).ToListAsync();
        ViewBag.Days = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
        return View(schedule);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMarket(int marketId, string operatingDay, string pickupStart, string pickupEnd, string? stallNumber)
    {
        var profile = await RequireProfileAsync();
        if (profile == null) return RedirectToAction(nameof(Index));

        if (marketId <= 0 || string.IsNullOrWhiteSpace(operatingDay))
        {
            TempData["Error"] = "Choose a market and a day.";
            return RedirectToAction(nameof(Markets));
        }

        Db.FarmerMarkets.Add(new FarmerMarket
        {
            FarmerProfileId = profile.Id,
            MarketId = marketId,
            OperatingDay = operatingDay,
            PickupStartTime = string.IsNullOrWhiteSpace(pickupStart) ? "08:00" : pickupStart,
            PickupEndTime = string.IsNullOrWhiteSpace(pickupEnd) ? "13:00" : pickupEnd,
            StallNumber = stallNumber
        });
        await Db.SaveChangesAsync();
        TempData["Success"] = "Market schedule added.";
        return RedirectToAction(nameof(Markets));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveMarket(int id)
    {
        var profile = await RequireProfileAsync();
        if (profile == null) return RedirectToAction(nameof(Index));

        var fm = await Db.FarmerMarkets.FirstOrDefaultAsync(x => x.Id == id && x.FarmerProfileId == profile.Id);
        if (fm != null)
        {
            Db.FarmerMarkets.Remove(fm);
            await Db.SaveChangesAsync();
            TempData["Success"] = "Removed.";
        }
        return RedirectToAction(nameof(Markets));
    }

    // GET: /Farmer/Reviews — reviews on this farmer and their products
    public async Task<IActionResult> Reviews()
    {
        var profile = await RequireProfileAsync();
        if (profile == null) return RedirectToAction(nameof(Index));

        var reviews = await Db.Reviews
            .Include(r => r.Customer)
            .Include(r => r.Product)
            .Where(r => (r.FarmerProfileId == profile.Id ||
                        (r.Product != null && r.Product.FarmerProfileId == profile.Id)) && !r.IsHidden)
            .OrderByDescending(r => r.ReviewDate)
            .ToListAsync();

        return View(reviews);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReplyReview(int id, string farmerReply)
    {
        var profile = await RequireProfileAsync();
        if (profile == null) return RedirectToAction(nameof(Index));

        var review = await Db.Reviews
            .Include(r => r.Product)
            .FirstOrDefaultAsync(r => r.Id == id &&
                (r.FarmerProfileId == profile.Id || (r.Product != null && r.Product.FarmerProfileId == profile.Id)));

        if (review != null)
        {
            review.FarmerReply = farmerReply;
            review.FarmerReplyDate = DateTime.UtcNow;
            await Db.SaveChangesAsync();
            TempData["Success"] = "Reply posted.";
        }
        return RedirectToAction(nameof(Reviews));
    }
}

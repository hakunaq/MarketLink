using MarketLink.Controllers;
using MarketLink.Data;
using MarketLink.Models;
using MarketLink.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Controllers;

/// <summary>The customer's personal area: dashboard, favorites and profile.</summary>
[Authorize(Roles = Roles.Customer)]
public class CustomerController : AppControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public CustomerController(MarketLinkDbContext db, UserManager<ApplicationUser> userManager)
        : base(db)
    {
        _userManager = userManager;
    }

    // GET: /Customer
    public async Task<IActionResult> Index()
    {
        var userId = CurrentUserId!;

        var model = new CustomerDashboardViewModel
        {
            RecentOrders = await Db.Orders
                .Include(o => o.Farmer)
                .Include(o => o.Items)
                .Where(o => o.CustomerId == userId)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToListAsync(),

            FavoriteFarmers = await Db.Favorites
                .Include(f => f.FarmerProfile)
                .Where(f => f.CustomerId == userId && f.FarmerProfileId != null)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync(),

            FavoriteProducts = await Db.Favorites
                .Include(f => f.Product).ThenInclude(p => p!.Farmer)
                .Where(f => f.CustomerId == userId && f.ProductId != null)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync(),

            RecommendedProducts = await Db.Products
                .Include(p => p.Farmer)
                .Where(p => p.IsAvailable && p.StockQuantity > 0 && p.Farmer!.IsApproved)
                .OrderByDescending(p => p.CreatedAt)
                .Take(4)
                .ToListAsync()
        };

        model.TotalOrders = await Db.Orders.CountAsync(o => o.CustomerId == userId);
        model.ActiveOrders = await Db.Orders.CountAsync(o => o.CustomerId == userId &&
            o.Status != OrderStatus.Completed && o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Declined);
        model.FavoriteCount = model.FavoriteFarmers.Count + model.FavoriteProducts.Count;

        ViewBag.Announcements = await Db.Announcements.Where(a => a.IsActive)
            .OrderByDescending(a => a.CreatedAt).Take(2).ToListAsync();

        return View(model);
    }

    // GET: /Customer/Favorites
    public async Task<IActionResult> Favorites()
    {
        var userId = CurrentUserId!;
        ViewBag.Farmers = await Db.Favorites
            .Include(f => f.FarmerProfile)
            .Where(f => f.CustomerId == userId && f.FarmerProfileId != null)
            .ToListAsync();
        var products = await Db.Favorites
            .Include(f => f.Product).ThenInclude(p => p!.Farmer)
            .Where(f => f.CustomerId == userId && f.ProductId != null)
            .ToListAsync();
        return View(products);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleFavoriteFarmer(int farmerId, string? returnUrl)
    {
        var userId = CurrentUserId!;
        var existing = await Db.Favorites.FirstOrDefaultAsync(f =>
            f.CustomerId == userId && f.FarmerProfileId == farmerId);

        if (existing != null) Db.Favorites.Remove(existing);
        else Db.Favorites.Add(new Favorite { CustomerId = userId, FarmerProfileId = farmerId });

        await Db.SaveChangesAsync();
        return RedirectSafely(returnUrl, "Farmers", "Details", farmerId);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleFavoriteProduct(int productId, string? returnUrl)
    {
        var userId = CurrentUserId!;
        var existing = await Db.Favorites.FirstOrDefaultAsync(f =>
            f.CustomerId == userId && f.ProductId == productId);

        if (existing != null) Db.Favorites.Remove(existing);
        else Db.Favorites.Add(new Favorite { CustomerId = userId, ProductId = productId });

        await Db.SaveChangesAsync();
        return RedirectSafely(returnUrl, "Products", "Details", productId);
    }

    // GET: /Customer/Profile
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.FindByIdAsync(CurrentUserId!);
        if (user == null) return NotFound();
        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(string fullName, string phoneNumber, string address)
    {
        var user = await _userManager.FindByIdAsync(CurrentUserId!);
        if (user == null) return NotFound();

        user.FullName = fullName;
        user.PhoneNumber = phoneNumber;
        user.Address = address;
        var result = await _userManager.UpdateAsync(user);

        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded ? "Profile updated." : "Could not update profile.";
        return RedirectToAction(nameof(Profile));
    }

    private IActionResult RedirectSafely(string? returnUrl, string controller, string action, int id)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction(action, controller, new { id });
    }
}

using MarketLink.Controllers;
using MarketLink.Data;
using MarketLink.Models;
using MarketLink.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Controllers;

/// <summary>Farmers accept, decline and fulfil incoming pre-orders.</summary>
[Authorize(Roles = Roles.Farmer)]
public class FarmerOrdersController : AppControllerBase
{
    private readonly INotificationService _notifications;

    public FarmerOrdersController(MarketLinkDbContext db, INotificationService notifications) : base(db)
    {
        _notifications = notifications;
    }

    private async Task<FarmerProfile?> RequireProfileAsync()
    {
        var profile = await GetCurrentUserFarmerProfileAsync();
        return profile is { IsSuspended: false } ? profile : null;
    }

    // GET: /FarmerOrders?status=Placed
    public async Task<IActionResult> Index(string? status)
    {
        var profile = await RequireProfileAsync();
        if (profile == null) return RedirectToAction("Index", "Farmer");

        var query = Db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Include(o => o.Market)
            .Where(o => o.FarmerProfileId == profile.Id);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderStatus>(status, out var parsed))
            query = query.Where(o => o.Status == parsed);

        var orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();

        ViewBag.Status = status;
        ViewBag.Statuses = Enum.GetValues<OrderStatus>();
        return View(orders);
    }

    // GET: /FarmerOrders/Details/4
    public async Task<IActionResult> Details(int id)
    {
        var profile = await RequireProfileAsync();
        if (profile == null) return RedirectToAction("Index", "Farmer");

        var order = await Db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Market)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id && o.FarmerProfileId == profile.Id);

        if (order == null) return NotFound();
        return View(order);
    }

    private async Task<IActionResult> ChangeStatusAsync(int id, OrderStatus newStatus, string customerMessage)
    {
        var profile = await RequireProfileAsync();
        if (profile == null) return RedirectToAction("Index", "Farmer");

        var order = await Db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id && o.FarmerProfileId == profile.Id);

        if (order == null) return NotFound();

        // If declining, give the reserved stock back.
        if (newStatus == OrderStatus.Declined && order.Status == OrderStatus.Placed)
            foreach (var item in order.Items)
                if (item.Product != null) item.Product.StockQuantity += item.Quantity;

        order.Status = newStatus;
        await Db.SaveChangesAsync();

        await _notifications.NotifyAsync(order.CustomerId, $"Order #{order.Id} {StatusWord(newStatus)}",
            customerMessage, "/Orders");

        TempData["Success"] = $"Order marked {StatusWord(newStatus).ToLowerInvariant()}.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Accept(int id)
        => ChangeStatusAsync(id, OrderStatus.Accepted,
            "Your pre-order has been accepted by the farmer.");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Decline(int id)
        => ChangeStatusAsync(id, OrderStatus.Declined,
            "The farmer could not accept your pre-order. Reserved stock has been released.");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Ready(int id)
        => ChangeStatusAsync(id, OrderStatus.ReadyForPickup,
            "Your order is ready for pickup at the market!");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Complete(int id)
        => ChangeStatusAsync(id, OrderStatus.Completed,
            "Your order is complete. Thanks for shopping local — please leave a review!");

    private static string StatusWord(OrderStatus s) => s switch
    {
        OrderStatus.Accepted => "Accepted",
        OrderStatus.Declined => "Declined",
        OrderStatus.ReadyForPickup => "Ready for pickup",
        OrderStatus.Completed => "Completed",
        _ => "Updated"
    };
}

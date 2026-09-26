using MarketLink.Controllers;
using MarketLink.Data;
using MarketLink.Models;
using MarketLink.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Controllers;

/// <summary>Customer-side order management: view, modify, cancel and reorder.</summary>
[Authorize(Roles = Roles.Customer)]
public class OrdersController : AppControllerBase
{
    private readonly INotificationService _notifications;
    private readonly ICartService _cart;

    public OrdersController(MarketLinkDbContext db, INotificationService notifications, ICartService cart)
        : base(db)
    {
        _notifications = notifications;
        _cart = cart;
    }

    // GET: /Orders
    public async Task<IActionResult> Index()
    {
        var orders = await Db.Orders
            .Include(o => o.Farmer)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Where(o => o.CustomerId == CurrentUserId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
        return View(orders);
    }

    // GET: /Orders/Details/4
    public async Task<IActionResult> Details(int id)
    {
        var order = await Db.Orders
            .Include(o => o.Farmer)
            .Include(o => o.Market)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == CurrentUserId);

        if (order == null) return NotFound();
        return View(order);
    }

    // POST: /Orders/Cancel/4
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var order = await Db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Include(o => o.Farmer)
            .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == CurrentUserId);

        if (order == null) return NotFound();

        if (!order.CanModify)
        {
            TempData["Error"] = "This order can no longer be cancelled.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Put the reserved stock back.
        foreach (var item in order.Items)
            if (item.Product != null) item.Product.StockQuantity += item.Quantity;

        order.Status = OrderStatus.Cancelled;
        await Db.SaveChangesAsync();

        if (order.Farmer != null)
            await _notifications.NotifyAsync(order.Farmer.UserId, "Order cancelled",
                $"Order #{order.Id} was cancelled by the customer.", "/Farmer/Orders");

        TempData["Success"] = "Order cancelled.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Orders/Modify/4
    public async Task<IActionResult> Modify(int id)
    {
        var order = await Db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Include(o => o.Farmer)
            .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == CurrentUserId);

        if (order == null) return NotFound();
        if (!order.CanModify)
        {
            TempData["Error"] = "This order can no longer be modified.";
            return RedirectToAction(nameof(Details), new { id });
        }
        return View(order);
    }

    // POST: /Orders/Modify/4
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Modify(int id, Dictionary<int, int> quantities, DateTime pickupDate, string? note)
    {
        var order = await Db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == CurrentUserId);

        if (order == null) return NotFound();
        if (!order.CanModify)
        {
            TempData["Error"] = "This order can no longer be modified.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (pickupDate.Date < DateTime.Today)
            ModelState.AddModelError(nameof(pickupDate), "Pickup date must be today or later.");

        decimal total = 0;
        foreach (var item in order.Items)
        {
            if (!quantities.TryGetValue(item.Id, out var qty)) qty = item.Quantity;

            // The customer may increase or decrease; validate against live stock
            // (add back what this order already reserved first).
            var available = (item.Product?.StockQuantity ?? 0) + item.Quantity;
            if (qty <= 0 || qty > available)
            {
                ModelState.AddModelError(string.Empty,
                    $"Invalid quantity for '{item.Product?.Name}'. Available: {available}.");
                qty = item.Quantity;
            }

            if (item.Product != null)
                item.Product.StockQuantity += item.Quantity - qty; // adjust reserved stock

            item.Quantity = qty;
            item.LineTotal = item.UnitPrice * qty;
            total += item.LineTotal;
        }

        if (!ModelState.IsValid)
            return View(order);

        order.TotalAmount = total;
        order.PickupDate = pickupDate.Date;
        order.CutoffTime = pickupDate.Date;
        order.CustomerNote = note;
        await Db.SaveChangesAsync();

        TempData["Success"] = "Order updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: /Orders/Reorder/4  — copy a past order's items back into the cart
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reorder(int id)
    {
        var order = await Db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == CurrentUserId);

        if (order == null) return NotFound();

        var added = 0;
        foreach (var item in order.Items)
            if (await _cart.AddAsync(item.ProductId, item.Quantity)) added++;

        TempData[added > 0 ? "Success" : "Error"] = added > 0
            ? "Items added to your cart."
            : "None of these items are currently available.";
        return RedirectToAction("Index", "Cart");
    }
}

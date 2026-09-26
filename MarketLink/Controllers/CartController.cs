using MarketLink.Controllers;
using MarketLink.Data;
using MarketLink.Models;
using MarketLink.Models.ViewModels;
using MarketLink.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Controllers;

/// <summary>
/// The shopping cart and checkout. Checkout splits the cart by farmer and creates
/// one pre-order per farmer, reduces stock and notifies both sides.
/// </summary>
[Authorize(Roles = Roles.Customer)]
public class CartController : AppControllerBase
{
    private readonly ICartService _cart;
    private readonly INotificationService _notifications;

    public CartController(MarketLinkDbContext db, ICartService cart, INotificationService notifications)
        : base(db)
    {
        _cart = cart;
        _notifications = notifications;
    }

    // GET: /Cart
    public IActionResult Index()
    {
        ViewBag.Total = _cart.GetTotal();
        return View(_cart.GetItems());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int productId, int quantity = 1)
    {
        var ok = await _cart.AddAsync(productId, quantity);
        TempData[ok ? "Success" : "Error"] = ok
            ? "Added to your cart."
            : "That item is not available right now.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Update(int productId, int quantity)
    {
        _cart.UpdateQuantity(productId, quantity);
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Remove(int productId)
    {
        _cart.Remove(productId);
        TempData["Success"] = "Item removed.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Clear()
    {
        _cart.Clear();
        TempData["Success"] = "Cart cleared.";
        return RedirectToAction("Index");
    }

    // GET: /Cart/Checkout
    public async Task<IActionResult> Checkout()
    {
        var groups = _cart.GroupByFarmer();
        if (groups.Count == 0)
        {
            TempData["Error"] = "Your cart is empty.";
            return RedirectToAction("Index");
        }

        var model = new CheckoutViewModel();
        foreach (var (farmerId, items) in groups)
        {
            var farmer = await Db.FarmerProfiles
                .Include(f => f.FarmerMarkets).ThenInclude(fm => fm.Market)
                .FirstOrDefaultAsync(f => f.Id == farmerId);

            var options = (farmer?.FarmerMarkets ?? new List<FarmerMarket>())
                .Select(fm => new SelectListItem
                {
                    Value = fm.Id.ToString(),
                    Text = $"{fm.Market?.Name} — {fm.OperatingDay} ({fm.PickupStartTime}–{fm.PickupEndTime})"
                })
                .ToList();

            model.FarmerGroups.Add(new CheckoutFarmerGroup
            {
                FarmerProfileId = farmerId,
                StallName = farmer?.StallName ?? items.First().FarmerName,
                Items = items,
                PickupOptions = new SelectList(options, "Value", "Text", options.FirstOrDefault()?.Value),
                SelectedPickupOptionId = options.Count > 0 ? int.Parse(options[0].Value!) : 0
            });
        }

        return View(model);
    }

    // POST: /Cart/ConfirmCheckout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmCheckout(CheckoutViewModel model)
    {
        var groups = _cart.GroupByFarmer();
        if (groups.Count == 0)
        {
            TempData["Error"] = "Your cart is empty.";
            return RedirectToAction("Index");
        }

        if (model.PickupDate.Date < DateTime.Today)
        {
            ModelState.AddModelError(nameof(model.PickupDate), "Pickup date must be today or later.");
        }

        // Read the chosen pickup option for each farmer from the posted form.
        var chosen = Request.Form["chosenPickup"];

        if (!ModelState.IsValid)
        {
            // Rebuild the view model so validation errors can be shown.
            return await RebuildCheckoutView(model);
        }

        var createdOrderIds = new List<int>();

        foreach (var (farmerId, items) in groups)
        {
            var farmer = await Db.FarmerProfiles.FirstOrDefaultAsync(f => f.Id == farmerId);
            if (farmer == null) continue;

            // Find the FarmerMarket row the customer picked for this farmer.
            var pickedId = chosen.FirstOrDefault(c => c != null && c.StartsWith(farmerId + ":"));
            int farmerMarketId = pickedId != null ? int.Parse(pickedId.Split(':')[1]) : 0;
            var farmerMarket = await Db.FarmerMarkets
                .Include(fm => fm.Market)
                .FirstOrDefaultAsync(fm => fm.Id == farmerMarketId && fm.FarmerProfileId == farmerId);

            var order = new Order
            {
                CustomerId = CurrentUserId!,
                FarmerProfileId = farmerId,
                MarketId = farmerMarket?.MarketId,
                Status = OrderStatus.Placed,
                OrderDate = DateTime.UtcNow,
                PickupDate = model.PickupDate.Date,
                PickupTimeSlot = farmerMarket != null
                    ? $"{farmerMarket.PickupStartTime}–{farmerMarket.PickupEndTime}"
                    : "Market hours",
                CutoffTime = model.PickupDate.Date, // modify/cancel allowed until pickup day
                CustomerNote = model.Note
            };

            decimal total = 0;
            foreach (var item in items)
            {
                var product = await Db.Products.FirstOrDefaultAsync(p => p.Id == item.ProductId);
                if (product == null || product.StockQuantity < item.Quantity)
                {
                    TempData["Error"] = $"Sorry, '{item.ProductName}' no longer has enough stock.";
                    return RedirectToAction("Index");
                }

                product.StockQuantity -= item.Quantity;

                var lineTotal = product.Price * item.Quantity;
                total += lineTotal;
                order.Items.Add(new OrderItem
                {
                    Product = product,
                    ProductId = product.Id,
                    Quantity = item.Quantity,
                    UnitPrice = product.Price,
                    LineTotal = lineTotal
                });
            }

            order.TotalAmount = total;
            Db.Orders.Add(order);
            await Db.SaveChangesAsync();
            createdOrderIds.Add(order.Id);

            // Notify the farmer and the customer.
            await _notifications.NotifyAsync(farmer.UserId,
                "New pre-order received",
                $"Order #{order.Id} from {User.Identity?.Name}. Total {total:0.00}. Please accept or decline it.",
                "/Farmer/Orders");

            await _notifications.NotifyAsync(CurrentUserId!,
                "Order placed",
                $"Your pre-order #{order.Id} with {farmer.StallName} is placed. Pickup on {order.PickupDate:dd MMM yyyy}.",
                "/Orders");
        }

        _cart.Clear();
        TempData["Success"] = $"Thank you! {createdOrderIds.Count} order(s) placed successfully.";
        return RedirectToAction("Index", "Orders");
    }

    private async Task<IActionResult> RebuildCheckoutView(CheckoutViewModel posted)
    {
        var groups = _cart.GroupByFarmer();
        var model = new CheckoutViewModel { PickupDate = posted.PickupDate, Note = posted.Note };

        foreach (var (farmerId, items) in groups)
        {
            var farmer = await Db.FarmerProfiles
                .Include(f => f.FarmerMarkets).ThenInclude(fm => fm.Market)
                .FirstOrDefaultAsync(f => f.Id == farmerId);

            var options = (farmer?.FarmerMarkets ?? new List<FarmerMarket>())
                .Select(fm => new SelectListItem
                {
                    Value = fm.Id.ToString(),
                    Text = $"{fm.Market?.Name} — {fm.OperatingDay} ({fm.PickupStartTime}–{fm.PickupEndTime})"
                }).ToList();

            model.FarmerGroups.Add(new CheckoutFarmerGroup
            {
                FarmerProfileId = farmerId,
                StallName = farmer?.StallName ?? items.First().FarmerName,
                Items = items,
                PickupOptions = new SelectList(options, "Value", "Text", options.FirstOrDefault()?.Value)
            });
        }

        return View("Checkout", model);
    }
}

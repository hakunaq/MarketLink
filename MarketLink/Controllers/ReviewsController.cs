using MarketLink.Controllers;
using MarketLink.Data;
using MarketLink.Models;
using MarketLink.Models.ViewModels;
using MarketLink.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Controllers;

/// <summary>Customers leave ratings/reviews on products or farmers after ordering.</summary>
[Authorize(Roles = Roles.Customer)]
public class ReviewsController : AppControllerBase
{
    private readonly INotificationService _notifications;

    public ReviewsController(MarketLinkDbContext db, INotificationService notifications) : base(db)
    {
        _notifications = notifications;
    }

    // GET: /Reviews/Create?productId=7   (or ?farmerProfileId=3)
    public async Task<IActionResult> Create(int? productId, int? farmerProfileId, int? orderItemId)
    {
        var model = new CreateReviewViewModel
        {
            ProductId = productId,
            FarmerProfileId = farmerProfileId,
            OrderItemId = orderItemId
        };

        if (productId.HasValue)
        {
            var product = await Db.Products.Include(p => p.Farmer)
                .FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null) return NotFound();
            model.ProductName = product.Name;
            model.FarmerName = product.Farmer?.StallName;
            // A product review also counts toward the farmer's average.
            model.FarmerProfileId ??= product.FarmerProfileId;
        }
        else if (farmerProfileId.HasValue)
        {
            var farmer = await Db.FarmerProfiles.FirstOrDefaultAsync(f => f.Id == farmerProfileId);
            if (farmer == null) return NotFound();
            model.FarmerName = farmer.StallName;
        }
        else
        {
            return BadRequest("A product or farmer is required.");
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateReviewViewModel model)
    {
        if (!model.ProductId.HasValue && !model.FarmerProfileId.HasValue)
            return BadRequest();

        if (!ModelState.IsValid) return View(model);

        var review = new Review
        {
            CustomerId = CurrentUserId!,
            ProductId = model.ProductId,
            FarmerProfileId = model.FarmerProfileId,
            OrderId = null,
            Rating = model.Rating,
            Comment = model.Comment,
            ReviewDate = DateTime.UtcNow
        };

        // If the review came from a specific order line, link it and flag reviewed.
        if (model.OrderItemId.HasValue)
        {
            var item = await Db.OrderItems.FirstOrDefaultAsync(i =>
                i.Id == model.OrderItemId && i.Order!.CustomerId == CurrentUserId);
            if (item != null)
            {
                item.IsReviewed = true;
                review.OrderId = item.OrderId;
            }
        }

        Db.Reviews.Add(review);
        await Db.SaveChangesAsync();

        // Let the farmer know someone reviewed them.
        if (review.FarmerProfileId.HasValue)
        {
            var farmer = await Db.FarmerProfiles.FirstOrDefaultAsync(f => f.Id == review.FarmerProfileId);
            if (farmer != null)
                await _notifications.NotifyAsync(farmer.UserId, "New review",
                    $"You received a {review.Rating}-star review.", "/Farmer/Reviews");
        }

        TempData["Success"] = "Thanks for your review!";
        return RedirectAfterReview(model);
    }

    private IActionResult RedirectAfterReview(CreateReviewViewModel model)
    {
        if (model.ProductId.HasValue)
            return RedirectToAction("Details", "Products", new { id = model.ProductId });
        return RedirectToAction("Details", "Farmers", new { id = model.FarmerProfileId });
    }
}

using MarketLink.Controllers;
using MarketLink.Data;
using MarketLink.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Controllers;

/// <summary>In-app notifications (the bell icon in the navbar).</summary>
[Authorize]
public class NotificationsController : AppControllerBase
{
    private readonly INotificationService _notifications;

    public NotificationsController(MarketLinkDbContext db, INotificationService notifications) : base(db)
    {
        _notifications = notifications;
    }

    // GET: /Notifications
    public async Task<IActionResult> Index()
    {
        var items = await Db.Notifications
            .Where(n => n.UserId == CurrentUserId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .ToListAsync();
        return View(items);
    }

    // GET: /Notifications/Count — polled by the navbar for the badge
    [HttpGet]
    public async Task<IActionResult> Count()
    {
        var count = await _notifications.GetUnreadCountAsync(CurrentUserId!);
        return Json(new { count });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        await _notifications.MarkAllReadAsync(CurrentUserId!);
        return RedirectToAction(nameof(Index));
    }
}

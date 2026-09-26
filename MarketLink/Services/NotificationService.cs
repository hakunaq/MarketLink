using MarketLink.Data;
using MarketLink.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Services;

public interface INotificationService
{
    /// <summary>Creates an in-app notification and (optionally) sends an email.</summary>
    Task NotifyAsync(string userId, string title, string message, string? linkUrl = null, bool sendEmail = true);
    Task<List<Notification>> GetUnreadAsync(string userId);
    Task<int> GetUnreadCountAsync(string userId);
    Task MarkAllReadAsync(string userId);
}

/// <summary>
/// Handles customer/farmer alerts. Notifications are stored in the database so
/// they appear in the bell icon in the navbar, and important ones are also
/// emailed through <see cref="IEmailSender"/>.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly MarketLinkDbContext _db;
    private readonly IEmailSender _emailSender;
    private readonly UserManager<ApplicationUser> _userManager;

    public NotificationService(
        MarketLinkDbContext db,
        IEmailSender emailSender,
        UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _emailSender = emailSender;
        _userManager = userManager;
    }

    public async Task NotifyAsync(string userId, string title, string message, string? linkUrl = null, bool sendEmail = true)
    {
        _db.Notifications.Add(new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            LinkUrl = linkUrl,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        if (sendEmail)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user?.Email != null)
            {
                await _emailSender.SendEmailAsync(user.Email, title, message);
            }
        }
    }

    public Task<List<Notification>> GetUnreadAsync(string userId)
        => _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .Take(20)
            .ToListAsync();

    public Task<int> GetUnreadCountAsync(string userId)
        => _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

    public async Task MarkAllReadAsync(string userId)
    {
        var unread = await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();
        foreach (var n in unread) n.IsRead = true;
        await _db.SaveChangesAsync();
    }
}

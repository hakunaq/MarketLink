using System.ComponentModel.DataAnnotations;

namespace MarketLink.Models;

/// <summary>
/// An in-app alert shown to a specific user (order confirmations, ready for
/// pickup, approval notices, and so on).
/// </summary>
public class Notification
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    [Required, StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsRead { get; set; }

    /// <summary>Optional link the notification points to.</summary>
    [StringLength(250)]
    public string? LinkUrl { get; set; }
}

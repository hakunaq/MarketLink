using System.ComponentModel.DataAnnotations;

namespace MarketLink.Models;

/// <summary>
/// A platform-wide message published by an admin and shown to all users.
/// </summary>
public class Announcement
{
    public int Id { get; set; }

    [Required, StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(1000)]
    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;
}

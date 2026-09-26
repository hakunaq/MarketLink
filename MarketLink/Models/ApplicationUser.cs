using Microsoft.AspNetCore.Identity;

namespace MarketLink.Models;

/// <summary>
/// The single user account for the platform. A user's role (Admin, Farmer or
/// Customer) is stored by ASP.NET Core Identity. Farmers get an extra
/// <see cref="FarmerProfile"/> record with their stall details.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    public string? Address { get; set; }

    /// <summary>Admins can deactivate an account (used for customers who break the rules).</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public FarmerProfile? FarmerProfile { get; set; }
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}

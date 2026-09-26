using System.ComponentModel.DataAnnotations;

namespace MarketLink.Models;

/// <summary>
/// Extra details for a user who sells at markets. One ApplicationUser (with the
/// Farmer role) has exactly one FarmerProfile. Admins must approve a profile
/// before the farmer can list products.
/// </summary>
public class FarmerProfile
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    [Required, StringLength(120)]
    [Display(Name = "Stall / Business Name")]
    public string StallName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "Contact Person")]
    public string ContactPerson { get; set; } = string.Empty;

    [Required, StringLength(20)]
    [Display(Name = "Contact Number")]
    public string ContactNumber { get; set; } = string.Empty;

    [StringLength(600)]
    public string? Bio { get; set; }

    [StringLength(250)]
    [Display(Name = "Pickup Address")]
    public string? AddressText { get; set; }

    [Display(Name = "Latitude")]
    [Range(-90, 90)]
    public double Latitude { get; set; }

    [Display(Name = "Longitude")]
    [Range(-180, 180)]
    public double Longitude { get; set; }

    [StringLength(30)]
    public string MapProvider { get; set; } = "OpenStreetMap";

    [StringLength(250)]
    public string? ProfileImageUrl { get; set; }

    /// <summary>Set to true once an admin approves the farmer.</summary>
    public bool IsApproved { get; set; } = false;

    /// <summary>Set to true to temporarily block a farmer from selling.</summary>
    public bool IsSuspended { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<FarmerMarket> FarmerMarkets { get; set; } = new List<FarmerMarket>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}

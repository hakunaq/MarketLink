using System.ComponentModel.DataAnnotations;

namespace MarketLink.Models;

/// <summary>
/// A physical farmers market where stalls set up on certain days.
/// </summary>
public class Market
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    [Display(Name = "Market Name")]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(250)]
    public string Address { get; set; } = string.Empty;

    [Range(-90, 90)]
    public double Latitude { get; set; }

    [Range(-180, 180)]
    public double Longitude { get; set; }

    [StringLength(30)]
    public string MapProvider { get; set; } = "OpenStreetMap";

    /// <summary>Comma-separated days, e.g. "Saturday, Sunday".</summary>
    [StringLength(120)]
    [Display(Name = "Operating Days")]
    public string OperatingDays { get; set; } = string.Empty;

    [StringLength(20)]
    [Display(Name = "Opens At")]
    public string OpenTime { get; set; } = "08:00";

    [StringLength(20)]
    [Display(Name = "Closes At")]
    public string CloseTime { get; set; } = "14:00";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<FarmerMarket> FarmerMarkets { get; set; } = new List<FarmerMarket>();
}

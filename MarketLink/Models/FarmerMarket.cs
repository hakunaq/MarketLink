using System.ComponentModel.DataAnnotations;

namespace MarketLink.Models;

/// <summary>
/// Links a farmer to a market on a specific day, with the pickup window and
/// stall location. A farmer can sell at many markets and a market hosts many
/// farmers, so this is the many-to-many join table.
/// </summary>
public class FarmerMarket
{
    public int Id { get; set; }

    public int FarmerProfileId { get; set; }
    public FarmerProfile? FarmerProfile { get; set; }

    public int MarketId { get; set; }
    public Market? Market { get; set; }

    [Required, StringLength(20)]
    [Display(Name = "Operating Day")]
    public string OperatingDay { get; set; } = string.Empty;

    [StringLength(20)]
    [Display(Name = "Pickup From")]
    public string PickupStartTime { get; set; } = "08:00";

    [StringLength(20)]
    [Display(Name = "Pickup To")]
    public string PickupEndTime { get; set; } = "13:00";

    [StringLength(40)]
    [Display(Name = "Stall Number / Spot")]
    public string? StallNumber { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace MarketLink.Models;

/// <summary>
/// A customer rating + comment. A review targets either a single product
/// (ProductId set) or a farmer overall (FarmerProfileId set). For product
/// reviews we also store the farmer so averages can be rolled up.
/// </summary>
public class Review
{
    public int Id { get; set; }

    [Required]
    public string CustomerId { get; set; } = string.Empty;
    public ApplicationUser? Customer { get; set; }

    public int? ProductId { get; set; }
    public Product? Product { get; set; }

    public int? FarmerProfileId { get; set; }
    public FarmerProfile? Farmer { get; set; }

    /// <summary>The order this review came from, if any.</summary>
    public int? OrderId { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; } = 5;

    [StringLength(600)]
    public string? Comment { get; set; }

    public DateTime ReviewDate { get; set; } = DateTime.UtcNow;

    [StringLength(600)]
    public string? FarmerReply { get; set; }

    public DateTime? FarmerReplyDate { get; set; }

    /// <summary>Admins can hide a review that breaks the guidelines.</summary>
    public bool IsHidden { get; set; }
}

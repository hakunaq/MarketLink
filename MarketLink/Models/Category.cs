using System.ComponentModel.DataAnnotations;

namespace MarketLink.Models;

/// <summary>
/// A product category (vegetables, fruits, dairy, baked goods, ...).
/// Managed by admins as master data.
/// </summary>
public class Category
{
    public int Id { get; set; }

    [Required, StringLength(60)]
    public string Name { get; set; } = string.Empty;

    [StringLength(250)]
    public string? Description { get; set; }

    /// <summary>Optional Bootstrap Icons class name used in the UI, e.g. "bi-egg-fried".</summary>
    [StringLength(60)]
    public string? IconClass { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}

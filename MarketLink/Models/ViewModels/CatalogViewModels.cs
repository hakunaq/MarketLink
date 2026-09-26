using MarketLink.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MarketLink.Models.ViewModels;

/// <summary>Holds the filter inputs and the matching products for the browse page.</summary>
public class ProductFilterViewModel
{
    public string? SearchTerm { get; set; }
    public int? CategoryId { get; set; }
    public int? MarketId { get; set; }
    public string? Day { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string SortBy { get; set; } = "newest";

    public List<Product> Products { get; set; } = new();
    public SelectList? Categories { get; set; }
    public SelectList? Markets { get; set; }
    public List<string> Days { get; set; } = new()
    {
        "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"
    };
}

/// <summary>Form used by farmers to add or edit a product.</summary>
public class ProductFormViewModel
{
    public int Id { get; set; }

    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.StringLength(600)]
    public string? Description { get; set; }

    [System.ComponentModel.DataAnnotations.Range(0.01, 100000)]
    public decimal Price { get; set; }

    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(30)]
    public string Unit { get; set; } = "kg";

    [System.ComponentModel.DataAnnotations.Range(0, 100000)]
    [System.ComponentModel.DataAnnotations.Display(Name = "Quantity Available")]
    public int StockQuantity { get; set; }

    [System.ComponentModel.DataAnnotations.StringLength(250)]
    [System.ComponentModel.DataAnnotations.Display(Name = "Image URL")]
    public string? ImageUrl { get; set; }

    [System.ComponentModel.DataAnnotations.Display(Name = "Available for sale")]
    public bool IsAvailable { get; set; } = true;

    [System.ComponentModel.DataAnnotations.Display(Name = "Category")]
    public int CategoryId { get; set; }

    public SelectList? Categories { get; set; }
}

/// <summary>One farmer's section of the checkout page.</summary>
public class CheckoutFarmerGroup
{
    public int FarmerProfileId { get; set; }
    public string StallName { get; set; } = string.Empty;
    public List<CartItem> Items { get; set; } = new();
    public decimal Subtotal => Items.Sum(i => i.LineTotal);

    /// <summary>Available pickup options (FarmerMarket rows) for this farmer.</summary>
    public SelectList? PickupOptions { get; set; }

    /// <summary>The chosen FarmerMarket id.</summary>
    public int SelectedPickupOptionId { get; set; }
}

public class CheckoutViewModel
{
    public List<CheckoutFarmerGroup> FarmerGroups { get; set; } = new();

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.Display(Name = "Pickup Date")]
    public DateTime PickupDate { get; set; } = DateTime.Today.AddDays(1);

    [System.ComponentModel.DataAnnotations.StringLength(300)]
    public string? Note { get; set; }

    public decimal GrandTotal => FarmerGroups.Sum(g => g.Subtotal);
}

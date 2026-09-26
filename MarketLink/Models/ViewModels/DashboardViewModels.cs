using MarketLink.Models;

namespace MarketLink.Models.ViewModels;

/// <summary>Numbers and lists shown on the farmer's dashboard.</summary>
public class FarmerDashboardViewModel
{
    public FarmerProfile Profile { get; set; } = new();

    public int TotalOrders { get; set; }
    public int PendingOrders { get; set; }
    public int CompletedOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public int ActiveProducts { get; set; }
    public int SoldOutProducts { get; set; }
    public double AverageRating { get; set; }

    public List<Order> RecentOrders { get; set; } = new();
    public List<BestSellerRow> BestSellers { get; set; } = new();
    public List<Review> RecentReviews { get; set; } = new();
}

public class BestSellerRow
{
    public string ProductName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}

/// <summary>Key platform metrics shown on the admin dashboard.</summary>
public class AdminDashboardViewModel
{
    public int TotalFarmers { get; set; }
    public int PendingFarmers { get; set; }
    public int TotalCustomers { get; set; }
    public int TotalMarkets { get; set; }
    public int TotalProducts { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }

    public List<Order> RecentOrders { get; set; } = new();
    public List<AdminFarmerRow> FarmersAwaitingApproval { get; set; } = new();
    public List<TopFarmerRow> MostActiveFarmers { get; set; } = new();
}

public class AdminFarmerRow
{
    public int FarmerProfileId { get; set; }
    public string StallName { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class TopFarmerRow
{
    public string StallName { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal Revenue { get; set; }
}

public class MarketRevenueRow
{
    public string Market { get; set; } = string.Empty;
    public int Orders { get; set; }
    public decimal Revenue { get; set; }
}

/// <summary>Customer's home dashboard: favorites, recent orders, announcements.</summary>
public class CustomerDashboardViewModel
{
    public int TotalOrders { get; set; }
    public int ActiveOrders { get; set; }
    public int FavoriteCount { get; set; }

    public List<Order> RecentOrders { get; set; } = new();
    public List<Favorite> FavoriteFarmers { get; set; } = new();
    public List<Favorite> FavoriteProducts { get; set; } = new();
    public List<Product> RecommendedProducts { get; set; } = new();
}

/// <summary>Inputs for leaving a review after a completed order.</summary>
public class CreateReviewViewModel
{
    public int? OrderItemId { get; set; }
    public int? ProductId { get; set; }
    public int? FarmerProfileId { get; set; }

    public string? ProductName { get; set; }
    public string? FarmerName { get; set; }

    [System.ComponentModel.DataAnnotations.Range(1, 5)]
    public int Rating { get; set; } = 5;

    [System.ComponentModel.DataAnnotations.StringLength(600)]
    public string? Comment { get; set; }
}

/// <summary>Landing page content.</summary>
public class HomeViewModel
{
    public List<Product> FeaturedProducts { get; set; } = new();
    public List<Market> Markets { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public List<Announcement> Announcements { get; set; } = new();
    public int FarmerCount { get; set; }
    public int ProductCount { get; set; }
}

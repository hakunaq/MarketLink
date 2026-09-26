namespace MarketLink.Models;

/// <summary>
/// Central place for the role names used across the application.
/// Keeping them as constants avoids typos in [Authorize] attributes and seeding.
/// </summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string Farmer = "Farmer";
    public const string Customer = "Customer";
}

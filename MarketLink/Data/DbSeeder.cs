using MarketLink.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Data;

/// <summary>
/// Fills the database with roles, demo users, markets, categories and products
/// the first time the app runs. Every method checks whether data already exists
/// so it is safe to call on every startup.
/// </summary>
public static class DbSeeder
{
    // Shared demo passwords (also listed in the README).
    private const string AdminPassword = "Admin@123";
    private const string FarmerPassword = "Farmer@123";
    private const string CustomerPassword = "Customer@123";

    public static async Task SeedAsync(
        MarketLinkDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        await SeedRolesAsync(roleManager);
        await SeedAdminAsync(userManager);

        if (!await db.Categories.AnyAsync())
        {
            await SeedCategoriesAsync(db);
        }

        if (!await db.Markets.AnyAsync())
        {
            await SeedMarketsAsync(db);
        }

        if (!await db.FarmerProfiles.AnyAsync())
        {
            await SeedFarmersAsync(db, userManager);
        }

        if (!await db.Products.AnyAsync())
        {
            await SeedProductsAsync(db);
        }

        if (!await db.Customers().AnyAsync())
        {
            await SeedCustomersAsync(db, userManager);
        }

        if (!await db.Announcements.AnyAsync())
        {
            db.Announcements.Add(new Announcement
            {
                Title = "Welcome to MarketLink!",
                Message = "Browse local farmers markets, reserve fresh produce and pay at pickup. " +
                          "Use the assistant (bottom-right) to find items or market timings.",
                IsActive = true
            });
            await db.SaveChangesAsync();
        }
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        string[] roles = { Roles.Admin, Roles.Farmer, Roles.Customer };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    private static async Task SeedAdminAsync(UserManager<ApplicationUser> userManager)
    {
        const string email = "admin@marketlink.com";
        if (await userManager.FindByEmailAsync(email) != null) return;

        var admin = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = "Platform Administrator",
            Address = "MarketLink Head Office",
            EmailConfirmed = true,
            IsActive = true
        };
        var result = await userManager.CreateAsync(admin, AdminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, Roles.Admin);
        }
    }

    private static async Task SeedCategoriesAsync(MarketLinkDbContext db)
    {
        db.Categories.AddRange(
            new Category { Name = "Vegetables", Description = "Fresh seasonal vegetables", IconClass = "bi-flower1" },
            new Category { Name = "Fruits", Description = "Orchard-fresh fruit", IconClass = "bi-bag-heart" },
            new Category { Name = "Dairy", Description = "Milk, cheese, butter and yoghurt", IconClass = "bi-egg-fried" },
            new Category { Name = "Baked Goods", Description = "Bread, pies and pastries", IconClass = "bi-cake2" },
            new Category { Name = "Herbs", Description = "Cut herbs and microgreens", IconClass = "bi-tree" },
            new Category { Name = "Eggs & Poultry", Description = "Free-range eggs and poultry", IconClass = "bi-egg" },
            new Category { Name = "Honey & Preserves", Description = "Honey, jams and pickles", IconClass = "bi-jar" }
        );
        await db.SaveChangesAsync();
    }

    private static async Task SeedMarketsAsync(MarketLinkDbContext db)
    {
        db.Markets.AddRange(
            new Market
            {
                Name = "Riverside Saturday Market",
                Address = "12 River Road, Greenfield",
                Latitude = 12.9716,
                Longitude = 77.5946,
                OperatingDays = "Saturday",
                OpenTime = "07:00",
                CloseTime = "14:00"
            },
            new Market
            {
                Name = "Town Square Farmers Market",
                Address = "Town Square, Main Street, Greenfield",
                Latitude = 12.9811,
                Longitude = 77.5870,
                OperatingDays = "Wednesday, Sunday",
                OpenTime = "08:00",
                CloseTime = "13:00"
            },
            new Market
            {
                Name = "Hilltop Community Market",
                Address = "Hilltop Park, North Avenue, Greenfield",
                Latitude = 12.9611,
                Longitude = 77.6100,
                OperatingDays = "Friday",
                OpenTime = "09:00",
                CloseTime = "15:00"
            }
        );
        await db.SaveChangesAsync();
    }

    private static async Task SeedFarmersAsync(
        MarketLinkDbContext db,
        UserManager<ApplicationUser> userManager)
    {
        var farmerSeeds = new (string Email, string Name, string Stall, string Contact, string Phone, string Bio, double Lat, double Lng, string Address)[]
        {
            ("green@marketlink.com", "Asha Green", "Green Acres Farm", "Asha Green", "555-0101",
             "Family-run organic vegetable farm. Harvested the morning of market day.",
             12.9720, 77.5950, "Stall 4, Riverside Saturday Market"),
            ("orchard@marketlink.com", "Ravi Orchard", "Sunrise Orchards", "Ravi Kumar", "555-0102",
             "Stone fruit, apples and citrus grown on our hillside orchard.",
             12.9815, 77.5875, "Stall 9, Town Square Farmers Market"),
            ("dairy@marketlink.com", "Meera Dairy", "Meadow Dairy Co.", "Meera Nair", "555-0103",
             "Grass-fed dairy: milk, cheese, butter and cultured yoghurt.",
             12.9615, 77.6105, "Stall 2, Hilltop Community Market")
        };

        foreach (var seed in farmerSeeds)
        {
            var user = new ApplicationUser
            {
                UserName = seed.Email,
                Email = seed.Email,
                FullName = seed.Name,
                Address = seed.Address,
                PhoneNumber = seed.Phone,
                EmailConfirmed = true,
                IsActive = true
            };
            var created = await userManager.CreateAsync(user, FarmerPassword);
            if (!created.Succeeded) continue;
            await userManager.AddToRoleAsync(user, Roles.Farmer);

            var profile = new FarmerProfile
            {
                UserId = user.Id,
                StallName = seed.Stall,
                ContactPerson = seed.Contact,
                ContactNumber = seed.Phone,
                Bio = seed.Bio,
                AddressText = seed.Address,
                Latitude = seed.Lat,
                Longitude = seed.Lng,
                IsApproved = true,
                IsSuspended = false
            };
            db.FarmerProfiles.Add(profile);
        }

        await db.SaveChangesAsync();

        // Link each farmer to a market with a pickup window.
        var markets = await db.Markets.OrderBy(m => m.Id).ToListAsync();
        var profiles = await db.FarmerProfiles.OrderBy(f => f.Id).ToListAsync();

        if (markets.Count >= 3 && profiles.Count >= 3)
        {
            db.FarmerMarkets.AddRange(
                new FarmerMarket { FarmerProfileId = profiles[0].Id, MarketId = markets[0].Id, OperatingDay = "Saturday", PickupStartTime = "07:00", PickupEndTime = "13:00", StallNumber = "Stall 4" },
                new FarmerMarket { FarmerProfileId = profiles[1].Id, MarketId = markets[1].Id, OperatingDay = "Sunday", PickupStartTime = "08:00", PickupEndTime = "12:00", StallNumber = "Stall 9" },
                new FarmerMarket { FarmerProfileId = profiles[1].Id, MarketId = markets[1].Id, OperatingDay = "Wednesday", PickupStartTime = "08:00", PickupEndTime = "12:00", StallNumber = "Stall 9" },
                new FarmerMarket { FarmerProfileId = profiles[2].Id, MarketId = markets[2].Id, OperatingDay = "Friday", PickupStartTime = "09:00", PickupEndTime = "14:00", StallNumber = "Stall 2" }
            );
            await db.SaveChangesAsync();
        }
    }

    private static async Task SeedProductsAsync(MarketLinkDbContext db)
    {
        var farmers = await db.FarmerProfiles.OrderBy(f => f.Id).ToListAsync();
        var cats = await db.Categories.ToDictionaryAsync(c => c.Name, c => c.Id);
        if (farmers.Count < 3) return;

        var veg = farmers[0].Id;
        var fruit = farmers[1].Id;
        var dairy = farmers[2].Id;

        db.Products.AddRange(
            new Product { FarmerProfileId = veg, CategoryId = cats["Vegetables"], Name = "Heirloom Tomatoes", Description = "Vine-ripened mixed heirloom tomatoes.", Price = 3.50m, Unit = "kg", StockQuantity = 60, ImageUrl = "/img/products/tomatoes.svg", IsAvailable = true },
            new Product { FarmerProfileId = veg, CategoryId = cats["Vegetables"], Name = "Rainbow Carrots", Description = "Sweet mixed-colour carrots, bunched.", Price = 2.20m, Unit = "bunch", StockQuantity = 45, ImageUrl = "/img/products/carrots.svg", IsAvailable = true },
            new Product { FarmerProfileId = veg, CategoryId = cats["Vegetables"], Name = "Baby Spinach", Description = "Tender spinach leaves, washed.", Price = 2.75m, Unit = "250g", StockQuantity = 30, ImageUrl = "/img/products/spinach.svg", IsAvailable = true },
            new Product { FarmerProfileId = veg, CategoryId = cats["Herbs"], Name = "Fresh Basil", Description = "Aromatic basil, cut this morning.", Price = 1.50m, Unit = "bunch", StockQuantity = 25, ImageUrl = "/img/products/basil.svg", IsAvailable = true },
            new Product { FarmerProfileId = veg, CategoryId = cats["Vegetables"], Name = "Zucchini", Description = "Young green zucchini.", Price = 2.00m, Unit = "kg", StockQuantity = 0, ImageUrl = "/img/products/zucchini.svg", IsAvailable = true },

            new Product { FarmerProfileId = fruit, CategoryId = cats["Fruits"], Name = "Honeycrisp Apples", Description = "Crisp and sweet orchard apples.", Price = 4.00m, Unit = "kg", StockQuantity = 80, ImageUrl = "/img/products/apples.svg", IsAvailable = true },
            new Product { FarmerProfileId = fruit, CategoryId = cats["Fruits"], Name = "Juicy Oranges", Description = "Freshly picked citrus oranges.", Price = 3.20m, Unit = "kg", StockQuantity = 70, ImageUrl = "/img/products/oranges.svg", IsAvailable = true },
            new Product { FarmerProfileId = fruit, CategoryId = cats["Fruits"], Name = "Strawberries", Description = "Punnet of ripe strawberries.", Price = 5.00m, Unit = "punnet", StockQuantity = 20, ImageUrl = "/img/products/strawberries.svg", IsAvailable = true },
            new Product { FarmerProfileId = fruit, CategoryId = cats["Honey & Preserves"], Name = "Strawberry Jam", Description = "Small-batch preserves, no added pectin.", Price = 6.50m, Unit = "jar", StockQuantity = 18, ImageUrl = "/img/products/jam.svg", IsAvailable = true },

            new Product { FarmerProfileId = dairy, CategoryId = cats["Dairy"], Name = "Whole Milk", Description = "Creamy grass-fed whole milk.", Price = 1.80m, Unit = "litre", StockQuantity = 50, ImageUrl = "/img/products/milk.svg", IsAvailable = true },
            new Product { FarmerProfileId = dairy, CategoryId = cats["Dairy"], Name = "Farmhouse Cheddar", Description = "Mature cheddar wedge.", Price = 8.00m, Unit = "400g", StockQuantity = 22, ImageUrl = "/img/products/cheese.svg", IsAvailable = true },
            new Product { FarmerProfileId = dairy, CategoryId = cats["Eggs & Poultry"], Name = "Free-Range Eggs", Description = "Mixed-size free-range eggs.", Price = 4.50m, Unit = "dozen", StockQuantity = 40, ImageUrl = "/img/products/eggs.svg", IsAvailable = true },
            new Product { FarmerProfileId = dairy, CategoryId = cats["Dairy"], Name = "Cultured Yoghurt", Description = "Thick natural yoghurt.", Price = 3.00m, Unit = "500g", StockQuantity = 26, ImageUrl = "/img/products/yoghurt.svg", IsAvailable = true },
            new Product { FarmerProfileId = dairy, CategoryId = cats["Baked Goods"], Name = "Sourdough Loaf", Description = "Slow-fermented country sourdough.", Price = 5.50m, Unit = "loaf", StockQuantity = 15, ImageUrl = "/img/products/bread.svg", IsAvailable = true }
        );
        await db.SaveChangesAsync();
    }

    private static async Task SeedCustomersAsync(
        MarketLinkDbContext db,
        UserManager<ApplicationUser> userManager)
    {
        var customerSeeds = new (string Email, string Name, string Address, string Phone)[]
        {
            ("customer@marketlink.com", "Sam Rivera", "45 Maple Street, Greenfield", "555-0201"),
            ("priya@marketlink.com", "Priya Shah", "8 Oak Lane, Greenfield", "555-0202")
        };

        foreach (var seed in customerSeeds)
        {
            var user = new ApplicationUser
            {
                UserName = seed.Email,
                Email = seed.Email,
                FullName = seed.Name,
                Address = seed.Address,
                PhoneNumber = seed.Phone,
                EmailConfirmed = true,
                IsActive = true
            };
            var created = await userManager.CreateAsync(user, CustomerPassword);
            if (created.Succeeded)
            {
                await userManager.AddToRoleAsync(user, Roles.Customer);
            }
        }
    }
}

/// <summary>Small helper so the seeder can ask for just the customer accounts.</summary>
internal static class SeederQueryExtensions
{
    public static IQueryable<ApplicationUser> Customers(this MarketLinkDbContext db)
        => db.Users.Where(u => u.Email == "customer@marketlink.com" || u.Email == "priya@marketlink.com");
}

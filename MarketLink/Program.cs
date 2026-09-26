using MarketLink.Data;
using MarketLink.Models;
using MarketLink.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Database (Entity Framework Core + SQL Server)
// ---------------------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<MarketLinkDbContext>(options =>
    options.UseSqlServer(connectionString));

// ---------------------------------------------------------------------------
// Authentication & roles (ASP.NET Core Identity)
// ---------------------------------------------------------------------------
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // Reasonable, easy-to-remember rules for a demo/competition project.
        options.Password.RequiredLength = 6;
        options.Password.RequireDigit = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;

        options.User.RequireUniqueEmail = true;

        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
    })
    .AddEntityFrameworkStores<MarketLinkDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(4);
    options.SlidingExpiration = true;
});

// ---------------------------------------------------------------------------
// MVC, session (used for the shopping cart) and app services
// ---------------------------------------------------------------------------
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Email is written to the log in this demo (no SMTP server configured).
builder.Services.AddTransient<IEmailSender, LoggingEmailSender>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAiAssistantService, AiAssistantService>();
builder.Services.AddScoped<ICartService, CartService>();

var app = builder.Build();

// ---------------------------------------------------------------------------
// Create the database, apply migrations and seed demo data on first run.
// ---------------------------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MarketLinkDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    db.Database.Migrate();
    await DbSeeder.SeedAsync(db, userManager, roleManager);
}

// ---------------------------------------------------------------------------
// Request pipeline
// ---------------------------------------------------------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Home/NotFoundPage");
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// your existing code above...

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MarketLinkDbContext>();

    if (!db.Categories.Any())
    {
        db.Categories.AddRange(
            new Category { Name = "Vegetables", IsActive = true },
            new Category { Name = "Fruits", IsActive = true },
            new Category { Name = "Dairy", IsActive = true },
            new Category { Name = "Grains", IsActive = true },
            new Category { Name = "Herbs", IsActive = true },
            new Category { Name = "Organic", IsActive = true }
        );

        db.SaveChanges();
    }
}

app.Run();

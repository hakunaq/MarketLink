using System.Security.Claims;
using MarketLink.Data;
using MarketLink.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Controllers;

/// <summary>
/// Shared base for controllers that need to know who the logged-in user is and,
/// for farmers, which FarmerProfile belongs to them.
/// </summary>
public abstract class AppControllerBase : Controller
{
    protected readonly MarketLinkDbContext Db;

    protected AppControllerBase(MarketLinkDbContext db)
    {
        Db = db;
    }

    /// <summary>The signed-in user's id, or null when anonymous.</summary>
    protected string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    /// <summary>Returns the FarmerProfile owned by the current user (or null).</summary>
    protected async Task<FarmerProfile?> GetCurrentUserFarmerProfileAsync()
    {
        var userId = CurrentUserId;
        if (string.IsNullOrEmpty(userId)) return null;
        return await Db.FarmerProfiles.FirstOrDefaultAsync(f => f.UserId == userId);
    }
}

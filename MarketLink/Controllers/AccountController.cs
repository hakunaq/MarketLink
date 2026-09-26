using MarketLink.Data;
using MarketLink.Models;
using MarketLink.Models.ViewModels;
using MarketLink.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Controllers;

/// <summary>
/// Handles registration (customer and farmer), login and logout. Roles decide
/// where a user lands after signing in.
/// </summary>
public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly MarketLinkDbContext _db;
    private readonly INotificationService _notifications;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        MarketLinkDbContext db,
        INotificationService notifications)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
        _notifications = notifications;
    }

    // ---------------------------------------------------------------- Login
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (_signInManager.IsSignedIn(User)) return RedirectToAction("Index", "Home");
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        if (!user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "This account has been deactivated. Please contact support.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Too many failed attempts. Try again later.");
            return View(model);
        }

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        // A suspended farmer can sign in but is sent to a holding page.
        if (await _userManager.IsInRoleAsync(user, Roles.Farmer))
        {
            var profile = await _db.FarmerProfiles.FirstOrDefaultAsync(f => f.UserId == user.Id);
            if (profile is { IsSuspended: true })
                return RedirectToAction("Suspended", "Farmer");
        }

        return RedirectToLocal(model.ReturnUrl) ?? RedirectToActionForRole(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    // ------------------------------------------------- Register (customer)
    [HttpGet]
    public IActionResult RegisterCustomer() => View(new RegisterCustomerViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterCustomer(RegisterCustomerViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            PhoneNumber = model.PhoneNumber,
            Address = model.Address,
            EmailConfirmed = true,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        await _userManager.AddToRoleAsync(user, Roles.Customer);
        await _signInManager.SignInAsync(user, isPersistent: false);
        await _notifications.NotifyAsync(user.Id, "Welcome to MarketLink",
            "Your customer account is ready. Browse markets and reserve fresh produce for pickup.",
            "/Customer", sendEmail: false);

        return RedirectToAction("Index", "Customer");
    }

    // --------------------------------------------------- Register (farmer)
    [HttpGet]
    public IActionResult RegisterFarmer() => View(new RegisterFarmerViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterFarmer(RegisterFarmerViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.ContactPerson,
            PhoneNumber = model.PhoneNumber,
            Address = model.Address,
            EmailConfirmed = true,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        await _userManager.AddToRoleAsync(user, Roles.Farmer);

        _db.FarmerProfiles.Add(new FarmerProfile
        {
            UserId = user.Id,
            StallName = model.StallName,
            ContactPerson = model.ContactPerson,
            ContactNumber = model.PhoneNumber,
            AddressText = model.Address,
            IsApproved = false // waits for admin approval
        });
        await _db.SaveChangesAsync();

        await _signInManager.SignInAsync(user, isPersistent: false);
        await _notifications.NotifyAsync(user.Id, "Farmer account created",
            "Thanks for registering. An administrator will review your stall shortly. " +
            "You can complete your profile and add products in the meantime.",
            "/Farmer", sendEmail: false);

        return RedirectToAction("Index", "Farmer");
    }

    public IActionResult AccessDenied() => View();

    // ------------------------------------------------------------- helpers
    private IActionResult? RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return null;
    }

    private IActionResult RedirectToActionForRole(ApplicationUser user)
    {
        if (_userManager.GetRolesAsync(user).GetAwaiter().GetResult().Contains(Roles.Admin))
            return RedirectToAction("Index", "Admin");
        if (_userManager.GetRolesAsync(user).GetAwaiter().GetResult().Contains(Roles.Farmer))
            return RedirectToAction("Index", "Farmer");
        return RedirectToAction("Index", "Customer");
    }
}

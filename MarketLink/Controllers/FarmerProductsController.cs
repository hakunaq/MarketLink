using MarketLink.Controllers;
using MarketLink.Data;
using MarketLink.Models;
using MarketLink.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Controllers;

/// <summary>Farmers manage their weekly stock: add, edit, delete, mark sold out.</summary>
[Authorize(Roles = Roles.Farmer)]
public class FarmerProductsController : AppControllerBase
{
    public FarmerProductsController(MarketLinkDbContext db) : base(db) { }

    private async Task<FarmerProfile?> RequireProfileAsync()
    {
        var profile = await GetCurrentUserFarmerProfileAsync();
        if (profile is { IsSuspended: false }) return profile;
        return null;
    }

    // GET: /FarmerProducts
    public async Task<IActionResult> Index()
    {
        var profile = await RequireProfileAsync();
        if (profile == null) return RedirectToAction("Index", "Farmer");

        var products = await Db.Products
            .Include(p => p.Category)
            .Where(p => p.FarmerProfileId == profile.Id)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return View(products);
    }

    private async Task LoadCategoriesAsync(ProductFormViewModel model)
    {
        var cats = await Db.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();

        model.Categories = new SelectList(cats, "Id", "Name", model.CategoryId);
    }

    // GET: /FarmerProducts/Create
    public async Task<IActionResult> Create()
    {
        if (await RequireProfileAsync() == null) return RedirectToAction("Index", "Farmer");

        var model = new ProductFormViewModel();
        await LoadCategoriesAsync(model);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductFormViewModel model)
    {
        var profile = await RequireProfileAsync();
        if (profile == null) return RedirectToAction("Index", "Farmer");

        if (!profile.IsApproved)
        {
            TempData["Error"] = "Your stall must be approved by an admin before you can list products.";
            return RedirectToAction("Index", "Farmer");
        }

        if (!ModelState.IsValid)
        {
            await LoadCategoriesAsync(model);
            return View(model);
        }

        Db.Products.Add(new Product
        {
            FarmerProfileId = profile.Id,
            CategoryId = model.CategoryId,
            Name = model.Name,
            Description = model.Description,
            Price = model.Price,
            Unit = model.Unit,
            StockQuantity = model.StockQuantity,
            ImageUrl = string.IsNullOrWhiteSpace(model.ImageUrl) ? "/img/products/placeholder.svg" : model.ImageUrl,
            IsAvailable = model.IsAvailable,
            CreatedAt = DateTime.UtcNow
        });
        await Db.SaveChangesAsync();

        TempData["Success"] = "Product added.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /FarmerProducts/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var profile = await RequireProfileAsync();
        if (profile == null) return RedirectToAction("Index", "Farmer");

        var product = await Db.Products.FirstOrDefaultAsync(p => p.Id == id && p.FarmerProfileId == profile.Id);
        if (product == null) return NotFound();

        var model = new ProductFormViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Unit = product.Unit,
            StockQuantity = product.StockQuantity,
            ImageUrl = product.ImageUrl,
            IsAvailable = product.IsAvailable,
            CategoryId = product.CategoryId
        };

        await LoadCategoriesAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProductFormViewModel model)
    {
        var profile = await RequireProfileAsync();
        if (profile == null) return RedirectToAction("Index", "Farmer");

        var product = await Db.Products.FirstOrDefaultAsync(p => p.Id == id && p.FarmerProfileId == profile.Id);
        if (product == null) return NotFound();

        if (!ModelState.IsValid)
        {
            model.Id = id;
            await LoadCategoriesAsync(model);
            return View(model);
        }

        product.CategoryId = model.CategoryId;
        product.Name = model.Name;
        product.Description = model.Description;
        product.Price = model.Price;
        product.Unit = model.Unit;
        product.StockQuantity = model.StockQuantity;
        product.ImageUrl = string.IsNullOrWhiteSpace(model.ImageUrl) ? product.ImageUrl : model.ImageUrl;
        product.IsAvailable = model.IsAvailable;
        await Db.SaveChangesAsync();

        TempData["Success"] = "Product updated.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /FarmerProducts/ToggleAvailable/5 — mark sold out / available
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleAvailable(int id)
    {
        var profile = await RequireProfileAsync();
        if (profile == null) return RedirectToAction("Index", "Farmer");

        var product = await Db.Products.FirstOrDefaultAsync(p => p.Id == id && p.FarmerProfileId == profile.Id);
        if (product != null)
        {
            product.IsAvailable = !product.IsAvailable;
            await Db.SaveChangesAsync();
            TempData["Success"] = product.IsAvailable ? "Marked as available." : "Marked as unavailable.";
        }
        return RedirectToAction(nameof(Index));
    }

    // GET: /FarmerProducts/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var profile = await RequireProfileAsync();
        if (profile == null) return RedirectToAction("Index", "Farmer");

        var product = await Db.Products.Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id && p.FarmerProfileId == profile.Id);
        if (product == null) return NotFound();

        ViewBag.HasOrders = await Db.OrderItems.AnyAsync(oi => oi.ProductId == id);
        return View(product);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var profile = await RequireProfileAsync();
        if (profile == null) return RedirectToAction("Index", "Farmer");

        var product = await Db.Products.FirstOrDefaultAsync(p => p.Id == id && p.FarmerProfileId == profile.Id);
        if (product == null) return NotFound();

        // Preserve order history: if the product was ever ordered, retire it
        // instead of deleting so past orders still make sense.
        if (await Db.OrderItems.AnyAsync(oi => oi.ProductId == id))
        {
            product.IsAvailable = false;
            product.StockQuantity = 0;
            await Db.SaveChangesAsync();
            TempData["Success"] = "Product retired (it was part of past orders, so it stays in history).";
            return RedirectToAction(nameof(Index));
        }

        // Otherwise remove it along with its favorites and reviews.
        var favorites = await Db.Favorites.Where(f => f.ProductId == id).ToListAsync();
        var reviews = await Db.Reviews.Where(r => r.ProductId == id).ToListAsync();
        Db.Favorites.RemoveRange(favorites);
        Db.Reviews.RemoveRange(reviews);
        Db.Products.Remove(product);
        await Db.SaveChangesAsync();

        TempData["Success"] = "Product deleted.";
        return RedirectToAction(nameof(Index));
    }
}

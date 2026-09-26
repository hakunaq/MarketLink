using System.Text;
using MarketLink.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Services;

public interface IAiAssistantService
{
    /// <summary>Answers a customer question and returns a plain-text reply.</summary>
    Task<string> GetReplyAsync(string userMessage);
}

/// <summary>
/// A lightweight, offline "AI" assistant. It understands a set of common
/// intents (greetings, market timings, farmer availability, product search,
/// ordering and payment help) and answers using live data from the database.
/// Being rule-based means it works without any external API key, which keeps
/// the project self-contained for the competition.
/// </summary>
public class AiAssistantService : IAiAssistantService
{
    private readonly MarketLinkDbContext _db;

    public AiAssistantService(MarketLinkDbContext db)
    {
        _db = db;
    }

    public async Task<string> GetReplyAsync(string userMessage)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
            return "Hi! Ask me about products, markets, timings or how pre-orders work.";

        var msg = userMessage.Trim().ToLowerInvariant();

        // 1. Greetings / small talk
        if (ContainsAny(msg, "hi", "hello", "hey", "good morning", "good evening"))
            return "Hello! I'm your MarketLink assistant. I can help you find produce, " +
                   "check market timings, see which farmers are selling, or explain how pre-orders work. " +
                   "What would you like to know?";

        if (ContainsAny(msg, "thank", "thanks", "cheers"))
            return "You're welcome! Happy shopping at your local market.";

        if (ContainsAny(msg, "bye", "goodbye", "see you"))
            return "Goodbye! Come back any time you need help finding fresh produce.";

        // 2. How ordering / payment / cancellation works
        if (ContainsAny(msg, "how do i order", "how to order", "place order", "pre-order", "preorder", "reserve"))
            return "To pre-order: add items to your cart, go to the cart, choose a pickup date and " +
                   "confirm. The farmer accepts your order and marks it ready for pickup. " +
                   "You can track status under My Orders.";

        if (ContainsAny(msg, "payment", "pay", "card", "cash", "price at pickup"))
            return "MarketLink has no online payment. You pay the farmer in person when you collect " +
                   "your order at the market.";

        if (ContainsAny(msg, "cancel", "modify", "change order", "cutoff", "cut-off"))
            return "You can cancel or modify an order any time before the farmer's cut-off time, " +
                   "as long as the order is still 'Placed' or 'Accepted'. Open the order under My Orders.";

        if (ContainsAny(msg, "pickup", "collect", "where do i collect", "delivery"))
            return "All orders are collected in person at the market you choose during checkout. " +
                   "There is no delivery. Each farmer shows their pickup window on their profile.";

        // 3. Market timings / when markets are open
        if (ContainsAny(msg, "timing", "timings", "when", "open", "hours", "close", "schedule"))
        {
            var markets = await _db.Markets.Where(m => m.IsActive).OrderBy(m => m.Name).ToListAsync();
            if (markets.Count == 0)
                return "There are no markets listed yet.";

            var sb = new StringBuilder("Here are the market timings:\n");
            foreach (var m in markets)
                sb.AppendLine($"• {m.Name} — {m.OperatingDays}, {m.OpenTime}–{m.CloseTime} ({m.Address})");
            return sb.ToString().TrimEnd();
        }

        // 4. Which farmers sell at a market / farmer availability
        if (ContainsAny(msg, "farmer", "farmers", "vendor", "stall", "who sells", "availability"))
        {
            // A specific market may be named in the message.
            var namedMarket = await _db.Markets
                .FirstOrDefaultAsync(m => msg.Contains(m.Name.ToLowerInvariant()));

            var query = _db.FarmerMarkets
                .Include(fm => fm.FarmerProfile)
                .Include(fm => fm.Market)
                .Where(fm => fm.FarmerProfile!.IsApproved && !fm.FarmerProfile.IsSuspended);

            if (namedMarket != null)
                query = query.Where(fm => fm.MarketId == namedMarket.Id);

            var rows = await query.OrderBy(fm => fm.Market!.Name).ToListAsync();
            if (rows.Count == 0)
                return "I couldn't find any farmer schedules matching that.";

            var sb = new StringBuilder(namedMarket != null
                ? $"Farmers at {namedMarket.Name}:\n"
                : "Here's who sells where:\n");
            foreach (var r in rows)
                sb.AppendLine($"• {r.FarmerProfile!.StallName} at {r.Market!.Name} on {r.OperatingDay} " +
                              $"({r.PickupStartTime}–{r.PickupEndTime}{(string.IsNullOrEmpty(r.StallNumber) ? "" : ", " + r.StallNumber)})");
            return sb.ToString().TrimEnd();
        }

        // 5. Product search — "find tomatoes", "do you have apples", "where can i buy milk"
        var products = await _db.Products
            .Include(p => p.Farmer)
            .Include(p => p.Category)
            .Where(p => p.IsAvailable && p.StockQuantity > 0)
            .ToListAsync();

        // Break the question into meaningful keywords so "find tomatoes" matches
        // the product "Heirloom Tomatoes" (the name contains the keyword).
        var keywords = msg
            .Split(new[] { ' ', '?', '!', ',', '.', '\'' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length >= 3 && !StopWords.Contains(w))
            .Distinct()
            .ToList();

        var matches = products
            .Where(p =>
                msg.Contains(p.Name.ToLowerInvariant()) ||
                keywords.Any(k => p.Name.ToLowerInvariant().Contains(k)) ||
                (p.Category != null && keywords.Any(k => p.Category.Name.ToLowerInvariant().Contains(k))))
            .ToList();

        if (matches.Count > 0)
        {
            var sb = new StringBuilder("I found these available for you:\n");
            foreach (var p in matches.Take(8))
                sb.AppendLine($"• {p.Name} — {p.Price:0.00} / {p.Unit} at {p.Farmer?.StallName} " +
                              $"({p.StockQuantity} {p.Unit} left)");
            sb.Append("Open a product to add it to your cart.");
            return sb.ToString();
        }

        // 6. Categories / what's available in general
        if (ContainsAny(msg, "what do you have", "categories", "browse", "products", "available", "sell"))
        {
            var cats = await _db.Categories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
            var catNames = string.Join(", ", cats.Select(c => c.Name));
            return $"You can browse these categories: {catNames}. " +
                   "Use the Products page to filter by category, market, day or price.";
        }

        // 7. Fallback
        return "I can help with: product search (e.g. \"find tomatoes\"), market timings, which farmers " +
               "are selling, how to place or cancel a pre-order, and payment/pickup questions. " +
               "What would you like to know?";
    }

    private static bool ContainsAny(string input, params string[] keywords)
        => keywords.Any(k => input.Contains(k));

    // Common words ignored when turning a question into product keywords.
    private static readonly HashSet<string> StopWords = new()
    {
        "the", "and", "for", "you", "your", "are", "can", "could", "would", "should",
        "what", "when", "where", "which", "who", "how", "why", "do", "does", "did",
        "have", "has", "had", "find", "get", "buy", "sell", "sells", "selling", "any",
        "some", "with", "from", "please", "there", "their", "they", "them", "this",
        "that", "these", "those", "about", "into", "onto", "near", "me", "my", "we",
        "our", "all", "show", "list", "looking", "want", "need", "available", "availability"
    };
}

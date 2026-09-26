# MarketLink — eGreen Basket

**Category:** End-to-End Web Solution
**Stack:** ASP.NET Core MVC (.NET 9) · C# · Entity Framework Core · SQL Server · Bootstrap 5 · OpenStreetMap/Leaflet

MarketLink connects local farmers-market **farmers** directly with **customers**. Farmers publish their weekly
stock, prices and market schedule; customers browse, search and filter produce, reserve items as pre-orders for
pickup at a market, track order status, save favourites, and leave reviews. An administrator manages users,
markets, categories, moderation and platform reports.

> Payment is **not** handled by the application — orders are settled in person at pickup. Delivery/courier
> logistics and farmer identity/licensing verification are intentionally out of scope (see *Assumptions*).

---

## 1. Problem Definition

Shoppers rarely know in advance which farmers will be at a market on a given day, what stock they have, or at
what price. Availability is communicated informally (chalkboards, flyers, word of mouth), so customers often
arrive to find items sold out or a farmer closed for the week. Farmers have no easy way to publicise weekly
inventory, take pre-orders ahead of market day, or build lasting relationships with regular customers.

MarketLink centralises this information on one platform: it reduces wasted trips, helps farmers plan harvest and
stock, and strengthens the connection between local producers and their community.

---

## 2. Features by Role

### Customer
- Register / log in with name, contact number, email and address.
- Browse markets and farmers by location and day; view farmer profiles (stall, operating days, weekly stock).
- View markets and farmer stalls on an embedded **OpenStreetMap** with markers and pickup points.
- Search and filter products by keyword, category, market, day and price range; sort results.
- Add products to a cart and place **pre-orders**, choosing a pickup market, date and time window per farmer.
- Track order status (Placed → Accepted → Ready for pickup → Completed), cancel or modify before the cut-off.
- View order history and **reorder** past purchases with one click.
- Save **favourite** farmers and products.
- Ask the built-in **AI assistant** about market timings, farmer availability, pickup windows and product search.
- Leave **reviews and ratings** on farmers and products after an order is completed.
- Receive **in-app notifications** (and logged email) for order confirmations and status changes.

### Farmer
- Register with stall/business name, contact person, phone, email and address (pending admin approval).
- Manage profile: markets served, operating days, pickup windows, map pin (latitude/longitude), bio.
- Add, edit, view, delete products (name, category, price, unit, quantity, description, image).
- Mark items sold out / temporarily unavailable.
- Manage incoming pre-orders: accept, decline, mark ready for pickup, mark completed; set cut-off times.
- Dashboard insights: total orders, pending orders, revenue (completed), active/sold-out products,
  best-selling products, and recent reviews.
- View and **respond to** customer reviews.

### Admin
- Dedicated dashboard: approved farmers, customers, markets, orders, products, revenue, pending approvals.
- Approve / suspend / reinstate farmers; activate / deactivate customers.
- Manage markets (create, edit, soft-delete) with address, operating days, timings and coordinates.
- Content moderation: hide reviews, remove inappropriate product listings.
- System configuration: manage product categories, publish platform announcements.
- Reports & analytics: all orders, revenue by market, most active farmers (with generated-report history).

### Cross-cutting
- **Role-based access control** (Admin / Farmer / Customer) via ASP.NET Core Identity.
- Responsive, mobile-friendly UI (Bootstrap 5) with a green "farm fresh" theme.
- Search, sort and filter across markets, farmers and products.
- About Us and Contact Us pages (Contact Us shows the team location on a map).

---

## 3. Technology Stack

| Layer | Technology |
|-------|-----------|
| Framework | ASP.NET Core MVC, .NET 9 (`net9.0`) |
| Language | C# (nullable reference types enabled) |
| ORM | Entity Framework Core 9.0 (Code First + Migrations) |
| Database | Microsoft SQL Server (developed against **LocalDB / MSSQLLocalDB**) |
| Auth | ASP.NET Core Identity (roles, password policy, lockout) |
| Front-end | Razor Views, Bootstrap 5, Bootstrap Icons, jQuery, vanilla JS |
| Maps | OpenStreetMap tiles + Leaflet.js 1.9.4 (no API key required) |
| AI assistant | Rule-based offline assistant (keyword/intent matching against the database) |
| Session/cart | Server session (in-memory distributed cache) |

---

## 4. Project Structure

```
MarketLink.sln
Database/
  MarketLink_Database_Schema.sql     -- full CREATE DATABASE + table/FK/index definitions
MarketLink/
  Program.cs                         -- app startup, DI, Identity, session, migrations + seed
  appsettings.json                   -- connection string
  Models/                            -- 17 domain entities + enums (OrderStatus, Roles)
    ViewModels/                      -- view-specific models (account, catalog, dashboards)
  Data/
    MarketLinkDbContext.cs           -- EF Core context, relationships, delete behaviour
    DbSeeder.cs                      -- roles, demo users, markets, farmers, products, categories
  Migrations/                        -- EF Core migrations (InitialCreate)
  Services/                          -- Cart, Notification, Email (logging), AI Assistant
  Controllers/                       -- 16 controllers (see below)
  Views/                             -- 58 Razor views + shared partials/layout
  wwwroot/
    css/site.css  js/site.js         -- theme, map helper, notification badge, AI widget
    img/products/                    -- 15 product placeholder images (SVG)
    lib/                             -- Bootstrap, jQuery, validation
```

**Controllers:** Account, Home, Markets, Farmers, Products, Cart, Orders, Customer, Reviews, Notifications,
Ai, Farmer (dashboard/profile/markets/reviews), FarmerProducts, FarmerOrders, Admin, and a shared
`AppControllerBase`.

**Main entities:** ApplicationUser, FarmerProfile, Market, FarmerMarket (join), Category, Product, Order,
OrderItem, Review, Favorite, Notification, Announcement, PlatformReport, CartItem (session-only).

---

## 5. Database Design (overview)

Relationships (no source code — see `Database/MarketLink_Database_Schema.sql` for exact DDL):

- **ApplicationUser (1) — (0..1) FarmerProfile**: a farmer login has one stall profile.
- **FarmerProfile (1) — (*) Product**: a farmer lists many products.
- **Category (1) — (*) Product**.
- **Market (*) — (*) FarmerProfile** via **FarmerMarket**, which stores the operating day, pickup window
  (start/end) and stall number for each farmer at each market.
- **ApplicationUser (customer) (1) — (*) Order**; **Order (1) — (*) OrderItem**; **OrderItem (*) — (1) Product**.
  An order belongs to one farmer and one market (pickup location).
- **Review** links a customer to a product and/or farmer (and optionally the originating order), with rating,
  comment and an optional farmer reply.
- **Favorite** links a customer to a product and/or farmer.
- **Notification** links to a user; **Announcement** and **PlatformReport** are platform-wide.
- Order **Status** is stored as a readable string (Placed, Accepted, ReadyForPickup, Completed, Cancelled,
  Declined). Indexes exist on product name, order status and the favourite composite key.

Delete behaviour uses `Restrict` on several relationships to avoid SQL Server "multiple cascade paths";
products that already have order history are soft-retired rather than hard-deleted.

---

## 6. Installation Instructions (MANDATORY)

### Prerequisites
1. **Windows 10/11** (developed on Windows 10).
2. **Visual Studio 2022** (17.12 or later) with the *ASP.NET and web development* workload, **or** the
   standalone **.NET 9 SDK**.
3. **SQL Server LocalDB** — installed by default with the Visual Studio *Data storage and processing*
   component. (A full SQL Server instance also works; just change the connection string in step 4.)
4. A modern browser (Chrome, Edge, Firefox).

### Option A — Run from Visual Studio
1. Open **`MarketLink.sln`** in Visual Studio 2022.
2. Wait for NuGet to restore packages (automatic). If prompted, allow the restore.
3. Set the startup profile to **`http`** (toolbar dropdown) — this uses `http://localhost:5168`.
4. Press **F5** (or Ctrl+F5 to run without debugging).
5. On first launch the app **automatically creates the database, applies migrations and seeds demo data**.
   The browser opens to the home page.

### Option B — Run from the command line
```
cd MarketLink
dotnet restore
dotnet run
```
Then open the URL printed in the console (default **http://localhost:5168**).

### Connection string
`appsettings.json` → `ConnectionStrings:DefaultConnection` defaults to LocalDB:
```
Server=(localdb)\MSSQLLocalDB;Database=MarketLinkDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True
```
To use a different SQL Server, edit this string only. The database is created and seeded automatically on
first run — no manual SQL step is required.

### (Optional) Create the schema manually
The application manages the database itself. If you prefer to create the schema by hand (e.g. in SSMS or on a
server without the app), run **`Database/MarketLink_Database_Schema.sql`**. Note that this script creates the
tables only; the demo data is inserted by the application's seeder on first launch.

### First launch — what happens
- Roles `Admin`, `Farmer`, `Customer` are created.
- Demo users, 3 markets, 3 farmers (approved), 7 categories, 14 products and 1 announcement are seeded.
- Seeding is idempotent — it will not duplicate data on subsequent runs.

### Troubleshooting
- **"A network-related error... / LocalDB not found"** → In Visual Studio Installer, ensure *SQL Server
  Express LocalDB* is installed, or run `sqllocaldb start MSSQLLocalDB`. Then retry.
- **Port 5168 already in use** → change the `applicationUrl` in `Properties/launchSettings.json`, or run
  `dotnet run --urls http://localhost:5200`.
- **Database already exists with an older schema** → delete the `MarketLinkDb` database (SSMS or
  `sqllocaldb`), then run again to recreate and reseed.

---

## 7. Default User Credentials (MANDATORY)

All demo accounts are created automatically on first launch.

| Role | Email | Password | Name / Stall |
|------|-------|----------|--------------|
| **Admin** | `admin@marketlink.com` | `Admin@123` | Platform administrator |
| **Farmer** | `green@marketlink.com` | `Farmer@123` | Green Acres Farm — Asha Green |
| **Farmer** | `orchard@marketlink.com` | `Farmer@123` | Sunrise Orchards — Ravi Kumar |
| **Farmer** | `dairy@marketlink.com` | `Farmer@123` | Meadow Dairy Co. — Meera Nair |
| **Customer** | `customer@marketlink.com` | `Customer@123` | Sam Rivera |
| **Customer** | `priya@marketlink.com` | `Customer@123` | Priya Shah |

New farmer registrations start **unapproved** and must be approved by the admin (Admin → Farmers) before the
farmer can list products. The three seeded farmers are already approved so they can be used immediately.

Password policy: minimum 6 characters, with at least one uppercase letter, one lowercase letter and one digit.
Accounts lock for 10 minutes after 5 failed sign-in attempts.

---

## 8. Test Data

Seeded on first launch (also serves as the project's test data):
- **3 markets** — Riverside Saturday Market, Town Square Farmers Market, Hilltop Community Market
  (with real latitude/longitude around Bengaluru for map display).
- **3 farmers** with market schedules and pickup windows.
- **7 categories** — Vegetables, Fruits, Dairy, Baked Goods, Herbs, Eggs & Poultry, Honey & Preserves.
- **14 products** across the farmers (e.g. Heirloom Tomatoes, Honeycrisp Apples, Strawberries, Farmhouse
  Cheddar, Free-Range Eggs, Sourdough Loaf). One product (Zucchini) is seeded **sold out** (quantity 0) to
  demonstrate the sold-out state.
- **1 announcement** shown on the customer dashboard.

A typical end-to-end test: log in as a customer → add a product to the cart → checkout and pick a pickup
window → place the pre-order → log in as that farmer → accept → mark ready → mark completed → confirm revenue
and best-seller stats update → log in as a customer → leave a review.

---

## 9. Maps and AI Assistant

- **Maps** use OpenStreetMap tiles with Leaflet.js, so they work out of the box with **no API key**. Markets and
  farmer stalls appear as markers on the Markets → Map page and on farmer/market detail pages. Latitude and
  longitude are stored per market and per farmer profile. (The design also allows switching to Google Maps by
  changing the `MapProvider` value and supplying an API key.)
- **AI assistant** is a lightweight, offline, rule-based chatbot (the floating button at the bottom-right).
  It answers questions about market timings, farmer availability, pickup windows, payment/cancellation policy,
  and performs product/category search by matching keywords against the live database. No external API or key
  is required.

---

## 10. Assumptions

1. **No payment gateway** — payment for pre-orders is settled in person at pickup, per the SRS.
2. **Pickup only** — no delivery or courier logistics.
3. **No verification** of farmer identity, licensing, or organic/food-safety certification.
4. **LocalDB** is used as the SQL Server engine for portability; any SQL Server works via the connection string.
5. **Email** is simulated: `LoggingEmailSender` writes messages to the application log instead of using SMTP,
   so no mail server configuration is needed for the demo. In-app notifications are fully functional.
6. **Product images** are simple generated SVG placeholders stored under `wwwroot/img/products`; farmers can
   supply an image URL on the product form.
7. **Market operating days** and pickup windows are stored as simple strings (day names and `HH:mm` times),
   which keeps the model easy to read and edit.
8. The **AI assistant** is intentionally rule-based and offline to avoid external dependencies during judging;
   it is designed to be easily replaced with a hosted LLM/API later.
9. A "weekly stock template" is represented by the farmer's product list with quantities that can be adjusted
   each week; recurring automation is left as a future enhancement.
10. Coordinates for demo markets are approximate real locations for map demonstration only.

---

## 11. AI Tools Acknowledgement

In line with the project guidelines, AI tools were used **as a supporting aid**, not as a substitute for design
and implementation. Assistance was used for:
- scaffolding boilerplate and iterating on EF Core relationships/migrations,
- debugging (e.g. resolving SQL Server "multiple cascade paths" and null-reference issues),
- generating placeholder product artwork, and
- general productivity while writing C#, Razor and CSS.

All architecture decisions, database design, business logic (cart, per-farmer order splitting, order lifecycle,
role-based access, the rule-based AI assistant), and the UI/UX were designed, implemented, reviewed and tested
by the developer, and can be explained and justified on request. No ready-made website template was used.

---

## 12. Notes

- This documentation intentionally **contains no source code** (aside from the separate `.sql` schema file and
  configuration snippets needed to run the app), as required by the SRS.
- The application targets the latest browsers and is responsive across desktop, tablet and mobile widths.
- Build status at time of writing: compiles with **0 warnings / 0 errors**; core customer, farmer and admin
  flows verified end-to-end in the browser.

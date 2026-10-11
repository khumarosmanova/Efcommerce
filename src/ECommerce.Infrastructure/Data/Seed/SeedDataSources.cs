using ECommerce.Domain.Constants;
using ECommerce.Domain.Enums;

namespace ECommerce.Infrastructure.Data.Seed;

public static class SeedDataSources
{
    public record CategorySeedModel(
        string Name,
        string Slug,
        string Description,
        string ImageUrl,
        string? ParentSlug = null
    );

    public record ProductSeedModel(
        string Name,
        string Slug,
        string Description,
        decimal Price,
        decimal? DiscountPrice,
        int StockQuantity,
        string SKU,
        string ImageUrl,
        string CategorySlug
    );

    public record OrderItemSeedModel(
        string ProductSku,
        string ProductName,
        decimal UnitPrice,
        int Quantity
    );

    public record OrderSeedModel(
        string OrderNumber,
        OrderStatus Status,
        int DaysAgo,
        string ShippingAddress,
        List<OrderItemSeedModel> Items
    );

    public static readonly Dictionary<string, List<string>> RolePermissions = new()
    {
        ["SuperAdmin"] = Permissions.GetAllPermissions().ToList(),
        ["Admin"] = new List<string>
        {
            Permissions.Product.View,
            Permissions.Product.Manage,
            Permissions.Category.Manage,
            Permissions.Order.Manage,
            Permissions.Statistics.View,
            Permissions.Coupon.Manage,
            Permissions.Trash.Manage
        },
        ["Moderator"] = new List<string>
        {
            Permissions.Product.View,
            Permissions.Product.Manage,
            Permissions.Category.Manage
        },
        ["Customer"] = new List<string>()
    };

    public static readonly List<CategorySeedModel> Categories = new()
    {
        // 1. Electronics (Parent)
        new("Electronics", "electronics", "Latest gadgets, computers, audio equipment, and smart devices.", "https://images.unsplash.com/photo-1498049794561-7780e7231661?w=800"),
        // Subcategories of Electronics
        new("Smartphones", "smartphones", "Flagship smartphones, budget devices, and mobile accessories.", "https://images.unsplash.com/photo-1511707171634-5f897ff02544?w=800", "electronics"),
        new("Laptops", "laptops", "High-performance laptops, ultrabooks, and workstation notebooks.", "https://images.unsplash.com/photo-1496181133206-80ce9b88a853?w=800", "electronics"),

        // 2. Clothing
        new("Clothing", "clothing", "Trendy apparel, timeless essentials, and everyday fashion.", "https://images.unsplash.com/photo-1489987707025-afc232f7ea0f?w=800"),

        // 3. Home & Garden
        new("Home & Garden", "home-garden", "Furniture, kitchenware, decor, and outdoor gardening essentials.", "https://images.unsplash.com/photo-1513694203232-719a280e022f?w=800"),

        // 4. Sports & Outdoors
        new("Sports", "sports", "Fitness gear, activewear, outdoor camping equipment, and sports accessories.", "https://images.unsplash.com/photo-1517649763962-0c623266ddc0?w=800"),

        // 5. Books
        new("Books", "books", "Bestselling novels, educational books, software design, and non-fiction.", "https://images.unsplash.com/photo-1495446815901-a7297e633e8d?w=800"),

        // 6. Beauty
        new("Beauty", "beauty", "Skincare, haircare, luxury cosmetics, and daily wellness items.", "https://images.unsplash.com/photo-1522337360788-8b13dee7a37e?w=800")
    };

    public static readonly List<ProductSeedModel> Products = new()
    {
        // --- Category: Smartphones (5 items) ---
        new("iPhone 15 Pro Max", "iphone-15-pro-max", "Titanium design with A17 Pro chip and 48MP main camera.", 1199.99m, 1149.99m, 45, "ELEC-SP-001", "https://images.unsplash.com/photo-1695048133142-1a20484d2569?w=800", "smartphones"),
        new("Samsung Galaxy S24 Ultra", "samsung-galaxy-s24-ultra", "AI-powered flagship with S-Pen, 200MP camera, and Snapdragon 8 Gen 3.", 1299.99m, 1199.99m, 30, "ELEC-SP-002", "https://images.unsplash.com/photo-1610945415295-d9bbf067e59c?w=800", "smartphones"),
        new("Google Pixel 8 Pro", "google-pixel-8-pro", "Google Tensor G3 chip, advanced computational photography, and OLED display.", 999.00m, 899.00m, 25, "ELEC-SP-003", "https://images.unsplash.com/photo-1598327105666-5b89351aff97?w=800", "smartphones"),
        new("OnePlus 12", "oneplus-12", "Fast 100W SuperVOOC charging, Hasselblad camera, and 120Hz ProXDR display.", 799.99m, 749.99m, 50, "ELEC-SP-004", "https://images.unsplash.com/photo-1580910051074-3eb694886505?w=800", "smartphones"),
        new("Xiaomi 14 Ultra", "xiaomi-14-ultra", "Leica quad-camera system, titanium frame, and bright AMOLED screen.", 1099.00m, null, 20, "ELEC-SP-005", "https://images.unsplash.com/photo-1567581935884-3349723552ca?w=800", "smartphones"),

        // --- Category: Laptops (5 items) ---
        new("MacBook Pro 16\" M3 Max", "macbook-pro-16-m3-max", "Apple M3 Max chip with 36GB unified memory and 1TB SSD storage.", 3499.00m, 3299.00m, 18, "ELEC-LT-001", "https://images.unsplash.com/photo-1517336714731-489689fd1ca8?w=800", "laptops"),
        new("Dell XPS 15 OLED", "dell-xps-15-oled", "Intel Core i7 13th Gen, 3.5K OLED touchscreen, and NVIDIA RTX 4060 graphics.", 1999.99m, 1849.99m, 22, "ELEC-LT-002", "https://images.unsplash.com/photo-1593642632823-8f785ba67e45?w=800", "laptops"),
        new("ThinkPad X1 Carbon Gen 11", "thinkpad-x1-carbon-gen11", "Ultra-lightweight business laptop with legendary keyboard and 14-inch IPS display.", 1749.00m, 1599.00m, 35, "ELEC-LT-003", "https://images.unsplash.com/photo-1588872657578-7efd1f1555ed?w=800", "laptops"),
        new("ASUS ROG Zephyrus G16", "asus-rog-zephyrus-g16", "Gaming beast with OLED 240Hz screen, Intel Core Ultra 9, and RTX 4080.", 2399.99m, 2249.99m, 15, "ELEC-LT-004", "https://images.unsplash.com/photo-1603302576837-37561b2e2302?w=800", "laptops"),
        new("HP Spectre x360 2-in-1", "hp-spectre-x360-14", "Convertible 2-in-1 touchscreen laptop with rechargeable MPP 2.0 tilt pen.", 1499.99m, null, 28, "ELEC-LT-005", "https://images.unsplash.com/photo-1541807084-5c52b6b3adef?w=800", "laptops"),

        // --- Category: Electronics General / Audio (5 items) ---
        new("Sony WH-1000XM5 Wireless Headphones", "sony-wh-1000xm5", "Industry-leading noise canceling headphones with 30-hour battery life.", 399.99m, 348.00m, 60, "ELEC-AU-001", "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=800", "electronics"),
        new("Apple Watch Ultra 2", "apple-watch-ultra-2", "Rugged 49mm titanium case with precision dual-frequency GPS.", 799.00m, null, 40, "ELEC-AU-002", "https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=800", "electronics"),
        new("Bose QuietComfort Ultra Earbuds", "bose-qc-ultra-earbuds", "Immersive audio experience with spatial sound and custom noise cancellation.", 299.00m, 269.00m, 75, "ELEC-AU-003", "https://images.unsplash.com/photo-1590658268037-6bf12165a8df?w=800", "electronics"),
        new("Sonos Era 300 Smart Speaker", "sonos-era-300", "Spatial audio smart speaker with Dolby Atmos and multi-room syncing.", 449.00m, null, 32, "ELEC-AU-004", "https://images.unsplash.com/photo-1545454675-3531b543be5d?w=800", "electronics"),
        new("Logitech MX Master 3S Mouse", "logitech-mx-master-3s", "Quiet clicks, 8K DPI tracking on glass, and ergonomic sculpted silhouette.", 99.99m, 89.99m, 110, "ELEC-AU-005", "https://images.unsplash.com/photo-1615663245857-ac93bb7c39e7?w=800", "electronics"),

        // --- Category: Clothing (7 items) ---
        new("Premium Organic Cotton T-Shirt", "premium-organic-cotton-tshirt", "100% certified organic combed ring-spun cotton with breathable classic fit.", 34.99m, 29.99m, 150, "CLOTH-001", "https://images.unsplash.com/photo-1521572267360-ee0c2909d518?w=800", "clothing"),
        new("Slim Fit Stretch Denim Jeans", "slim-fit-stretch-denim-jeans", "Classic 5-pocket styling with flexible stretch comfort denim.", 79.99m, 64.99m, 95, "CLOTH-002", "https://images.unsplash.com/photo-1542272604-780c96856592?w=800", "clothing"),
        new("Waterproof Alpine Winter Parka", "waterproof-alpine-winter-parka", "Thermal insulated goose down parka with seam-sealed breathable shell.", 289.00m, 249.00m, 40, "CLOTH-003", "https://images.unsplash.com/photo-1539533018447-63fcce667883?w=800", "clothing"),
        new("Merino Wool Crewneck Sweater", "merino-wool-crewneck-sweater", "Superfine 100% Italian merino wool offering natural odor resistance.", 119.50m, 99.00m, 60, "CLOTH-004", "https://images.unsplash.com/photo-1620799140408-edc6dcb6d633?w=800", "clothing"),
        new("Performance Running Shorts", "performance-running-shorts", "Lightweight four-way stretch fabric with moisture-wicking liner.", 45.00m, null, 120, "CLOTH-005", "https://images.unsplash.com/photo-1591195853828-11db59a44f6b?w=800", "clothing"),
        new("Linen Long Sleeve Casual Shirt", "linen-long-sleeve-casual-shirt", "Breathable natural flax linen shirt for relaxed warm-weather elegance.", 59.99m, 49.99m, 80, "CLOTH-006", "https://images.unsplash.com/photo-1596755094514-f87e34085b2c?w=800", "clothing"),
        new("Seamless High-Waist Yoga Leggings", "seamless-high-waist-yoga-leggings", "Buttery soft compression fabric with squat-proof 4-way flexibility.", 68.00m, 54.00m, 140, "CLOTH-007", "https://images.unsplash.com/photo-1506619216599-9d16d0903dfd?w=800", "clothing"),

        // --- Category: Home & Garden (6 items) ---
        new("Ergonomic High-Back Office Chair", "ergonomic-high-back-office-chair", "Adjustable 4D armrests, dynamic lumbar support, and breathable mesh back.", 349.99m, 299.99m, 35, "HOME-001", "https://images.unsplash.com/photo-1580481077197-3f30b91d295b?w=800", "home-garden"),
        new("Cast Iron Dutch Oven 6-Quart", "cast-iron-dutch-oven-6qt", "Enamelled cast iron for superior heat distribution and slow cooking.", 89.95m, 74.95m, 70, "HOME-002", "https://images.unsplash.com/photo-1584990347449-34b7f57a3e74?w=800", "home-garden"),
        new("Minimalist Ceramic Planter Set (3-Pack)", "minimalist-ceramic-planter-set", "Matte glaze planters with built-in drainage holes and bamboo saucers.", 42.00m, 35.00m, 85, "HOME-003", "https://images.unsplash.com/photo-1485955900006-10f4d324d411?w=800", "home-garden"),
        new("Smart Ambient LED Floor Lamp", "smart-ambient-led-floor-lamp", "RGBIC lighting with 16 million colors and Alexa / Google voice sync.", 129.99m, 109.99m, 50, "HOME-004", "https://images.unsplash.com/photo-1507473885765-e6ed057f782c?w=800", "home-garden"),
        new("Cordless 40V Electric Lawn Mower", "cordless-40v-electric-lawn-mower", "Brushless motor with 20-inch cutting deck and dual battery runtime.", 429.00m, 389.00m, 20, "HOME-005", "https://images.unsplash.com/photo-1592417817098-8f3d6910985b?w=800", "home-garden"),
        new("Luxury Bamboo Cooling Bed Sheets (Queen)", "luxury-bamboo-cooling-sheets-queen", "100% organic viscose bamboo with silky soft 400-thread count feel.", 110.00m, 89.99m, 65, "HOME-006", "https://images.unsplash.com/photo-1631679706909-1844bbd07221?w=800", "home-garden"),

        // --- Category: Sports (6 items) ---
        new("Full-Suspension Mountain Bike 29\"", "full-suspension-mountain-bike-29", "Lightweight hydroformed aluminum frame with 12-speed Shimano drivetrain.", 1499.00m, 1349.00m, 12, "SPORT-001", "https://images.unsplash.com/photo-1485965120184-e220f721d03e?w=800", "sports"),
        new("Adjustable Quick-Select Dumbbell Pair (50 lbs)", "adjustable-dumbbell-pair-50lbs", "Space-saving dumbbell system adjustable from 5 to 50 lbs with click dial.", 379.99m, 329.99m, 40, "SPORT-002", "https://images.unsplash.com/photo-1584735935682-2f2b69dff9d2?w=800", "sports"),
        new("Ultralight 2-Person Backpacking Tent", "ultralight-2p-backpacking-tent", "Waterproof silicone-coated ripstop nylon weighing just 3.2 lbs.", 219.00m, 189.00m, 55, "SPORT-003", "https://images.unsplash.com/photo-1504280390367-361c6d9f38f4?w=800", "sports"),
        new("Carbon Fiber Pro Tennis Racket", "carbon-fiber-pro-tennis-racket", "Precision-engineered frame for tournament level spin and control.", 195.00m, 169.00m, 45, "SPORT-004", "https://images.unsplash.com/photo-1617083934555-563d6b052fd8?w=800", "sports"),
        new("Insulated Stainless Steel Water Bottle 32oz", "insulated-water-bottle-32oz", "Double-wall vacuum insulation keeps drinks cold for 24 hours.", 38.00m, 29.99m, 200, "SPORT-005", "https://images.unsplash.com/photo-1602143407151-7111542de6e8?w=800", "sports"),
        new("High-Density Eco Yoga Mat", "high-density-eco-yoga-mat", "Non-slip textured surface made with biodegradable natural tree rubber.", 55.00m, 45.00m, 110, "SPORT-006", "https://images.unsplash.com/photo-1601925260368-ae2f83cf8b7f?w=800", "sports"),

        // --- Category: Books (6 items) ---
        new("Clean Architecture: A Craftsman's Guide", "clean-architecture-guide", "Essential software structures, principles, and component architecture patterns.", 42.99m, 36.50m, 80, "BOOK-001", "https://images.unsplash.com/photo-1532012164546-f432f2e3777a?w=800", "books"),
        new("Designing Data-Intensive Applications", "designing-data-intensive-applications", "The definitive handbook on reliability, scalability, and distributed data systems.", 49.99m, 44.99m, 65, "BOOK-002", "https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?w=800", "books"),
        new("Atomic Habits by James Clear", "atomic-habits", "Proven framework for improving every day, breaking bad habits and mastering good ones.", 27.00m, 19.99m, 180, "BOOK-003", "https://images.unsplash.com/photo-1512820790803-83ca734da794?w=800", "books"),
        new("Domain-Driven Design: Tackling Complexity", "domain-driven-design-evans", "The foundational work on ubiquitous language, aggregates, and domain modeling.", 54.99m, 48.00m, 50, "BOOK-004", "https://images.unsplash.com/photo-1524995997946-a1c2e315a42f?w=800", "books"),
        new("The Pragmatic Programmer (20th Anniversary)", "the-pragmatic-programmer-20th", "Timeless advice on code craftsmanship, career growth, and debugging.", 45.00m, 38.00m, 90, "BOOK-005", "https://images.unsplash.com/photo-1497633762265-9d179a990aa6?w=800", "books"),
        new("Deep Work: Rules for Focused Success", "deep-work-cal-newport", "Techniques for eliminating distractions and achieving peak cognitive performance.", 28.00m, null, 100, "BOOK-006", "https://images.unsplash.com/photo-1456513080510-7bf3a84b82f8?w=800", "books"),

        // --- Category: Beauty (6 items) ---
        new("Pure Hyaluronic Acid Hydrating Serum", "pure-hyaluronic-acid-serum", "Deep multi-depth hydration for plumper, softer, and rejuvenated skin.", 32.00m, 26.00m, 130, "BEAUTY-001", "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?w=800", "beauty"),
        new("Vitamin C Brightening Facial Cleanser", "vitamin-c-facial-cleanser", "Gentle foaming antioxidant wash that clears pores and evens tone.", 24.50m, 19.99m, 115, "BEAUTY-002", "https://images.unsplash.com/photo-1556228720-195a672e8a03?w=800", "beauty"),
        new("Mineral Sunscreen SPF 50+ Invisible", "mineral-sunscreen-spf-50", "Broad spectrum zinc oxide formula that leaves no white cast or greasy finish.", 28.00m, null, 160, "BEAUTY-003", "https://images.unsplash.com/photo-1598440947619-2c35fc9aa908?w=800", "beauty"),
        new("Ionic Salon Hair Dryer Pro", "ionic-salon-hair-dryer-pro", "Fast-drying 1800W brushless motor with intelligent heat control and diffuser.", 149.99m, 129.99m, 45, "BEAUTY-004", "https://images.unsplash.com/photo-1522337360788-8b13dee7a37e?w=800", "beauty"),
        new("Cold-Pressed Moroccan Argan Hair Oil", "moroccan-argan-hair-oil", "100% pure organic oil nourishing dry hair, split ends, and frizzy strands.", 22.00m, 17.50m, 140, "BEAUTY-005", "https://images.unsplash.com/photo-1608248597359-548482431d17?w=800", "beauty"),
        new("Sonic Rechargeable Electric Toothbrush", "sonic-electric-toothbrush", "40,000 vibrations per minute with 5 brushing modes and travel case.", 79.99m, 64.99m, 85, "BEAUTY-006", "https://images.unsplash.com/photo-1559591937-e105322fc751?w=800", "beauty")
    };

    public static readonly List<OrderSeedModel> Orders = new()
    {
        // Order 1: 150 days ago (Delivered)
        new("ORD-2026-0001", OrderStatus.Delivered, 150, "742 Evergreen Terrace, Springfield, OR 97477", new List<OrderItemSeedModel>
        {
            new("ELEC-AU-001", "Sony WH-1000XM5 Wireless Headphones", 348.00m, 1),
            new("BOOK-001", "Clean Architecture: A Craftsman's Guide", 36.50m, 1)
        }),

        // Order 2: 110 days ago (Delivered)
        new("ORD-2026-0002", OrderStatus.Delivered, 110, "742 Evergreen Terrace, Springfield, OR 97477", new List<OrderItemSeedModel>
        {
            new("CLOTH-001", "Premium Organic Cotton T-Shirt", 29.99m, 3),
            new("CLOTH-002", "Slim Fit Stretch Denim Jeans", 64.99m, 1)
        }),

        // Order 3: 80 days ago (Delivered)
        new("ORD-2026-0003", OrderStatus.Delivered, 80, "742 Evergreen Terrace, Springfield, OR 97477", new List<OrderItemSeedModel>
        {
            new("SPORT-005", "Insulated Stainless Steel Water Bottle 32oz", 29.99m, 2),
            new("SPORT-006", "High-Density Eco Yoga Mat", 45.00m, 1),
            new("BEAUTY-001", "Pure Hyaluronic Acid Hydrating Serum", 26.00m, 1)
        }),

        // Order 4: 50 days ago (Cancelled)
        new("ORD-2026-0004", OrderStatus.Cancelled, 50, "742 Evergreen Terrace, Springfield, OR 97477", new List<OrderItemSeedModel>
        {
            new("ELEC-LT-002", "Dell XPS 15 OLED", 1849.99m, 1)
        }),

        // Order 5: 25 days ago (Delivered)
        new("ORD-2026-0005", OrderStatus.Delivered, 25, "742 Evergreen Terrace, Springfield, OR 97477", new List<OrderItemSeedModel>
        {
            new("HOME-001", "Ergonomic High-Back Office Chair", 299.99m, 1),
            new("HOME-004", "Smart Ambient LED Floor Lamp", 109.99m, 1)
        }),

        // Order 6: 10 days ago (Shipped)
        new("ORD-2026-0006", OrderStatus.Shipped, 10, "742 Evergreen Terrace, Springfield, OR 97477", new List<OrderItemSeedModel>
        {
            new("ELEC-SP-001", "iPhone 15 Pro Max", 1149.99m, 1),
            new("ELEC-AU-005", "Logitech MX Master 3S Mouse", 89.99m, 1)
        }),

        // Order 7: 4 days ago (Confirmed)
        new("ORD-2026-0007", OrderStatus.Confirmed, 4, "742 Evergreen Terrace, Springfield, OR 97477", new List<OrderItemSeedModel>
        {
            new("BOOK-002", "Designing Data-Intensive Applications", 44.99m, 1),
            new("BOOK-003", "Atomic Habits by James Clear", 19.99m, 1)
        }),

        // Order 8: 1 day ago (Pending)
        new("ORD-2026-0008", OrderStatus.Pending, 1, "742 Evergreen Terrace, Springfield, OR 97477", new List<OrderItemSeedModel>
        {
            new("BEAUTY-002", "Vitamin C Brightening Facial Cleanser", 19.99m, 2),
            new("BEAUTY-004", "Ionic Salon Hair Dryer Pro", 129.99m, 1)
        })
    };
}

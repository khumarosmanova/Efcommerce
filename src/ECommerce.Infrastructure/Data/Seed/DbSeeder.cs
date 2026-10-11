using System.Security.Claims;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Data.Seed;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;

        var context = services.GetRequiredService<AppDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<AppRole>>();
        var userManager = services.GetRequiredService<UserManager<AppUser>>();
        var environment = services.GetService<IHostEnvironment>();
        var logger = services.GetService<ILoggerFactory>()?.CreateLogger("DbSeeder");

        try
        {
            await MigrateDatabaseAsync(context, environment, logger);
            await SeedRolesAndPermissionsAsync(roleManager, logger);
            await SeedUsersAsync(userManager, logger);
            await SeedCategoriesAsync(context, logger);
            await SeedProductsAsync(context, logger);
            await SeedOrdersAsync(context, userManager, logger);
            await SeedReviewsAsync(context, userManager, logger);
            await SeedCouponsAsync(context, logger);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    private static async Task MigrateDatabaseAsync(
        AppDbContext context,
        IHostEnvironment? environment,
        ILogger? logger)
    {
        if (environment != null && environment.IsDevelopment())
        {
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                logger?.LogInformation("Applying pending database migrations...");
                await context.Database.MigrateAsync();
                logger?.LogInformation("Database migrations applied successfully.");
            }
        }
    }

    private static async Task SeedRolesAndPermissionsAsync(
        RoleManager<AppRole> roleManager,
        ILogger? logger)
    {
        foreach (var (roleName, permissions) in SeedDataSources.RolePermissions)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role == null)
            {
                role = new AppRole(roleName, $"{roleName} role");
                var createResult = await roleManager.CreateAsync(role);
                if (!createResult.Succeeded)
                {
                    logger?.LogWarning("Failed to create role: {Role}", roleName);
                    continue;
                }
            }

            var existingClaims = await roleManager.GetClaimsAsync(role);
            var existingPermissions = existingClaims
                .Where(c => c.Type == "permission")
                .Select(c => c.Value)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var permission in permissions)
            {
                if (!existingPermissions.Contains(permission))
                {
                    await roleManager.AddClaimAsync(role, new Claim("permission", permission));
                }
            }
        }
    }

    private static async Task SeedUsersAsync(
        UserManager<AppUser> userManager,
        ILogger? logger)
    {
        var usersToSeed = new (string Email, string FirstName, string LastName, string Password, string Role)[]
        {
            ("superadmin@shop.com", "Super", "Admin", "Admin123!", "SuperAdmin"),
            ("admin@shop.com", "Store", "Admin", "Admin123!", "Admin"),
            ("customer@shop.com", "John", "Customer", "Customer123!", "Customer")
        };

        foreach (var (email, firstName, lastName, password, role) in usersToSeed)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new AppUser
                {
                    Id = Guid.NewGuid(),
                    UserName = email,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                };

                var createResult = await userManager.CreateAsync(user, password);
                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, role);
                    logger?.LogInformation("Seeded user: {Email} with role {Role}", email, role);
                }
                else
                {
                    logger?.LogWarning("Failed to seed user {Email}: {Errors}",
                        email, string.Join(", ", createResult.Errors.Select(e => e.Description)));
                }
            }
        }
    }

    private static async Task SeedCategoriesAsync(AppDbContext context, ILogger? logger)
    {
        if (await context.Categories.IgnoreQueryFilters().AnyAsync())
        {
            return;
        }

        logger?.LogInformation("Seeding categories...");

        // 1. Seed root categories first
        var rootModels = SeedDataSources.Categories.Where(c => c.ParentSlug == null).ToList();
        var categoryMap = new Dictionary<string, Category>();

        foreach (var model in rootModels)
        {
            var category = new Category
            {
                Name = model.Name,
                Slug = model.Slug,
                Description = model.Description,
                ImageUrl = model.ImageUrl,
                ParentCategoryId = null,
                CreatedAt = DateTime.UtcNow
            };
            context.Categories.Add(category);
            categoryMap[model.Slug] = category;
        }

        await context.SaveChangesAsync();

        // 2. Seed subcategories with ParentCategoryId resolved
        var subModels = SeedDataSources.Categories.Where(c => c.ParentSlug != null).ToList();
        foreach (var model in subModels)
        {
            if (categoryMap.TryGetValue(model.ParentSlug!, out var parentCategory))
            {
                var subCategory = new Category
                {
                    Name = model.Name,
                    Slug = model.Slug,
                    Description = model.Description,
                    ImageUrl = model.ImageUrl,
                    ParentCategoryId = parentCategory.Id,
                    CreatedAt = DateTime.UtcNow
                };
                context.Categories.Add(subCategory);
            }
        }

        await context.SaveChangesAsync();
        logger?.LogInformation("Categories seeded successfully.");
    }

    private static async Task SeedProductsAsync(AppDbContext context, ILogger? logger)
    {
        if (await context.Products.IgnoreQueryFilters().AnyAsync())
        {
            return;
        }

        logger?.LogInformation("Seeding products...");

        var categories = await context.Categories
            .IgnoreQueryFilters()
            .ToDictionaryAsync(c => c.Slug, c => c.Id);

        var productsToInsert = new List<Product>();

        foreach (var model in SeedDataSources.Products)
        {
            if (!categories.TryGetValue(model.CategorySlug, out var categoryId))
            {
                logger?.LogWarning("Category slug '{Slug}' not found for product '{Name}'",
                    model.CategorySlug, model.Name);
                continue;
            }

            productsToInsert.Add(new Product
            {
                Name = model.Name,
                Slug = model.Slug,
                Description = model.Description,
                Price = model.Price,
                DiscountPrice = model.DiscountPrice,
                StockQuantity = model.StockQuantity,
                SKU = model.SKU,
                ImageUrl = model.ImageUrl,
                CategoryId = categoryId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        await context.Products.AddRangeAsync(productsToInsert);
        await context.SaveChangesAsync();

        logger?.LogInformation("Seeded {Count} products successfully.", productsToInsert.Count);
    }

    private static async Task SeedOrdersAsync(
        AppDbContext context,
        UserManager<AppUser> userManager,
        ILogger? logger)
    {
        if (await context.Orders.AnyAsync())
        {
            return;
        }

        logger?.LogInformation("Seeding sample orders...");

        var customer = await userManager.FindByEmailAsync("customer@shop.com");
        if (customer == null)
        {
            logger?.LogWarning("Customer customer@shop.com not found for order seeding.");
            return;
        }

        var products = await context.Products
            .IgnoreQueryFilters()
            .ToDictionaryAsync(p => p.SKU, p => p);

        var ordersToInsert = new List<Order>();

        foreach (var orderModel in SeedDataSources.Orders)
        {
            var orderDate = DateTime.UtcNow.AddDays(-orderModel.DaysAgo);
            var order = new Order
            {
                OrderNumber = orderModel.OrderNumber,
                UserId = customer.Id.ToString(),
                Status = orderModel.Status,
                ShippingAddress = orderModel.ShippingAddress,
                CreatedAt = orderDate
            };

            decimal totalAmount = 0;

            foreach (var itemModel in orderModel.Items)
            {
                int productId = 0;
                if (products.TryGetValue(itemModel.ProductSku, out var product))
                {
                    productId = product.Id;
                }

                var orderItem = new OrderItem
                {
                    ProductId = productId,
                    ProductName = itemModel.ProductName,
                    UnitPrice = itemModel.UnitPrice,
                    Quantity = itemModel.Quantity,
                    CreatedAt = orderDate
                };

                order.OrderItems.Add(orderItem);
                totalAmount += itemModel.UnitPrice * itemModel.Quantity;
            }

            order.TotalAmount = totalAmount;
            ordersToInsert.Add(order);
        }

        await context.Orders.AddRangeAsync(ordersToInsert);
        await context.SaveChangesAsync();

        logger?.LogInformation("Seeded {Count} orders successfully.", ordersToInsert.Count);
    }

    private static async Task SeedReviewsAsync(
        AppDbContext context,
        UserManager<AppUser> userManager,
        ILogger? logger)
    {
        if (await context.Reviews.AnyAsync())
        {
            return;
        }

        var customer = await userManager.FindByEmailAsync("customer@shop.com");
        var customerId = customer?.Id ?? Guid.NewGuid();
        var customerName = customer != null ? $"{customer.FirstName} {customer.LastName}".Trim() : "Nijat Aliyev";
        var customerEmail = customer?.Email ?? "customer@shop.com";

        var products = await context.Products.Take(6).ToListAsync();
        var reviews = new List<Review>();

        foreach (var p in products)
        {
            reviews.Add(new Review
            {
                ProductId = p.Id,
                UserId = customerId,
                UserFullName = customerName,
                UserEmail = customerEmail,
                Rating = 5,
                Comment = "Əla keyfiyyət və sürətli çatdırılma! Hər kəsə tövsiyə edirəm.",
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            });
            reviews.Add(new Review
            {
                ProductId = p.Id,
                UserId = Guid.NewGuid(),
                UserFullName = "Aysel Məmmədova",
                UserEmail = "aysel.m@example.com",
                Rating = 4,
                Comment = "Məhsul tam təsvir olunduğu kimidir, qablaşdırma çox səliqəlidir.",
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            });
        }

        await context.Reviews.AddRangeAsync(reviews);
        await context.SaveChangesAsync();

        logger?.LogInformation("Seeded {Count} reviews successfully.", reviews.Count);
    }

    private static async Task SeedCouponsAsync(AppDbContext context, ILogger? logger)
    {
        if (await context.Coupons.AnyAsync()) return;

        var coupons = new List<Coupon>
        {
            new Coupon
            {
                Code = "WELCOME10",
                Description = "10% off for all registered customers",
                DiscountType = Domain.Enums.CouponDiscountType.Percentage,
                DiscountValue = 10,
                MinOrderAmount = 30,
                MaxDiscountAmount = 100,
                UsageLimit = 500,
                TimesUsed = 12,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                ExpiryDate = DateTime.UtcNow.AddMonths(3)
            },
            new Coupon
            {
                Code = "SUMMER20",
                Description = "$20 flat discount on orders over $100",
                DiscountType = Domain.Enums.CouponDiscountType.FixedAmount,
                DiscountValue = 20,
                MinOrderAmount = 100,
                UsageLimit = 200,
                TimesUsed = 25,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                ExpiryDate = DateTime.UtcNow.AddMonths(1)
            },
            new Coupon
            {
                Code = "VIP50",
                Description = "50% off VIP promo campaign",
                DiscountType = Domain.Enums.CouponDiscountType.Percentage,
                DiscountValue = 50,
                MinOrderAmount = 50,
                MaxDiscountAmount = 250,
                UsageLimit = 50,
                TimesUsed = 8,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                ExpiryDate = DateTime.UtcNow.AddDays(14)
            }
        };

        await context.Coupons.AddRangeAsync(coupons);
        await context.SaveChangesAsync();
        logger?.LogInformation("Seeded {Count} coupons successfully.", coupons.Count);
    }
}

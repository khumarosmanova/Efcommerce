using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Identity.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<AppUser, AppRole, Guid>
{
    private readonly ICurrentUserService _currentUserService;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        ICurrentUserService currentUserService) : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
    public DbSet<Coupon> Coupons => Set<Coupon>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    private readonly HashSet<object> _hardDeleteEntries = new();

    /// <summary>
    /// Explicitly marks an entity for physical database removal, bypassing soft delete interception.
    /// </summary>
    public void HardDelete<T>(T entity) where T : class
    {
        _hardDeleteEntries.Add(entity);
        Set<T>().Remove(entity);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditAndSoftDeleteRules();
        await HandleCategoryCascadeSoftDeleteAsync(cancellationToken);

        var result = await base.SaveChangesAsync(cancellationToken);
        _hardDeleteEntries.Clear();
        return result;
    }

    private void ApplyAuditAndSoftDeleteRules()
    {
        var utcNow = DateTime.UtcNow;
        var currentUserId = _currentUserService.UserId ?? "System";

        // 1. Audit timestamps (CreatedAt, UpdatedAt)
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAt == default)
                {
                    entry.Entity.CreatedAt = utcNow;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = utcNow;
            }
        }

        // 2. Intercept physical deletion on ISoftDeletable and convert to Soft Delete
        foreach (var entry in ChangeTracker.Entries<ISoftDeletable>())
        {
            if (_hardDeleteEntries.Contains(entry.Entity))
            {
                // Explicit permanent deletion requested; allow EF Core to perform physical DELETE
                continue;
            }

            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedAt = utcNow;
                entry.Entity.DeletedBy = currentUserId;

                if (entry.Entity is BaseEntity baseEntity)
                {
                    baseEntity.UpdatedAt = utcNow;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                var isDeletedProp = entry.Property(nameof(ISoftDeletable.IsDeleted));

                // If marked as deleted manually via update
                if (entry.Entity.IsDeleted && isDeletedProp.OriginalValue is false)
                {
                    entry.Entity.DeletedAt ??= utcNow;
                    entry.Entity.DeletedBy ??= currentUserId;
                }
                // If entity is being restored
                else if (!entry.Entity.IsDeleted && isDeletedProp.OriginalValue is true)
                {
                    entry.Entity.DeletedAt = null;
                    entry.Entity.DeletedBy = null;
                }
            }
        }
    }

    /*
     * BUSINESS RULE:
     * When a Category is soft-deleted, cascade soft-delete all of its active products.
     * When a Category is restored, restore ONLY the category; its products stay deleted
     * until restored manually by an administrator.
     * This prevents accidentally reactivating deprecated or discontinued products
     * when a category structure is un-archived.
     */
    private async Task HandleCategoryCascadeSoftDeleteAsync(CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var currentUserId = _currentUserService.UserId ?? "System";

        var softDeletedCategoryEntries = ChangeTracker.Entries<Category>()
            .Where(e => e.Entity.IsDeleted &&
                        (e.State == EntityState.Modified || e.State == EntityState.Deleted))
            .ToList();

        foreach (var entry in softDeletedCategoryEntries)
        {
            var categoryId = entry.Entity.Id;
            if (categoryId <= 0) continue;

            // Fetch any currently active products under this category
            var activeProducts = await Products
                .Where(p => p.CategoryId == categoryId && !p.IsDeleted)
                .ToListAsync(cancellationToken);

            foreach (var product in activeProducts)
            {
                product.IsDeleted = true;
                product.DeletedAt = utcNow;
                product.DeletedBy = currentUserId;
                product.UpdatedAt = utcNow;
            }
        }
    }
}

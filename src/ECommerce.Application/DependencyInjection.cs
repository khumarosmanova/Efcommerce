using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddScoped<Interfaces.ICategoryService, Services.CategoryService>();
        services.AddScoped<Interfaces.IProductService, Services.ProductService>();
        services.AddScoped<Interfaces.ICartService, Services.CartService>();
        services.AddScoped<Interfaces.IOrderService, Services.OrderService>();
        services.AddScoped<Interfaces.ITrashService, Services.TrashService>();
        services.AddScoped<Interfaces.IReviewService, Services.ReviewService>();
        services.AddScoped<Interfaces.IWishlistService, Services.WishlistService>();
        services.AddScoped<Interfaces.ICouponService, Services.CouponService>();

        return services;
    }
}

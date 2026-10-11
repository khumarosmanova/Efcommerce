using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.DTOs.Orders;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Services;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmailService _emailService;

    public OrderService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _emailService = emailService;
    }

    private bool IsAdminUser() =>
        _currentUserService.Roles.Any(r => r is "SuperAdmin" or "Admin" or "Moderator");

    private string GetRequiredUserId()
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedException("User is not authenticated.");
        }
        return userId;
    }

    public async Task<OrderDto> CheckoutAsync(CheckoutDto dto, CancellationToken cancellationToken = default)
    {
        if (IsAdminUser())
        {
            throw new ValidationException("Administrator accounts cannot place orders.");
        }
        var userId = GetRequiredUserId();

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // 1. Load user's cart with items and products
            var cart = await _unitOfWork.Repository<Cart>()
                .Query()
                .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

            if (cart == null || !cart.Items.Any())
            {
                throw new ValidationException("Your shopping cart is empty.");
            }

            var orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
            var order = new Order
            {
                OrderNumber = orderNumber,
                UserId = userId,
                Status = OrderStatus.Pending,
                ShippingAddress = dto.ShippingAddress.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            decimal totalAmount = 0;

            // 2. Validate stock, decrement, and snapshot items
            foreach (var cartItem in cart.Items.ToList())
            {
                var product = await _unitOfWork.Repository<Product>()
                    .GetByIdAsync(cartItem.ProductId, cancellationToken);

                if (product == null || !product.IsActive)
                {
                    throw new ValidationException(
                        $"Product '{cartItem.Product?.Name ?? cartItem.ProductId.ToString()}' is no longer available.");
                }

                if (product.StockQuantity < cartItem.Quantity)
                {
                    throw new ValidationException(
                        $"Insufficient stock for '{product.Name}'. Available: {product.StockQuantity}, Requested: {cartItem.Quantity}.");
                }

                // Decrement stock
                product.StockQuantity -= cartItem.Quantity;
                _unitOfWork.Repository<Product>().Update(product);

                // Snapshot unit price (discount price if active, otherwise standard price)
                var unitPrice = product.DiscountPrice ?? product.Price;

                var orderItem = new OrderItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    UnitPrice = unitPrice,
                    Quantity = cartItem.Quantity,
                    CreatedAt = DateTime.UtcNow
                };

                order.OrderItems.Add(orderItem);
                totalAmount += unitPrice * cartItem.Quantity;

                // Remove from cart
                _unitOfWork.Repository<CartItem>().Delete(cartItem);
            }

            // 3. Apply optional discount coupon
            decimal discountAmount = 0;
            if (!string.IsNullOrWhiteSpace(dto.CouponCode))
            {
                var couponCode = dto.CouponCode.Trim().ToUpperInvariant();
                var coupon = await _unitOfWork.Repository<Coupon>()
                    .Query()
                    .FirstOrDefaultAsync(c => c.Code == couponCode, cancellationToken);

                if (coupon == null || !coupon.IsActive ||
                    (coupon.ExpiryDate.HasValue && coupon.ExpiryDate.Value < DateTime.UtcNow) ||
                    (coupon.UsageLimit.HasValue && coupon.TimesUsed >= coupon.UsageLimit.Value) ||
                    (coupon.MinOrderAmount.HasValue && totalAmount < coupon.MinOrderAmount.Value))
                {
                    throw new ValidationException($"Coupon '{couponCode}' is invalid, expired, or does not meet the minimum order requirement.");
                }

                if (coupon.DiscountType == Domain.Enums.CouponDiscountType.Percentage)
                {
                    discountAmount = Math.Round((totalAmount * coupon.DiscountValue) / 100m, 2);
                    if (coupon.MaxDiscountAmount.HasValue && discountAmount > coupon.MaxDiscountAmount.Value)
                    {
                        discountAmount = coupon.MaxDiscountAmount.Value;
                    }
                }
                else
                {
                    discountAmount = Math.Min(totalAmount, coupon.DiscountValue);
                }

                coupon.TimesUsed++;
                _unitOfWork.Repository<Coupon>().Update(coupon);

                order.CouponCode = coupon.Code;
                order.DiscountAmount = discountAmount;
            }

            order.TotalAmount = Math.Max(0, totalAmount - discountAmount);

            await _unitOfWork.Repository<Order>().AddAsync(order, cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // Send order confirmation email in background
            var customerEmail = _currentUserService.Email;
            if (!string.IsNullOrWhiteSpace(customerEmail))
            {
                var emailItems = order.OrderItems.Select(oi => (
                    oi.ProductName,
                    oi.Quantity,
                    oi.UnitPrice,
                    oi.UnitPrice * oi.Quantity
                )).ToList();

                _ = Task.Run(() => _emailService.SendOrderConfirmationEmailAsync(
                    customerEmail,
                    order.OrderNumber,
                    order.TotalAmount,
                    order.ShippingAddress,
                    emailItems,
                    CancellationToken.None));
            }

            return MapToDto(order);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<PagedResult<OrderDto>> GetMyOrdersAsync(
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var userId = GetRequiredUserId();

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _unitOfWork.Repository<Order>()
            .Query()
            .AsNoTracking()
            .Include(o => o.OrderItems)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var orders = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = orders.Select(MapToDto).ToList();
        return new PagedResult<OrderDto>(items, totalCount, page, pageSize);
    }

    public async Task<OrderDto> GetMyOrderByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var userId = GetRequiredUserId();

        var order = await _unitOfWork.Repository<Order>()
            .Query()
            .AsNoTracking()
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId, cancellationToken);

        if (order == null)
        {
            throw new NotFoundException("Order", id);
        }

        return MapToDto(order);
    }

    public async Task<PagedResult<OrderDto>> GetAdminOrdersAsync(
        OrderFilterParams filter,
        CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.Repository<Order>()
            .Query()
            .AsNoTracking()
            .Include(o => o.OrderItems)
            .AsQueryable();

        if (filter.Status.HasValue)
        {
            query = query.Where(o => o.Status == filter.Status.Value);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt <= filter.ToDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            query = query.Where(o =>
                o.OrderNumber.ToLower().Contains(search) ||
                o.ShippingAddress.ToLower().Contains(search) ||
                o.UserId.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = orders.Select(MapToDto).ToList();
        return new PagedResult<OrderDto>(items, totalCount, page, pageSize);
    }

    public async Task<OrderDto> GetAdminOrderByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var order = await _unitOfWork.Repository<Order>()
            .Query()
            .AsNoTracking()
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (order == null)
        {
            throw new NotFoundException("Order", id);
        }

        return MapToDto(order);
    }

    public async Task<OrderDto> UpdateOrderStatusAsync(
        int id,
        UpdateOrderStatusDto dto,
        CancellationToken cancellationToken = default)
    {
        var order = await _unitOfWork.Repository<Order>()
            .Query()
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (order == null)
        {
            throw new NotFoundException("Order", id);
        }

        var currentStatus = order.Status;
        var newStatus = dto.Status;

        if (currentStatus == newStatus)
        {
            return MapToDto(order);
        }

        // Validate state machine transitions
        var isValid = currentStatus switch
        {
            OrderStatus.Pending => newStatus == OrderStatus.Confirmed || newStatus == OrderStatus.Cancelled,
            OrderStatus.Confirmed => newStatus == OrderStatus.Shipped || newStatus == OrderStatus.Cancelled,
            OrderStatus.Shipped => newStatus == OrderStatus.Delivered || newStatus == OrderStatus.Cancelled,
            OrderStatus.Delivered => false, // Delivered is final
            OrderStatus.Cancelled => false, // Cancelled is final
            _ => false
        };

        if (!isValid)
        {
            throw new ValidationException(
                $"Invalid order status transition from '{currentStatus}' to '{newStatus}'.");
        }

        // When order is cancelled, return stock back to products
        if (newStatus == OrderStatus.Cancelled)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                foreach (var item in order.OrderItems)
                {
                    var product = await _unitOfWork.Repository<Product>()
                        .GetByIdAsync(item.ProductId, cancellationToken);
                    if (product != null)
                    {
                        product.StockQuantity += item.Quantity;
                        _unitOfWork.Repository<Product>().Update(product);
                    }
                }

                order.Status = newStatus;
                _unitOfWork.Repository<Order>().Update(order);

                await _unitOfWork.CommitTransactionAsync(cancellationToken);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
        else
        {
            order.Status = newStatus;
            _unitOfWork.Repository<Order>().Update(order);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        // Notify customer about status change
        var targetEmail = _currentUserService.Email ?? order.UserId;
        if (!string.IsNullOrWhiteSpace(targetEmail) && targetEmail.Contains('@'))
        {
            _ = Task.Run(() => _emailService.SendOrderStatusChangedEmailAsync(
                targetEmail,
                order.OrderNumber,
                newStatus.ToString(),
                CancellationToken.None));
        }

        return MapToDto(order);
    }

    private static OrderDto MapToDto(Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            UserId = order.UserId,
            Status = order.Status.ToString(),
            TotalAmount = order.TotalAmount,
            DiscountAmount = order.DiscountAmount,
            CouponCode = order.CouponCode,
            ShippingAddress = order.ShippingAddress,
            CreatedAt = order.CreatedAt,
            Items = order.OrderItems.Select(i => new OrderItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity
            }).ToList()
        };
    }
}

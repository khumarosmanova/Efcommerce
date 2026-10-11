using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.DTOs.Reviews;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Services;

public class ReviewService : IReviewService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateReviewDto> _validator;

    public ReviewService(
        IUnitOfWork unitOfWork,
        IValidator<CreateReviewDto> validator)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<ProductRatingSummaryDto> GetProductReviewsAsync(int productId, CancellationToken cancellationToken = default)
    {
        var productExists = await _unitOfWork.Repository<Product>()
            .ExistsAsync(p => p.Id == productId, cancellationToken);

        if (!productExists)
        {
            throw new NotFoundException($"Product with ID {productId} was not found.");
        }

        var reviews = await _unitOfWork.Repository<Review>()
            .Query()
            .AsNoTracking()
            .Where(r => r.ProductId == productId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        var reviewDtos = reviews.Select(r => new ReviewDto
        {
            Id = r.Id,
            ProductId = r.ProductId,
            UserId = r.UserId,
            UserFullName = r.UserFullName,
            UserEmail = r.UserEmail,
            Rating = r.Rating,
            Comment = r.Comment,
            CreatedAt = r.CreatedAt
        }).ToList();

        var totalReviews = reviewDtos.Count;
        var avgRating = totalReviews > 0 ? Math.Round(reviewDtos.Average(r => r.Rating), 1) : 0;

        return new ProductRatingSummaryDto
        {
            ProductId = productId,
            AverageRating = avgRating,
            TotalReviews = totalReviews,
            FiveStarCount = reviewDtos.Count(r => r.Rating == 5),
            FourStarCount = reviewDtos.Count(r => r.Rating == 4),
            ThreeStarCount = reviewDtos.Count(r => r.Rating == 3),
            TwoStarCount = reviewDtos.Count(r => r.Rating == 2),
            OneStarCount = reviewDtos.Count(r => r.Rating == 1),
            Reviews = reviewDtos
        };
    }

    public async Task<ReviewDto> AddReviewAsync(
        int productId,
        CreateReviewDto dto,
        Guid userId,
        string userFullName,
        string userEmail,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new Common.Exceptions.ValidationException(validationResult.Errors.Select(e => e.ErrorMessage).ToList());
        }

        var product = await _unitOfWork.Repository<Product>().GetByIdAsync(productId, cancellationToken);
        if (product == null)
        {
            throw new NotFoundException($"Product with ID {productId} was not found.");
        }

        var review = new Review
        {
            ProductId = productId,
            UserId = userId,
            UserFullName = string.IsNullOrWhiteSpace(userFullName) ? "Valued Customer" : userFullName,
            UserEmail = userEmail,
            Rating = dto.Rating,
            Comment = dto.Comment?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<Review>().AddAsync(review, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ReviewDto
        {
            Id = review.Id,
            ProductId = review.ProductId,
            UserId = review.UserId,
            UserFullName = review.UserFullName,
            UserEmail = review.UserEmail,
            Rating = review.Rating,
            Comment = review.Comment,
            CreatedAt = review.CreatedAt
        };
    }

    public async Task DeleteReviewAsync(int id, Guid userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var review = await _unitOfWork.Repository<Review>().GetByIdAsync(id, cancellationToken);
        if (review == null)
        {
            throw new NotFoundException($"Review with ID {id} was not found.");
        }

        if (!isAdmin && review.UserId != userId)
        {
            throw new UnauthorizedException("You are not authorized to delete this review.");
        }

        _unitOfWork.Repository<Review>().Delete(review);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

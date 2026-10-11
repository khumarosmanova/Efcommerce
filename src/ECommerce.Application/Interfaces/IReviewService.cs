using ECommerce.Application.DTOs.Reviews;

namespace ECommerce.Application.Interfaces;

public interface IReviewService
{
    Task<ProductRatingSummaryDto> GetProductReviewsAsync(int productId, CancellationToken cancellationToken = default);
    Task<ReviewDto> AddReviewAsync(int productId, CreateReviewDto dto, Guid userId, string userFullName, string userEmail, CancellationToken cancellationToken = default);
    Task DeleteReviewAsync(int id, Guid userId, bool isAdmin, CancellationToken cancellationToken = default);
}

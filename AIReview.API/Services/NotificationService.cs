using Microsoft.AspNetCore.SignalR;
using AIReview.Core.Interfaces;

namespace AIReview.API.Services;

public class NotificationService : INotificationService
{
    private readonly IHubContext<Hubs.NotificationHub> _hubContext;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IHubContext<Hubs.NotificationHub> hubContext,
        ILogger<NotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task SendReviewStatusUpdateAsync(string userId, string reviewId, string status, string message)
    {
        try
        {
            var notification = new
            {
                Type = "review_status_update",
                ReviewId = reviewId,
                Status = status,
                Message = message,
                Timestamp = DateTime.UtcNow
            };

            await _hubContext.Clients.Group($"user_{userId}")
                .SendAsync("ReviewStatusUpdate", notification);

            _logger.LogInformation("Sent review status update to user {UserId} for review {ReviewId}", userId, reviewId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send review status update to user {UserId}", userId);
        }
    }

    public async Task SendProjectNotificationAsync(string projectId, string message, List<string> userIds)
    {
        try
        {
            var notification = new
            {
                Type = "project_notification",
                ProjectId = projectId,
                Message = message,
                Timestamp = DateTime.UtcNow
            };

            foreach (var userId in userIds)
            {
                await _hubContext.Clients.Group($"user_{userId}")
                    .SendAsync("ProjectNotification", notification);
            }

            _logger.LogInformation("Sent project notification for project {ProjectId} to {UserCount} users", 
                projectId, userIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send project notification for project {ProjectId}", projectId);
        }
    }

    public async Task SendReviewCommentAsync(string reviewId, string commentId, string authorName, string content)
    {
        try
        {
            var notification = new
            {
                Type = "review_comment",
                ReviewId = reviewId,
                CommentId = commentId,
                AuthorName = authorName,
                Content = content,
                Timestamp = DateTime.UtcNow
            };

            await _hubContext.Clients.Group($"review_{reviewId}")
                .SendAsync("ReviewComment", notification);

            _logger.LogInformation("Sent review comment notification for review {ReviewId}", reviewId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send review comment notification for review {ReviewId}", reviewId);
        }
    }

    public async Task SendBroadcastAsync(string message)
    {
        try
        {
            var notification = new
            {
                Type = "broadcast",
                Message = message,
                Timestamp = DateTime.UtcNow
            };

            await _hubContext.Clients.All.SendAsync("Broadcast", notification);

            _logger.LogInformation("Sent broadcast notification: {Message}", message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send broadcast notification");
        }
    }
}
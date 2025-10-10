namespace AIReview.Core.Interfaces;

public interface INotificationService
{
    Task SendReviewStatusUpdateAsync(string userId, string reviewId, string status, string message);
    Task SendProjectNotificationAsync(string projectId, string message, List<string> userIds);
    Task SendReviewCommentAsync(string reviewId, string commentId, string authorName, string content);
    Task SendBroadcastAsync(string message);
}
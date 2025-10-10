namespace AIReview.Core.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IProjectRepository Projects { get; }
    IProjectMemberRepository ProjectMembers { get; }
    IReviewRequestRepository ReviewRequests { get; }
    IReviewCommentRepository ReviewComments { get; }
    
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
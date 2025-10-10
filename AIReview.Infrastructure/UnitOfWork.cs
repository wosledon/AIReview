using Microsoft.EntityFrameworkCore.Storage;
using AIReview.Core.Interfaces;
using AIReview.Infrastructure.Data;
using AIReview.Infrastructure.Repositories;

namespace AIReview.Infrastructure;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction? _transaction;

    private IProjectRepository? _projects;
    private IProjectMemberRepository? _projectMembers;
    private IReviewRequestRepository? _reviewRequests;
    private IReviewCommentRepository? _reviewComments;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IProjectRepository Projects =>
        _projects ??= new ProjectRepository(_context);

    public IProjectMemberRepository ProjectMembers =>
        _projectMembers ??= new ProjectMemberRepository(_context);

    public IReviewRequestRepository ReviewRequests =>
        _reviewRequests ??= new ReviewRequestRepository(_context);

    public IReviewCommentRepository ReviewComments =>
        _reviewComments ??= new ReviewCommentRepository(_context);

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
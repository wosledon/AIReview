using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AIReview.Core.Entities;
using AIReview.Shared.Enums;

namespace AIReview.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Project> Projects { get; set; }
    public DbSet<ProjectMember> ProjectMembers { get; set; }
    public DbSet<ReviewRequest> ReviewRequests { get; set; }
    public DbSet<ReviewComment> ReviewComments { get; set; }
    public DbSet<LLMConfiguration> LLMConfigurations { get; set; }
    
    // Git相关实体
    public DbSet<GitRepository> GitRepositories { get; set; }
    public DbSet<GitBranch> GitBranches { get; set; }
    public DbSet<GitCommit> GitCommits { get; set; }
    public DbSet<GitFileChange> GitFileChanges { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // 配置项目实体
        builder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Language).IsRequired().HasMaxLength(50);
            entity.Property(e => e.RepositoryUrl).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            // 关系配置
            entity.HasOne(e => e.Owner)
                .WithMany(u => u.OwnedProjects)
                .HasForeignKey(e => e.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Members)
                .WithOne(m => m.Project)
                .HasForeignKey(m => m.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.ReviewRequests)
                .WithOne(r => r.Project)
                .HasForeignKey(r => r.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 配置项目成员实体
        builder.Entity<ProjectMember>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.JoinedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            // 确保同一用户在同一项目中只能有一个角色
            entity.HasIndex(e => new { e.ProjectId, e.UserId }).IsUnique();

            // 关系配置
            entity.HasOne(e => e.User)
                .WithMany(u => u.ProjectMemberships)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 配置评审请求实体
        builder.Entity<ReviewRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Branch).IsRequired().HasMaxLength(255);
            entity.Property(e => e.BaseBranch).HasMaxLength(255).HasDefaultValue("main");
            entity.Property(e => e.Status).HasDefaultValue(ReviewState.Pending);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            // 关系配置
            entity.HasOne(e => e.Author)
                .WithMany(u => u.AuthoredReviews)
                .HasForeignKey(e => e.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Comments)
                .WithOne(c => c.ReviewRequest)
                .HasForeignKey(c => c.ReviewRequestId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 配置评审评论实体
        builder.Entity<ReviewComment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.FilePath).HasMaxLength(500);
            entity.Property(e => e.Severity).HasDefaultValue(ReviewCommentSeverity.Info);
            entity.Property(e => e.Category).HasDefaultValue(ReviewCommentCategory.Quality);
            entity.Property(e => e.IsAIGenerated).HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            // 关系配置
            entity.HasOne(e => e.Author)
                .WithMany(u => u.ReviewComments)
                .HasForeignKey(e => e.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 配置Identity表名（可选）
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // 配置LLM配置实体
        builder.Entity<LLMConfiguration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Provider).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ApiEndpoint).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ApiKey).IsRequired();
            entity.Property(e => e.Model).HasMaxLength(100);
            entity.Property(e => e.MaxTokens).HasDefaultValue(4000);
            entity.Property(e => e.Temperature).HasDefaultValue(0.3);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsDefault).HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            // 确保只有一个默认配置
            entity.HasIndex(e => e.IsDefault)
                .HasFilter("IsDefault = 1")
                .IsUnique();
        });

        // 配置Git仓库实体
        builder.Entity<GitRepository>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Url).IsRequired().HasMaxLength(500);
            entity.Property(e => e.LocalPath).HasMaxLength(500);
            entity.Property(e => e.DefaultBranch).HasMaxLength(100).HasDefaultValue("main");
            entity.Property(e => e.Username).HasMaxLength(100);
            entity.Property(e => e.AccessToken).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            // 关系配置
            entity.HasOne(e => e.Project)
                .WithMany()
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.Branches)
                .WithOne(b => b.Repository)
                .HasForeignKey(b => b.RepositoryId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Commits)
                .WithOne(c => c.Repository)
                .HasForeignKey(c => c.RepositoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 配置Git分支实体
        builder.Entity<GitBranch>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CommitSha).IsRequired().HasMaxLength(100);
            entity.Property(e => e.IsDefault).HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            // 唯一约束：同一仓库中分支名唯一
            entity.HasIndex(e => new { e.RepositoryId, e.Name }).IsUnique();
        });

        // 配置Git提交实体
        builder.Entity<GitCommit>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Sha).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(500);
            entity.Property(e => e.AuthorName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.AuthorEmail).IsRequired().HasMaxLength(200);
            entity.Property(e => e.CommitterName).HasMaxLength(100);
            entity.Property(e => e.CommitterEmail).HasMaxLength(200);
            entity.Property(e => e.ParentSha).HasMaxLength(100);
            entity.Property(e => e.BranchName).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            // 唯一约束：同一仓库中SHA唯一
            entity.HasIndex(e => new { e.RepositoryId, e.Sha }).IsUnique();

            entity.HasMany(e => e.FileChanges)
                .WithOne(f => f.Commit)
                .HasForeignKey(f => f.CommitId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 配置Git文件变更实体
        builder.Entity<GitFileChange>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ChangeType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.AddedLines).HasDefaultValue(0);
            entity.Property(e => e.DeletedLines).HasDefaultValue(0);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entities = ChangeTracker.Entries()
            .Where(e => e.Entity is Project || e.Entity is ReviewRequest || e.Entity is ApplicationUser || e.Entity is LLMConfiguration)
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entity in entities)
        {
            if (entity.State == EntityState.Added)
            {
                if (entity.Entity is Project project)
                {
                    project.CreatedAt = DateTime.UtcNow;
                    project.UpdatedAt = DateTime.UtcNow;
                }
                else if (entity.Entity is ReviewRequest review)
                {
                    review.CreatedAt = DateTime.UtcNow;
                    review.UpdatedAt = DateTime.UtcNow;
                }
                else if (entity.Entity is ApplicationUser user)
                {
                    user.CreatedAt = DateTime.UtcNow;
                    user.UpdatedAt = DateTime.UtcNow;
                }
                else if (entity.Entity is LLMConfiguration llmConfig)
                {
                    llmConfig.CreatedAt = DateTime.UtcNow;
                    llmConfig.UpdatedAt = DateTime.UtcNow;
                }
                else if (entity.Entity is GitRepository gitRepo)
                {
                    gitRepo.CreatedAt = DateTime.UtcNow;
                    gitRepo.UpdatedAt = DateTime.UtcNow;
                }
                else if (entity.Entity is GitBranch gitBranch)
                {
                    gitBranch.CreatedAt = DateTime.UtcNow;
                    gitBranch.UpdatedAt = DateTime.UtcNow;
                }
                else if (entity.Entity is GitCommit gitCommit)
                {
                    gitCommit.CreatedAt = DateTime.UtcNow;
                }
                else if (entity.Entity is GitFileChange gitFileChange)
                {
                    gitFileChange.CreatedAt = DateTime.UtcNow;
                }
            }
            else if (entity.State == EntityState.Modified)
            {
                if (entity.Entity is Project project)
                {
                    project.UpdatedAt = DateTime.UtcNow;
                }
                else if (entity.Entity is ReviewRequest review)
                {
                    review.UpdatedAt = DateTime.UtcNow;
                }
                else if (entity.Entity is ApplicationUser user)
                {
                    user.UpdatedAt = DateTime.UtcNow;
                }
                else if (entity.Entity is LLMConfiguration llmConfig)
                {
                    llmConfig.UpdatedAt = DateTime.UtcNow;
                }
                else if (entity.Entity is GitRepository gitRepo)
                {
                    gitRepo.UpdatedAt = DateTime.UtcNow;
                }
                else if (entity.Entity is GitBranch gitBranch)
                {
                    gitBranch.UpdatedAt = DateTime.UtcNow;
                }
            }
        }
    }
}
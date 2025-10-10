using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using AIReview.Core.Entities;
using AIReview.Core.Services;
using AIReview.Infrastructure;
using AIReview.Infrastructure.Data;
using AIReview.Shared.DTOs;

namespace AIReview.Tests.Services
{
    public class ReviewServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ILogger<ReviewService>> _loggerMock;
    private readonly ReviewService _reviewService;
    private readonly Mock<AIReview.Core.Interfaces.IProjectService> _projectServiceMock;
        private readonly string _testUserId = "test-user-id";

        public ReviewServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _loggerMock = new Mock<ILogger<ReviewService>>();
            
            var unitOfWork = new UnitOfWork(_context);
            _projectServiceMock = new Mock<AIReview.Core.Interfaces.IProjectService>();
            _projectServiceMock.Setup(p => p.HasProjectAccessAsync(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(true);
            _reviewService = new ReviewService(unitOfWork, _projectServiceMock.Object, _loggerMock.Object);

            // Setup test data
            SeedTestData();
        }

        private void SeedTestData()
        {
            var project = new Project
            {
                Name = "Test Project",
                Description = "Test Description",
                OwnerId = _testUserId,
                RepositoryUrl = "https://github.com/test/repo",
                Language = "csharp"
            };

            _context.Projects.Add(project);
            _context.SaveChanges();
        }

        [Fact]
        public async Task CreateReviewAsync_ShouldCreateReview_WhenValidInput()
        {
            // Arrange
            var createReviewDto = new CreateReviewRequest
            {
                ProjectId = 1,
                Title = "Test Review",
                Description = "Test Description",
                Branch = "feature/x",
                BaseBranch = "main",
                PullRequestNumber = 1
            };

            // Act
            var result = await _reviewService.CreateReviewAsync(createReviewDto, _testUserId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(createReviewDto.Title, result.Title);
            Assert.Equal(createReviewDto.Description, result.Description);
            Assert.Equal(_testUserId, result.AuthorId);

            var reviewInDb = await _context.ReviewRequests.FindAsync(result.Id);
            Assert.NotNull(reviewInDb);
        }

        [Fact]
        public async Task GetReviewAsync_ShouldReturnReview_WhenExists()
        {
            // Arrange
            // 通过服务创建，确保完整管道
            var created = await _reviewService.CreateReviewAsync(new CreateReviewRequest
            {
                ProjectId = _context.Projects.First().Id,
                Title = "Test Review",
                Description = "Test Description",
                Branch = "feature/x",
                BaseBranch = "main",
                PullRequestNumber = 1
            }, _testUserId);

            // Act
            var result = await _reviewService.GetReviewAsync(created.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(created.Title, result.Title);
            Assert.Equal(created.Description, result.Description);
        }

        [Fact]
        public async Task AddReviewCommentAsync_ShouldAddComment_WhenValidInput()
        {
            // Arrange
            var created = await _reviewService.CreateReviewAsync(new CreateReviewRequest
            {
                ProjectId = _context.Projects.First().Id,
                Title = "Test Review",
                Description = "Test Description",
                Branch = "feature/x",
                BaseBranch = "main",
                PullRequestNumber = 2
            }, _testUserId);

            var createCommentDto = new AddCommentRequest
            {
                Content = "This is a test comment",
                FilePath = "/src/test.cs",
                LineNumber = 10,
                Severity = AIReview.Shared.Enums.ReviewCommentSeverity.Info
            };

            // Act
            var result = await _reviewService.AddReviewCommentAsync(created.Id, createCommentDto, _testUserId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(createCommentDto.Content, result.Content);
            Assert.Equal(_testUserId, result.AuthorId);

            var commentInDb = await _context.ReviewComments.FindAsync(result.Id);
            Assert.NotNull(commentInDb);
        }

        [Fact]
        public async Task GetReviewCommentsAsync_ShouldReturnComments_WhenReviewExists()
        {
            // Arrange
            var created = await _reviewService.CreateReviewAsync(new CreateReviewRequest
            {
                ProjectId = _context.Projects.First().Id,
                Title = "Test Review",
                Description = "Test Description",
                Branch = "feature/x",
                BaseBranch = "main",
                PullRequestNumber = 3
            }, _testUserId);

            var comment1 = new ReviewComment
            {
                ReviewRequestId = created.Id,
                Content = "Comment 1",
                AuthorId = _testUserId,
                FilePath = "/src/test.cs",
                LineNumber = 10,
                Severity = AIReview.Shared.Enums.ReviewCommentSeverity.Info
            };

            var comment2 = new ReviewComment
            {
                ReviewRequestId = created.Id,
                Content = "Comment 2",
                AuthorId = _testUserId,
                FilePath = "/src/test.cs",
                LineNumber = 20,
                Severity = AIReview.Shared.Enums.ReviewCommentSeverity.Warning
            };

            _context.ReviewComments.AddRange(comment1, comment2);
            await _context.SaveChangesAsync();

            // Act
            var result = (await _reviewService.GetReviewCommentsAsync(created.Id)).ToList();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
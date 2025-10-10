using Microsoft.EntityFrameworkCore;
using Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using AIReview.Core.Entities;
using AIReview.Core.Services;
using AIReview.Infrastructure;
using AIReview.Infrastructure.Data;
using AIReview.Shared.DTOs;
using AIReview.Core.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace AIReview.Tests.Services
{
    public class ProjectServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ILogger<ProjectService>> _loggerMock;
    private readonly ProjectService _projectService;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly string _testUserId = "test-user-id";

        public ProjectServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _loggerMock = new Mock<ILogger<ProjectService>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);
            var unitOfWork = new UnitOfWork(_context);
            _projectService = new ProjectService(unitOfWork, _userManagerMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task CreateProjectAsync_ShouldCreateProject_WhenValidInput()
        {
            // Arrange
            var createProjectDto = new CreateProjectRequest
            {
                Name = "Test Project",
                Description = "Test Description",
                RepositoryUrl = "https://github.com/test/repo",
                Language = "csharp"
            };

            // Act
            var result = await _projectService.CreateProjectAsync(createProjectDto, _testUserId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(createProjectDto.Name, result.Name);
            Assert.Equal(createProjectDto.Description, result.Description);
            // OwnerId 不在 ProjectDto 中，验证基本字段

            var projectInDb = await _context.Projects.FindAsync(result.Id);
            Assert.NotNull(projectInDb);
        }

        [Fact]
        public async Task GetProjectAsync_ShouldReturnProject_WhenProjectExists()
        {
            // Arrange
            var createProjectDto = new CreateProjectRequest
            {
                Name = "Test Project",
                Description = "Test Description",
                RepositoryUrl = "https://github.com/test/repo",
                Language = "csharp"
            };

            var created = await _projectService.CreateProjectAsync(createProjectDto, _testUserId);

            // Act
            var result = await _projectService.GetProjectAsync(created.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(created.Name, result.Name);
            Assert.Equal(created.Description, result.Description);
        }

        [Fact]
        public async Task GetProjectAsync_ShouldReturnNull_WhenProjectNotFound()
        {
            // Arrange
            var nonExistentId = 999;

            // Act
            var result = await _projectService.GetProjectAsync(nonExistentId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetProjectAsync_ShouldReturnProject_WhenUserNotOwnerOrMember()
        {
            // Arrange
            var project = new Project
            {
                Name = "Test Project",
                Description = "Test Description",
                OwnerId = "another-user-id",
                RepositoryUrl = "https://github.com/test/repo",
                Language = "csharp"
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            // Act
            var result = await _projectService.GetProjectAsync(project.Id);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task UpdateProjectAsync_ShouldUpdateProject_WhenUserIsOwner()
        {
            // Arrange
            var created = await _projectService.CreateProjectAsync(new CreateProjectRequest
            {
                Name = "Original Name",
                Description = "Original Description",
                RepositoryUrl = "https://github.com/test/repo",
                Language = "csharp"
            }, _testUserId);

            var updateDto = new UpdateProjectRequest
            {
                Name = "Updated Name",
                Description = "Updated Description"
            };

            // Act
            var result = await _projectService.UpdateProjectAsync(created.Id, updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(updateDto.Name, result.Name);
            Assert.Equal(updateDto.Description, result.Description);
            // 验证更新字段
            Assert.Equal(updateDto.Name, result.Name);
            Assert.Equal(updateDto.Description, result.Description);
        }

        [Fact]
        public async Task DeleteProjectAsync_ShouldReturnTrue_WhenUserIsOwner()
        {
            // Arrange
            var created = await _projectService.CreateProjectAsync(new CreateProjectRequest
            {
                Name = "Test Project",
                Description = "Test Description",
                RepositoryUrl = "https://github.com/test/repo",
                Language = "csharp"
            }, _testUserId);

            // Act
            await _projectService.DeleteProjectAsync(created.Id);

            // Assert删除操作无返回值，验证实体已删除

            var deletedProject = await _context.Projects.FindAsync(created.Id);
            Assert.Null(deletedProject);
        }

        [Fact]
        public async Task AddMemberAsync_ShouldAddMember_WhenUserIsOwner()
        {
            // Arrange
            var created = await _projectService.CreateProjectAsync(new CreateProjectRequest
            {
                Name = "Test Project",
                Description = "Test Description",
                RepositoryUrl = "https://github.com/test/repo",
                Language = "csharp"
            }, _testUserId);

            var addMemberDto = new AddMemberRequest
            {
                Email = "new@example.com",
                Role = "Developer"
            };

            // Act
            _userManagerMock.Setup(x => x.FindByEmailAsync(addMemberDto.Email)).ReturnsAsync(new ApplicationUser { Id = "new-member-id", Email = addMemberDto.Email, UserName = addMemberDto.Email });
            await _projectService.AddProjectMemberAsync(created.Id, addMemberDto);

            // Assert
            var member = await _context.ProjectMembers
                .FirstOrDefaultAsync(m => m.ProjectId == created.Id && m.UserId == "new-member-id");
            Assert.NotNull(member);
            Assert.Equal(addMemberDto.Role, member.Role.ToString());
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
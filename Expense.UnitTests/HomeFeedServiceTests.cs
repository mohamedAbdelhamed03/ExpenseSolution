using Expense.Core.Application.Home;
using Expense.Core.Application.Persistence;
using Expense.Core.DTOs.Home;
using Expense.Infrastructure.Home;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Expense.UnitTests
{
    public class HomeFeedServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IHomeFeedRepository> _mockHomeFeedRepository;
        private readonly HomeFeedService _service;

        public HomeFeedServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockHomeFeedRepository = new Mock<IHomeFeedRepository>();
            _mockUnitOfWork.Setup(u => u.HomeFeed).Returns(_mockHomeFeedRepository.Object);
            _service = new HomeFeedService(_mockUnitOfWork.Object);
        }

        [Fact]
        public async Task GetFeedAsync_ShouldDelegateToRepository_WithCorrectParameters()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var page = 1;
            var pageSize = 20;
            var expectedItems = new List<HomeFeedItemDto>();

            _mockHomeFeedRepository
                .Setup(r => r.GetFeedAsync(userId, page, pageSize))
                .ReturnsAsync(expectedItems);

            // Act
            var result = await _service.GetFeedAsync(userId, page, pageSize);

            // Assert
            Assert.Same(expectedItems, result);
            _mockHomeFeedRepository.Verify(r => r.GetFeedAsync(userId, page, pageSize), Times.Once);
        }

        [Fact]
        public async Task GetFeedAsync_ShouldEnforcePaginationDefaults()
        {
            // Arrange
            var userId = Guid.NewGuid();
            
            // Act
            await _service.GetFeedAsync(userId, 0, 0);

            // Assert
            // Expect page 1, pageSize 10 (from implementation: if (pageSize < 1) pageSize = 10)
            _mockHomeFeedRepository.Verify(r => r.GetFeedAsync(userId, 1, 10), Times.Once);
        }

        [Fact]
        public async Task GetFeedAsync_ShouldCapPageSize()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            await _service.GetFeedAsync(userId, 1, 100);

            // Assert
            // Expect pageSize 50 (from implementation: if (pageSize > 50) pageSize = 50)
            _mockHomeFeedRepository.Verify(r => r.GetFeedAsync(userId, 1, 50), Times.Once);
        }
    }
}

using Expense.Core.Application.Persistence;
using Expense.Core.Application.Services;
using Expense.Core.Common.Exceptions;
using Expense.Core.Domain.Entities;
using Expense.Core.DTOs.Personal;
using Moq;
using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Expense.UnitTests
{
    public class PersonalExpenseServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IPersonalExpenseRepository> _mockRepo;
        private readonly PersonalExpenseService _service;

        public PersonalExpenseServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockRepo = new Mock<IPersonalExpenseRepository>();
            _mockUnitOfWork.Setup(u => u.PersonalExpenses).Returns(_mockRepo.Object);
            _service = new PersonalExpenseService(_mockUnitOfWork.Object);
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateEntity_WhenValid()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var expenseId = Guid.NewGuid();
            var existing = new PersonalExpense
            {
                Id = expenseId,
                UserId = userId.ToString(),
                Amount = 100,
                Currency = "EGP",
                Description = "Old",
                Date = DateTime.UtcNow
            };

            _mockRepo.Setup(r => r.Get(It.IsAny<Expression<Func<PersonalExpense, bool>>>(), false, It.IsAny<Expression<Func<PersonalExpense, object>>[]>()))
                .ReturnsAsync(existing);

            var dto = new UpdatePersonalExpenseDto
            {
                Amount = 200,
                Currency = "USD",
                Description = "New",
                Date = DateTime.UtcNow.AddDays(1)
            };

            // Act
            var result = await _service.UpdateAsync(userId, expenseId, dto);

            // Assert
            Assert.Equal(200, result.Amount);
            Assert.Equal("USD", result.Currency);
            Assert.Equal("New", result.Description);
            _mockRepo.Verify(r => r.Update(existing), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdatePatchAsync_ShouldUpdatePartialFields_WhenValid()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var expenseId = Guid.NewGuid();
            var existing = new PersonalExpense
            {
                Id = expenseId,
                UserId = userId.ToString(),
                Amount = 100,
                Currency = "EGP",
                Description = "Old",
                Date = DateTime.UtcNow
            };

            _mockRepo.Setup(r => r.Get(It.IsAny<Expression<Func<PersonalExpense, bool>>>(), false, It.IsAny<Expression<Func<PersonalExpense, object>>[]>()))
                .ReturnsAsync(existing);

            var dto = new UpdatePersonalExpensePatchDto
            {
                Amount = 200, // Only updating Amount
                // Currency, Date, Description are null
            };

            // Act
            var result = await _service.UpdatePatchAsync(userId, expenseId, dto);

            // Assert
            Assert.Equal(200, result.Amount);
            Assert.Equal("EGP", result.Currency); // Should remain unchanged
            Assert.Equal("Old", result.Description); // Should remain unchanged
            _mockRepo.Verify(r => r.Update(existing), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowNotFound_WhenEntityDoesNotExist()
        {
             _mockRepo.Setup(r => r.Get(It.IsAny<Expression<Func<PersonalExpense, bool>>>(), false, It.IsAny<Expression<Func<PersonalExpense, object>>[]>()))
                .ReturnsAsync((PersonalExpense)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateAsync(Guid.NewGuid(), Guid.NewGuid(), new UpdatePersonalExpenseDto()));
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowAccessDenied_WhenUserDoesNotOwnEntity()
        {
            var userId = Guid.NewGuid();
            var otherUser = Guid.NewGuid();
            var existing = new PersonalExpense { Id = Guid.NewGuid(), UserId = otherUser.ToString() };

             _mockRepo.Setup(r => r.Get(It.IsAny<Expression<Func<PersonalExpense, bool>>>(), false, It.IsAny<Expression<Func<PersonalExpense, object>>[]>()))
                .ReturnsAsync(existing);

            await Assert.ThrowsAsync<AccessDeniedException>(() => _service.UpdateAsync(userId, existing.Id, new UpdatePersonalExpenseDto()));
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveEntity_WhenValid()
        {
            var userId = Guid.NewGuid();
            var expenseId = Guid.NewGuid();
            var existing = new PersonalExpense
            {
                Id = expenseId,
                UserId = userId.ToString()
            };

            _mockRepo.Setup(r => r.Get(It.IsAny<Expression<Func<PersonalExpense, bool>>>(), false, It.IsAny<Expression<Func<PersonalExpense, object>>[]>()))
                .ReturnsAsync(existing);

            await _service.DeleteAsync(userId, expenseId);

            _mockRepo.Verify(r => r.Remove(existing), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Expense.Core.Application.Persistence;
using Expense.Core.Common.Exceptions;
using Expense.Core.Domain.Entities;
using Expense.Core.DTOs.Debts;
using Expense.Infrastructure.Debts;
using FluentAssertions;
using Moq;
using Xunit;
using ExpenseEntity = Expense.Core.Domain.Entities.Expense;

namespace Expense.UnitTests
{
    public class DebtSimplificationServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IBalanceRepository> _mockBalanceRepo;
        private readonly Mock<IGroupRepository> _mockGroupRepo;
        private readonly DebtSimplificationService _service;

        public DebtSimplificationServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockBalanceRepo = new Mock<IBalanceRepository>();
            _mockGroupRepo = new Mock<IGroupRepository>();

            _mockUnitOfWork.Setup(u => u.Balances).Returns(_mockBalanceRepo.Object);
            _mockUnitOfWork.Setup(u => u.Groups).Returns(_mockGroupRepo.Object);

            _service = new DebtSimplificationService(_mockUnitOfWork.Object);
        }

        [Fact]
        public async Task GetSimplifiedDebtsAsync_ShouldThrow_WhenUserNotMember()
        {
            // Arrange
            var groupId = Guid.NewGuid();
            var userId = "user1";
            _mockGroupRepo.Setup(r => r.IsMemberAsync(groupId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act & Assert
            await Assert.ThrowsAsync<GroupAccessDeniedException>(() => 
                _service.GetSimplifiedDebtsAsync(groupId, userId, CancellationToken.None));
        }

        [Fact]
        public async Task GetSimplifiedDebtsAsync_ShouldSimplifyChain_WhenValid()
        {
            // Arrange
            var groupId = Guid.NewGuid();
            var userId = "userA";
            
            // A owes B 100
            // B owes C 100
            // Result should be A owes C 100
            
            var expenses = new List<ExpenseEntity>
            {
                // B paid 100 for A
                new ExpenseEntity 
                { 
                    GroupId = groupId, 
                    PaidByUserId = "userB", 
                    Amount = 100, 
                    Currency = "USD",
                    Splits = new List<ExpenseSplit> 
                    { 
                        new ExpenseSplit { UserId = "userA", Amount = 100 } 
                    } 
                },
                // C paid 100 for B
                new ExpenseEntity 
                { 
                    GroupId = groupId, 
                    PaidByUserId = "userC", 
                    Amount = 100, 
                    Currency = "USD",
                    Splits = new List<ExpenseSplit> 
                    { 
                        new ExpenseSplit { UserId = "userB", Amount = 100 } 
                    } 
                }
            };

            var settlements = new List<Settlement>();
            var members = new List<GroupMember>
            {
                new GroupMember { UserId = "userA" },
                new GroupMember { UserId = "userB" },
                new GroupMember { UserId = "userC" }
            };

            _mockGroupRepo.Setup(r => r.IsMemberAsync(groupId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mockBalanceRepo.Setup(r => r.GetExpensesWithSplitsAsync(groupId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expenses);
            _mockBalanceRepo.Setup(r => r.GetSettlementsAsync(groupId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(settlements);
            _mockBalanceRepo.Setup(r => r.GetMembersAsync(groupId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(members);

            // Act
            var result = await _service.GetSimplifiedDebtsAsync(groupId, userId, CancellationToken.None);

            // Assert
            result.Should().HaveCount(1); // 1 Currency Group
            var group = result.First();
            group.Currency.Should().Be("USD");
            group.Transfers.Should().HaveCount(1);
            
            var transfer = group.Transfers.First();
            transfer.FromUserId.Should().Be("userA");
            transfer.ToUserId.Should().Be("userC");
            transfer.Amount.Should().Be(100);
        }

        [Fact]
        public async Task GetSimplifiedDebtsAsync_ShouldHandleMultipleCurrencies_Independently()
        {
            // Arrange
            var groupId = Guid.NewGuid();
            var userId = "userA";

            // USD: A owes B 100
            // EUR: B owes A 50

            var expenses = new List<ExpenseEntity>
            {
                // B paid 100 USD for A
                new ExpenseEntity 
                { 
                    GroupId = groupId, 
                    PaidByUserId = "userB", 
                    Amount = 100, 
                    Currency = "USD",
                    Splits = new List<ExpenseSplit> 
                    { 
                        new ExpenseSplit { UserId = "userA", Amount = 100 } 
                    } 
                },
                // A paid 50 EUR for B
                new ExpenseEntity 
                { 
                    GroupId = groupId, 
                    PaidByUserId = "userA", 
                    Amount = 50, 
                    Currency = "EUR",
                    Splits = new List<ExpenseSplit> 
                    { 
                        new ExpenseSplit { UserId = "userB", Amount = 50 } 
                    } 
                }
            };

            var settlements = new List<Settlement>();
            var members = new List<GroupMember>
            {
                new GroupMember { UserId = "userA" },
                new GroupMember { UserId = "userB" }
            };

            _mockGroupRepo.Setup(r => r.IsMemberAsync(groupId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mockBalanceRepo.Setup(r => r.GetExpensesWithSplitsAsync(groupId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expenses);
            _mockBalanceRepo.Setup(r => r.GetSettlementsAsync(groupId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(settlements);
            _mockBalanceRepo.Setup(r => r.GetMembersAsync(groupId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(members);

            // Act
            var result = await _service.GetSimplifiedDebtsAsync(groupId, userId, CancellationToken.None);

            // Assert
            result.Should().HaveCount(2);
            
            var usdGroup = result.First(g => g.Currency == "USD");
            usdGroup.Transfers.Should().ContainSingle(t => t.FromUserId == "userA" && t.ToUserId == "userB" && t.Amount == 100);

            var eurGroup = result.First(g => g.Currency == "EUR");
            eurGroup.Transfers.Should().ContainSingle(t => t.FromUserId == "userB" && t.ToUserId == "userA" && t.Amount == 50);
        }

        [Fact]
        public async Task GetSimplifiedDebtsAsync_ShouldAccountForSettlements()
        {
            // Arrange
            var groupId = Guid.NewGuid();
            var userId = "userA";

            // A owes B 100 USD
            // A pays B 50 USD (Settlement)
            // Remaining Debt: A owes B 50 USD

            var expenses = new List<ExpenseEntity>
            {
                new ExpenseEntity 
                { 
                    GroupId = groupId, 
                    PaidByUserId = "userB", 
                    Amount = 100, 
                    Currency = "USD",
                    Splits = new List<ExpenseSplit> 
                    { 
                        new ExpenseSplit { UserId = "userA", Amount = 100 } 
                    } 
                }
            };

            var settlements = new List<Settlement>
            {
                new Settlement
                {
                    GroupId = groupId,
                    PayerUserId = "userA",
                    PayeeUserId = "userB",
                    Amount = 50,
                    Currency = "USD"
                }
            };

            var members = new List<GroupMember>
            {
                new GroupMember { UserId = "userA" },
                new GroupMember { UserId = "userB" }
            };

            _mockGroupRepo.Setup(r => r.IsMemberAsync(groupId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mockBalanceRepo.Setup(r => r.GetExpensesWithSplitsAsync(groupId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expenses);
            _mockBalanceRepo.Setup(r => r.GetSettlementsAsync(groupId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(settlements);
            _mockBalanceRepo.Setup(r => r.GetMembersAsync(groupId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(members);

            // Act
            var result = await _service.GetSimplifiedDebtsAsync(groupId, userId, CancellationToken.None);

            // Assert
            result.Should().HaveCount(1);
            var transfer = result.First().Transfers.Single();
            transfer.FromUserId.Should().Be("userA");
            transfer.ToUserId.Should().Be("userB");
            transfer.Amount.Should().Be(50);
        }
    }
}

using System;

namespace Expense.Core.DTOs.Home
{
    public enum HomeFeedType
    {
        Expense = 1,
        Settlement = 2,
        PersonalExpense = 3
    }

    public enum HomeFeedDirection
    {
        In = 1,       // Money coming in (Received settlement)
        Out = 2,      // Money going out (Paid expense, Paid settlement, Personal expense)
        Neutral = 3   // No immediate cash flow (Added to split/debt but didn't pay)
    }

    public class HomeFeedItemDto
    {
        public Guid Id { get; set; }
        public HomeFeedType Type { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Description { get; set; } = string.Empty;
        
        public Guid? GroupId { get; set; }
        public string? GroupName { get; set; }
        
        public string? OtherUserId { get; set; } // Nullable
        public string? OtherUserName { get; set; }

        public HomeFeedDirection Direction { get; set; }
    }
}

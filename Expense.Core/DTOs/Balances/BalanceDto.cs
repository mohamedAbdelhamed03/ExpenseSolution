using System;

namespace Expense.Core.DTOs.Balances
{
    public class BalanceDto
    {
        public string UserId { get; set; } = string.Empty;
        public decimal TotalPaid { get; set; }
        public decimal TotalShared { get; set; }
        public decimal Balance { get; set; }
    }
}

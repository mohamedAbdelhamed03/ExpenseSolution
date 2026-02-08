using System;

namespace Expense.Core.DTOs.Settlements
{
    public class CreateSettlementDto
    {
        public string PayeeUserId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "EGP";
        public decimal? ExchangeRate { get; set; }
        public DateTime SettlementDate { get; set; }
    }

    public class SettlementDto
    {
        public Guid Id { get; set; }
        public Guid GroupId { get; set; }
        public string PayerUserId { get; set; } = string.Empty;
        public string PayeeUserId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public decimal? ExchangeRate { get; set; }
        public DateTime SettlementDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

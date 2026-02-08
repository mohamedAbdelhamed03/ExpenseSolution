using System.Collections.Generic;

namespace Expense.Core.DTOs.Debts
{
    public class SimplifiedDebtGroupDto
    {
        public string Currency { get; set; } = string.Empty;
        public List<SimplifiedDebtDto> Transfers { get; set; } = new List<SimplifiedDebtDto>();
    }
}

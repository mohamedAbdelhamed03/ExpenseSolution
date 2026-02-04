namespace Expense.Core.Common.Exceptions
{
    public class ExpenseNotFoundException : NotFoundException
    {
        public ExpenseNotFoundException() : base("Expense.NotFound")
        {
        }
    }
}
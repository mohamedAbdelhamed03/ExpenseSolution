namespace Expense.Core.DTOs.Home
{
    public class HomeFeedRequestDto
    {
        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;
    }
}

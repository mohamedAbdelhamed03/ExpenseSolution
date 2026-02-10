using System.ComponentModel.DataAnnotations;

namespace Expense.Core.DTOs.Home
{
    public class HomeFeedRequestDto
    {
        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;

        [Range(1, 100)]
        public int PageSize { get; set; } = 20;
    }
}

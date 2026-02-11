using Expense.Core.Application.Insights;
using Expense.Core.Application.Persistence;
using Expense.Core.DTOs.Insights;
using Expense.Core.Common.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Expense.Infrastructure.Insights
{
    public class InsightsService : IInsightsService
    {
        private readonly IUnitOfWork _unitOfWork;

        public InsightsService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<InsightsSummaryDto>> GetInsightsAsync(Guid groupId, string period, string date, string scope, string userId, CancellationToken cancellationToken = default)
        {
            var isMember = await _unitOfWork.Groups.IsMemberAsync(groupId, userId, cancellationToken);
            if (!isMember) throw new GroupAccessDeniedException("Group_NotMember");

            ResolveDateRange(period, date, out DateTime startDate, out DateTime endDate);

            IEnumerable<CategoryStatistics> stats;
            scope = scope?.ToLower().Trim() ?? "group";

            if (scope == "me")
            {
                stats = await _unitOfWork.Expenses.GetMyInsightsByCategoryAsync(groupId, userId, startDate, endDate);
            }
            else
            {
                stats = await _unitOfWork.Expenses.GetInsightsByCategoryAsync(groupId, startDate, endDate);
            }

            var categories = await _unitOfWork.Categories.GetAll(c => c.GroupId == groupId);

            var result = new List<InsightsSummaryDto>();
            var byCurrency = stats.GroupBy(s => s.Currency);

            foreach (var currencyGroup in byCurrency)
            {
                var currency = currencyGroup.Key;
                var total = currencyGroup.Sum(s => s.TotalAmount);

                var summary = new InsightsSummaryDto
                {
                    GroupId = groupId,
                    Period = period,
                    Date = date,
                    Currency = currency,
                    TotalAmount = total,
                    Categories = new List<CategoryInsightDto>()
                };

                foreach (var stat in currencyGroup)
                {
                    string catName = "Uncategorized";
                    if (stat.CategoryId.HasValue)
                    {
                        var cat = categories.FirstOrDefault(c => c.Id == stat.CategoryId.Value);
                        if (cat != null) catName = cat.Name;
                    }

                    summary.Categories.Add(new CategoryInsightDto
                    {
                        CategoryId = stat.CategoryId,
                        CategoryName = catName,
                        Amount = stat.TotalAmount,
                        Percentage = total == 0 ? 0 : Math.Round((stat.TotalAmount / total) * 100, 2),
                        Currency = currency
                    });
                }

                summary.Categories = summary.Categories.OrderByDescending(c => c.Amount).ToList();
                result.Add(summary);
            }

            return result;
        }

        public async Task<IEnumerable<InsightsSummaryDto>> GetHomeInsightsAsync(string period, string date, string userId, CancellationToken cancellationToken = default)
        {
            ResolveDateRange(period, date, out DateTime startDate, out DateTime endDate);

            var groupStats = await _unitOfWork.Expenses.GetUserExpensesByCategoryAsync(userId, startDate, endDate);
            var personalStats = await _unitOfWork.PersonalExpenses.GetPersonalExpensesByCategoryAsync(userId, startDate, endDate);

            var allStats = groupStats.Concat(personalStats);
            var categoryIds = allStats.Where(s => s.CategoryId.HasValue).Select(s => s.CategoryId.Value).Distinct().ToList();
            var categories = await _unitOfWork.Categories.GetAll(c => categoryIds.Contains(c.Id));

            var result = new List<InsightsSummaryDto>();
            var byCurrency = allStats.GroupBy(s => s.Currency);

            foreach (var currencyGroup in byCurrency)
            {
                var currency = currencyGroup.Key;
                // Group by CategoryId within Currency to merge split and personal stats for same category
                var categoryGroups = currencyGroup.GroupBy(s => s.CategoryId);
                var total = currencyGroup.Sum(s => s.TotalAmount);

                var summary = new InsightsSummaryDto
                {
                    GroupId = Guid.Empty, // Indicates home/global scope
                    Period = period,
                    Date = date,
                    Currency = currency,
                    TotalAmount = total,
                    Categories = new List<CategoryInsightDto>()
                };

                foreach (var catGroup in categoryGroups)
                {
                    var catId = catGroup.Key;
                    var catTotal = catGroup.Sum(s => s.TotalAmount);
                    
                    string catName = "Uncategorized";
                    if (catId.HasValue)
                    {
                        var cat = categories.FirstOrDefault(c => c.Id == catId.Value);
                        if (cat != null) catName = cat.Name;
                    }

                    summary.Categories.Add(new CategoryInsightDto
                    {
                        CategoryId = catId,
                        CategoryName = catName,
                        Amount = catTotal,
                        Percentage = total == 0 ? 0 : Math.Round((catTotal / total) * 100, 2),
                        Currency = currency
                    });
                }

                summary.Categories = summary.Categories.OrderByDescending(c => c.Amount).ToList();
                result.Add(summary);
            }

            return result;
        }

        private void ResolveDateRange(string period, string dateStr, out DateTime startDate, out DateTime endDate)
        {
            period = period?.ToLower().Trim() ?? "month";

            if (period == "all")
            {
                startDate = DateTime.MinValue;
                endDate = DateTime.MaxValue;
                return;
            }

            if (string.IsNullOrEmpty(dateStr))
            {
                // Default to current date if missing
                var now = DateTime.UtcNow;
                dateStr = period == "year" ? now.Year.ToString() : $"{now.Year}-{now.Month:D2}";
            }

            if (period == "year")
            {
                if (!int.TryParse(dateStr, out int year))
                {
                    throw new BusinessException("Insights_InvalidDate_Year");
                }
                startDate = new DateTime(year, 1, 1);
                endDate = new DateTime(year, 12, 31, 23, 59, 59);
            }
            else // month
            {
                // Expected format YYYY-MM
                var parts = dateStr.Split('-');
                if (parts.Length != 2 || !int.TryParse(parts[0], out int year) || !int.TryParse(parts[1], out int month))
                {
                     throw new BusinessException("Insights_InvalidDate_Month");
                }
                
                if (month < 1 || month > 12) throw new BusinessException("Insights_InvalidDate_Month");

                startDate = new DateTime(year, month, 1);
                endDate = startDate.AddMonths(1).AddSeconds(-1);
            }
        }
    }
}

using Expense.Core.Application.Persistence;
using Expense.Core.DTOs.Home;
using Expense.Core.Domain.Entities;
using Expense.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Expense.Infrastructure.Repositories
{
    public class HomeFeedRepository : IHomeFeedRepository
    {
        private readonly ApplicationDbContext _db;

        public HomeFeedRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<HomeFeedItemDto>> GetFeedAsync(Guid userId, int page, int pageSize)
        {
            var userIdString = userId.ToString();

            // 1. Expenses where user is payer (AND is currently a member of the group)
            var paidExpenses = _db.Expenses
                .AsNoTracking()
                .Where(e => e.PaidByUserId == userIdString && e.Group!.Members.Any(m => m.UserId == userIdString))
                .Select(e => new HomeFeedItemDto
                {
                    Id = e.Id,
                    Type = HomeFeedType.Expense,
                    Amount = e.Amount,
                    Currency = e.Currency,
                    Date = e.ExpenseDate,
                    Description = e.Description ?? "Expense",
                    GroupId = e.GroupId,
                    GroupName = e.Group != null ? e.Group.Name : null,
                    Direction = HomeFeedDirection.Out, // I paid -> Out
                    OtherUserId = null,
                    OtherUserName = null
                });

            // 2. Expenses where user is in split (and NOT payer) (AND is currently a member of the group)
            var owedExpenses = _db.ExpenseSplits
                .AsNoTracking()
                .Where(s => s.UserId == userIdString && s.Expense.PaidByUserId != userIdString && s.Expense.Group!.Members.Any(m => m.UserId == userIdString))
                .Select(s => new HomeFeedItemDto
                {
                    Id = s.Expense.Id,
                    Type = HomeFeedType.Expense,
                    Amount = s.Amount, // My share
                    Currency = s.Expense.Currency,
                    Date = s.Expense.ExpenseDate,
                    Description = s.Expense.Description ?? "Expense",
                    GroupId = s.Expense.GroupId,
                    GroupName = s.Expense.Group != null ? s.Expense.Group.Name : null,
                    Direction = HomeFeedDirection.Neutral, // I didn't pay, I just owe -> Neutral (or In debt, but no cash flow yet)
                    OtherUserId = s.Expense.PaidByUserId,
                    OtherUserName = null // To be filled
                });

            // 3. Settlements Paid by user (AND is currently a member of the group)
            var paidSettlements = _db.Settlements
                .AsNoTracking()
                .Where(s => s.PayerUserId == userIdString && s.Group!.Members.Any(m => m.UserId == userIdString))
                .Select(s => new HomeFeedItemDto
                {
                    Id = s.Id,
                    Type = HomeFeedType.Settlement,
                    Amount = s.Amount,
                    Currency = s.Currency,
                    Date = s.SettlementDate,
                    Description = "Settlement Paid",
                    GroupId = s.GroupId,
                    GroupName = s.Group != null ? s.Group.Name : null,
                    Direction = HomeFeedDirection.Out, // I paid -> Out
                    OtherUserId = s.PayeeUserId,
                    OtherUserName = null // To be filled
                });

            // 4. Settlements Received by user (AND is currently a member of the group)
            var receivedSettlements = _db.Settlements
                .AsNoTracking()
                .Where(s => s.PayeeUserId == userIdString && s.Group!.Members.Any(m => m.UserId == userIdString))
                .Select(s => new HomeFeedItemDto
                {
                    Id = s.Id,
                    Type = HomeFeedType.Settlement,
                    Amount = s.Amount,
                    Currency = s.Currency,
                    Date = s.SettlementDate,
                    Description = "Settlement Received",
                    GroupId = s.GroupId,
                    GroupName = s.Group != null ? s.Group.Name : null,
                    Direction = HomeFeedDirection.In, // I received -> In
                    OtherUserId = s.PayerUserId,
                    OtherUserName = null // To be filled
                });

            // 5. Personal Expenses
            var personalExpenses = _db.PersonalExpenses
                .AsNoTracking()
                .Where(p => p.UserId == userIdString)
                .Select(p => new HomeFeedItemDto
                {
                    Id = p.Id,
                    Type = HomeFeedType.PersonalExpense,
                    Amount = p.Amount,
                    Currency = p.Currency,
                    Date = p.Date,
                    Description = p.Description,
                    GroupId = null,
                    GroupName = null,
                    Direction = HomeFeedDirection.Out, // I spent money -> Out
                    OtherUserId = null,
                    OtherUserName = null
                });

            // Concat (UNION ALL)
            var query = paidExpenses
                .Concat(owedExpenses)
                .Concat(paidSettlements)
                .Concat(receivedSettlements)
                .Concat(personalExpenses);

            // Order and Paging
            var items = await query
                .OrderByDescending(x => x.Date)
                .ThenByDescending(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Enrich with User Names
            await EnrichUserNamesAsync(items);

            return items;
        }

        private async Task EnrichUserNamesAsync(List<HomeFeedItemDto> items)
        {
            var userIdsToFetch = items
                .Where(x => !string.IsNullOrEmpty(x.OtherUserId))
                .Select(x => x.OtherUserId!)
                .Distinct()
                .ToList();

            if (!userIdsToFetch.Any()) return;

            // Cannot efficiently join string ids with Guid ids in LINQ-to-Entities if types mismatch,
            // but fetching dictionary is safe.
            // We need to parse string to Guid for the query if IdentityUser<Guid>.
            
            var guidIds = new List<Guid>();
            foreach(var idStr in userIdsToFetch)
            {
                if(Guid.TryParse(idStr, out var g)) guidIds.Add(g);
            }

            if (!guidIds.Any()) return;

            var users = await _db.Users
                .AsNoTracking()
                .Where(u => guidIds.Contains(u.Id))
                .Select(u => new { u.Id, u.FirstName, u.LastName, u.Email })
                .ToDictionaryAsync(u => u.Id);

            foreach (var item in items)
            {
                if (!string.IsNullOrEmpty(item.OtherUserId) && Guid.TryParse(item.OtherUserId, out var uid))
                {
                    if (users.TryGetValue(uid, out var user))
                    {
                        item.OtherUserName = $"{user.FirstName} {user.LastName}".Trim();
                        if (string.IsNullOrEmpty(item.OtherUserName)) item.OtherUserName = user.Email;
                    }
                }
            }
        }
    }
}

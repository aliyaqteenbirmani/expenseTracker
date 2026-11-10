using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpendwiseSystem.Domain.DTOs.SplitExpenseDtos;
using SpendwiseSystem.Domain.Entities;

namespace SpendwiseSystem.Application.Interfaces
{
    public interface ISplitExpenseRepository
    {
        Task<SplitExpense?> GetExpenseByIdAsync(Guid expenseId);
        Task<SplitExpenseDetailDto?> GetExpenseDetailsAsync(Guid expenseId, Guid groupId);
        Task<PagedExpenseResultDto> GetExpensesListAsync(Guid groupId, Guid currentUserId, SplitExpenseQueryParameters parameters);
        Task<SplitExpenseDetailDto> CreateExpenseWithSharesAndActivityAsync(
            SplitExpense expense, List<SplitExpenseShare> shares, string creatorName, string groupName);
        Task<SplitExpenseDetailDto?> UpdateExpenseWithSharesAndActivityAsync(
            SplitExpense expense, List<SplitExpenseShare> newShares, string editorName, string groupName);
        Task<bool> SoftDeleteExpenseWithActivityAsync(
            Guid expenseId, Guid groupId, Guid deletedByUserId, string deletedByName);
        Task<List<SplitExpenseShare>> GetExpenseSharesAsync(Guid expenseId);
    }
}

using System;
using System.Threading.Tasks;
using SpendwiseSystem.Domain.DTOs.SplitExpenseDtos;
using SpendwiseSystem.Domain.Entities;

namespace SpendwiseSystem.Application.Interfaces
{
    public interface ISplitExpenseService
    {
        Task<ApiResponse<SplitExpenseDetailDto>> AddExpenseAsync(
            Guid groupId, CreateSplitExpenseDto dto, Guid currentUserId, string currentUserName);

        Task<ApiResponse<PagedExpenseResultDto>> GetExpensesListAsync(
            Guid groupId, Guid currentUserId, SplitExpenseQueryParameters parameters);

        Task<ApiResponse<SplitExpenseDetailDto>> GetExpenseDetailsAsync(
            Guid groupId, Guid expenseId, Guid currentUserId);

        Task<ApiResponse<SplitExpenseDetailDto>> EditExpenseAsync(
            Guid groupId, Guid expenseId, UpdateSplitExpenseDto dto, Guid currentUserId, string currentUserName);

        Task<ApiResponse<bool>> DeleteExpenseAsync(
            Guid groupId, Guid expenseId, Guid currentUserId, string currentUserName);
    }
}
